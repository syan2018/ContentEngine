using ContentEngine.Core.DataPipeline.Services;
using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.Inference.Utils;
using ContentEngine.Core.Storage;
using LiteDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理引擎服务实现
    /// </summary>
    public class ReasoningService : IReasoningService
    {
        private readonly LiteDbContext _liteDbContext;
        private readonly IDataEntryService _dataEntryService;
        private readonly IPromptExecutionService _promptExecutionService;
        private readonly ILogger<ReasoningService> _logger;

        private readonly ILiteCollection<ReasoningTransactionDefinition> _definitionsCollection;
        private readonly ILiteCollection<ReasoningTransactionInstance> _instancesCollection;

        public ReasoningService(
            LiteDbContext liteDbContext,
            IDataEntryService dataEntryService,
            IPromptExecutionService promptExecutionService,
            ILogger<ReasoningService> logger)
        {
            _liteDbContext = liteDbContext;
            _dataEntryService = dataEntryService;
            _promptExecutionService = promptExecutionService;
            _logger = logger;

            _definitionsCollection = _liteDbContext.ReasoningDefinitions;
            _instancesCollection = _liteDbContext.ReasoningInstances;
        }

        #region 推理事务定义管理

        public async Task<ReasoningTransactionDefinition> CreateDefinitionAsync(ReasoningTransactionDefinition definition, CancellationToken cancellationToken = default)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new ArgumentException("推理事务名称不能为空", nameof(definition));

            if (definition.QueryDefinitions == null || !definition.QueryDefinitions.Any())
                throw new ArgumentException("推理事务必须包含至少一个查询定义", nameof(definition));

            // 验证模板
            var templateValidation = PromptTemplatingEngine.ValidateTemplate(definition.PromptTemplate.TemplateContent);
            if (!templateValidation.IsValid)
            {
                throw new ArgumentException($"Prompt模板无效: {string.Join("; ", templateValidation.Errors)}");
            }

            definition.CreatedAt = DateTime.UtcNow;
            definition.UpdatedAt = DateTime.UtcNow;

            _definitionsCollection.Insert(definition);
            _logger.LogInformation("创建推理事务定义: {DefinitionName} (ID: {DefinitionId})", definition.Name, definition.Id);

            return definition;
        }

        public async Task<List<ReasoningTransactionDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_definitionsCollection.FindAll().OrderByDescending(x => x.CreatedAt).ToList());
        }

        public async Task<ReasoningTransactionDefinition?> GetDefinitionByIdAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                return null;

            return await Task.FromResult(_definitionsCollection.FindById(definitionId));
        }

        public async Task<ReasoningTransactionDefinition> UpdateDefinitionAsync(ReasoningTransactionDefinition definition, CancellationToken cancellationToken = default)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var existing = await GetDefinitionByIdAsync(definition.Id, cancellationToken);
            if (existing == null)
                throw new InvalidOperationException($"推理事务定义不存在: {definition.Id}");

            // 验证模板
            var templateValidation = PromptTemplatingEngine.ValidateTemplate(definition.PromptTemplate.TemplateContent);
            if (!templateValidation.IsValid)
            {
                throw new ArgumentException($"Prompt模板无效: {string.Join("; ", templateValidation.Errors)}");
            }

            definition.UpdatedAt = DateTime.UtcNow;
            _definitionsCollection.Update(definition);

            _logger.LogInformation("更新推理事务定义: {DefinitionName} (ID: {DefinitionId})", definition.Name, definition.Id);
            return definition;
        }

        public async Task<bool> DeleteDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            // 检查是否有关联的实例
            var hasInstances = _instancesCollection.Exists(x => x.DefinitionId == definitionId);
            if (hasInstances)
            {
                throw new InvalidOperationException("无法删除存在关联实例的推理事务定义，请先删除相关实例");
            }

            var deleted = _definitionsCollection.Delete(definitionId);
            if (deleted)
            {
                _logger.LogInformation("删除推理事务定义: {DefinitionId}", definitionId);
            }

            return deleted;
        }

        #endregion

        #region 推理事务执行

        public async Task<ReasoningTransactionInstance> ExecuteTransactionAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            // 获取定义
            var definition = await GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"推理事务定义不存在: {definitionId}");
            }

            // 创建实例
            var instance = new ReasoningTransactionInstance
            {
                DefinitionId = definitionId,
                Status = TransactionStatus.Pending,
                StartedAt = DateTime.UtcNow
            };

            _instancesCollection.Insert(instance);
            _logger.LogInformation("开始执行推理事务: {DefinitionName} (实例ID: {InstanceId})", definition.Name, instance.InstanceId);

            try
            {
                // 预估成本
                var estimatedCost = await EstimateExecutionCostAsync(definitionId, executionParams, cancellationToken);
                if (estimatedCost > definition.ExecutionConstraints.MaxEstimatedCostUSD)
                {
                    throw new InvalidOperationException($"预估成本 ${estimatedCost:F2} 超过限制 ${definition.ExecutionConstraints.MaxEstimatedCostUSD:F2}");
                }

                instance.Metrics.EstimatedCostUSD = estimatedCost;

                // 执行各阶段
                await FetchDataAsync(instance, definition, cancellationToken);
                await CombineDataAsync(instance, definition, cancellationToken);
                await GenerateOutputsAsync(instance, definition, cancellationToken);

                // 标记完成
                instance.Status = TransactionStatus.Completed;
                instance.CompletedAt = DateTime.UtcNow;
                instance.Metrics.ElapsedTime = DateTime.UtcNow - instance.StartedAt;

                _instancesCollection.Update(instance);
                _logger.LogInformation("推理事务执行完成: {InstanceId}", instance.InstanceId);

                return instance;
            }
            catch (Exception ex)
            {
                instance.Status = TransactionStatus.Failed;
                instance.CompletedAt = DateTime.UtcNow;
                instance.Metrics.ElapsedTime = DateTime.UtcNow - instance.StartedAt;
                instance.Errors.Add(new ErrorRecord
                {
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    IsRetriable = true
                });

                _instancesCollection.Update(instance);
                _logger.LogError(ex, "推理事务执行失败: {InstanceId}", instance.InstanceId);

                throw;
            }
        }

        #endregion

        #region 实例管理方法

        public async Task<List<ReasoningTransactionInstance>> GetInstancesAsync(
            string? definitionId = null, 
            TransactionStatus? status = null, 
            CancellationToken cancellationToken = default)
        {
            var query = _instancesCollection.Query();

            if (!string.IsNullOrWhiteSpace(definitionId))
            {
                query = query.Where(x => x.DefinitionId == definitionId);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            return await Task.FromResult(query.OrderByDescending(x => x.StartedAt).ToList());
        }

        public async Task<ReasoningTransactionInstance?> GetInstanceByIdAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return null;

            return await Task.FromResult(_instancesCollection.FindById(instanceId));
        }

        public async Task<bool> DeleteInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return false;

            var deleted = _instancesCollection.Delete(instanceId);
            if (deleted)
            {
                _logger.LogInformation("删除推理事务实例: {InstanceId}", instanceId);
            }

            return deleted;
        }

        #endregion

        #region 预估方法

        public async Task<decimal> EstimateExecutionCostAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var definition = await GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
                return 0;

            // 简单预估：基于模板长度和预期组合数
            var templateLength = definition.PromptTemplate.TemplateContent.Length;
            var estimatedCombinations = await EstimateCombinationCountAsync(definitionId, executionParams, cancellationToken);
            
            // 每个组合的预估成本
            var costPerCombination = await _promptExecutionService.EstimateCostAsync(
                new string('x', templateLength), // 模拟模板长度
                "ContentEngineHelper");

            return costPerCombination * estimatedCombinations;
        }

        public async Task<TimeSpan> EstimateExecutionTimeAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var estimatedCombinations = await EstimateCombinationCountAsync(definitionId, executionParams, cancellationToken);
            
            // 假设每个组合平均需要2秒处理
            var secondsPerCombination = 2;
            var totalSeconds = estimatedCombinations * secondsPerCombination;
            
            return TimeSpan.FromSeconds(totalSeconds);
        }

        public async Task<int> EstimateCombinationCountAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var definition = await GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
                return 0;

            // 简单预估：假设每个查询返回10条记录
            var estimatedRecordsPerQuery = 10;
            var queryCount = definition.QueryDefinitions.Count;
            
            // 如果有叉积规则，计算组合数
            var combinationRule = definition.DataCombinationRules.FirstOrDefault();
            if (combinationRule != null && combinationRule.ViewNamesToCrossProduct.Any())
            {
                var crossProductViews = combinationRule.ViewNamesToCrossProduct.Count;
                var estimatedCombinations = (int)Math.Pow(estimatedRecordsPerQuery, crossProductViews);
                return Math.Min(estimatedCombinations, combinationRule.MaxCombinations);
            }

            return estimatedRecordsPerQuery;
        }

        #endregion

        #region 暂未实现的方法

        public Task<ReasoningTransactionInstance> ResumeTransactionAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("恢复执行功能将在后续版本实现");
        }

        public Task<bool> PauseTransactionAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("暂停执行功能将在后续版本实现");
        }

        public Task<bool> CancelTransactionAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("取消执行功能将在后续版本实现");
        }

        #endregion

        #region 私有辅助方法

        private async Task FetchDataAsync(ReasoningTransactionInstance instance, ReasoningTransactionDefinition definition, CancellationToken cancellationToken)
        {
            instance.Status = TransactionStatus.FetchingData;
            _instancesCollection.Update(instance);

            _logger.LogInformation("开始获取数据: {InstanceId}", instance.InstanceId);

            foreach (var queryDef in definition.QueryDefinitions)
            {
                try
                {
                    var viewData = await ExecuteQueryDefinitionAsync(queryDef, cancellationToken);
                    instance.ResolvedViews[queryDef.OutputViewName] = viewData;

                    _logger.LogDebug("视图数据已获取: {ViewName}, 条目数: {ItemCount}", 
                        queryDef.OutputViewName, viewData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取视图数据失败: {ViewName}", queryDef.OutputViewName);
                    throw;
                }
            }

            _logger.LogInformation("数据获取完成: {InstanceId}, 视图数: {ViewCount}", 
                instance.InstanceId, instance.ResolvedViews.Count);
        }

        private async Task CombineDataAsync(ReasoningTransactionInstance instance, ReasoningTransactionDefinition definition, CancellationToken cancellationToken)
        {
            instance.Status = TransactionStatus.CombiningData;
            _instancesCollection.Update(instance);

            _logger.LogInformation("开始组合数据: {InstanceId}", instance.InstanceId);

            var combinationRule = definition.DataCombinationRules.FirstOrDefault();
            if (combinationRule == null)
            {
                throw new InvalidOperationException("推理事务定义缺少数据组合规则");
            }

            var combinations = GenerateDataCombinations(instance.ResolvedViews, combinationRule);
            instance.InputCombinations = combinations;
            instance.Metrics.TotalCombinations = combinations.Count;

            _logger.LogInformation("数据组合完成: {InstanceId}, 组合数: {CombinationCount}", 
                instance.InstanceId, combinations.Count);

            await Task.CompletedTask;
        }

        private async Task GenerateOutputsAsync(ReasoningTransactionInstance instance, ReasoningTransactionDefinition definition, CancellationToken cancellationToken)
        {
            instance.Status = TransactionStatus.GeneratingOutputs;
            _instancesCollection.Update(instance);

            _logger.LogInformation("开始生成AI输出: {InstanceId}, 组合数: {CombinationCount}", 
                instance.InstanceId, instance.InputCombinations.Count);

            var constraints = definition.ExecutionConstraints;
            var semaphore = new SemaphoreSlim(constraints.MaxConcurrentAICalls, constraints.MaxConcurrentAICalls);
            var tasks = new List<Task>();

            foreach (var combination in instance.InputCombinations)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var task = ProcessCombinationAsync(instance, definition, combination, semaphore, cancellationToken);
                tasks.Add(task);

                // 如果启用批处理，等待一批任务完成
                if (constraints.EnableBatching && tasks.Count >= constraints.BatchSize)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            // 等待剩余任务完成
            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }

            _logger.LogInformation("AI输出生成完成: {InstanceId}, 成功: {SuccessCount}, 失败: {FailureCount}", 
                instance.InstanceId, instance.Metrics.SuccessfulOutputs, instance.Metrics.FailedOutputs);
        }

        private async Task<ViewData> ExecuteQueryDefinitionAsync(QueryDefinition queryDef, CancellationToken cancellationToken)
        {
            var collection = _liteDbContext.GetDataCollection(queryDef.SourceSchemaName);
            var query = collection.Query();

            // 应用筛选条件
            if (!string.IsNullOrWhiteSpace(queryDef.FilterExpression))
            {
                // 这里需要解析LiteDB查询表达式，暂时简化处理
                _logger.LogWarning("筛选表达式暂未完全实现: {FilterExpression}", queryDef.FilterExpression);
            }

            var documents = query.ToList();

            // 应用字段选择
            if (queryDef.SelectFields.Any())
            {
                var filteredDocuments = new List<BsonDocument>();
                foreach (var doc in documents)
                {
                    var filteredDoc = new BsonDocument();
                    filteredDoc["_id"] = doc["_id"]; // 保留ID

                    foreach (var field in queryDef.SelectFields)
                    {
                        if (doc.ContainsKey(field))
                        {
                            filteredDoc[field] = doc[field];
                        }
                    }
                    filteredDocuments.Add(filteredDoc);
                }
                documents = filteredDocuments;
            }

            return new ViewData
            {
                ViewName = queryDef.OutputViewName,
                Items = documents
            };
        }

        private List<ReasoningInputCombination> GenerateDataCombinations(Dictionary<string, ViewData> resolvedViews, DataCombinationRule combinationRule)
        {
            var combinations = new List<ReasoningInputCombination>();

            if (!combinationRule.ViewNamesToCrossProduct.Any())
            {
                // 如果没有叉积视图，创建单个组合
                var combination = new ReasoningInputCombination();
                foreach (var kvp in resolvedViews)
                {
                    if (kvp.Value.Items.Any())
                    {
                        combination.DataMap[kvp.Key] = kvp.Value.Items.First();
                    }
                }
                combinations.Add(combination);
                return combinations;
            }

            // 获取需要叉积的视图
            var crossProductViews = new List<ViewData>();
            foreach (var viewName in combinationRule.ViewNamesToCrossProduct)
            {
                if (resolvedViews.TryGetValue(viewName, out var viewData))
                {
                    crossProductViews.Add(viewData);
                }
            }

            // 生成叉积组合
            GenerateCrossProductCombinations(crossProductViews, 0, new Dictionary<string, BsonDocument>(), combinations, combinationRule.MaxCombinations);

            // 添加单例上下文视图
            foreach (var combination in combinations)
            {
                foreach (var singletonViewName in combinationRule.SingletonViewNamesForContext)
                {
                    if (resolvedViews.TryGetValue(singletonViewName, out var singletonView) && singletonView.Items.Any())
                    {
                        combination.DataMap[singletonViewName] = singletonView.Items.First();
                    }
                }
            }

            return combinations;
        }

        private void GenerateCrossProductCombinations(
            List<ViewData> views, 
            int currentViewIndex, 
            Dictionary<string, BsonDocument> currentCombination, 
            List<ReasoningInputCombination> results, 
            int maxCombinations)
        {
            if (results.Count >= maxCombinations)
                return;

            if (currentViewIndex >= views.Count)
            {
                // 完成一个组合
                var combination = new ReasoningInputCombination();
                foreach (var kvp in currentCombination)
                {
                    combination.DataMap[kvp.Key] = kvp.Value;
                }
                results.Add(combination);
                return;
            }

            var currentView = views[currentViewIndex];
            foreach (var item in currentView.Items)
            {
                if (results.Count >= maxCombinations)
                    break;

                currentCombination[currentView.ViewName] = item;
                GenerateCrossProductCombinations(views, currentViewIndex + 1, currentCombination, results, maxCombinations);
                currentCombination.Remove(currentView.ViewName);
            }
        }

        private async Task ProcessCombinationAsync(
            ReasoningTransactionInstance instance, 
            ReasoningTransactionDefinition definition, 
            ReasoningInputCombination combination, 
            SemaphoreSlim semaphore, 
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // 填充Prompt模板
                var filledPrompt = PromptTemplatingEngine.Fill(definition.PromptTemplate.TemplateContent, combination.DataMap);

                // 执行AI调用
                var result = await _promptExecutionService.ExecutePromptAsync(filledPrompt, "ContentEngineHelper", cancellationToken);

                // 创建输出项
                var outputItem = new ReasoningOutputItem
                {
                    InputCombinationId = combination.CombinationId,
                    GeneratedText = result.GeneratedText,
                    IsSuccess = result.IsSuccess,
                    FailureReason = result.FailureReason,
                    CostUSD = result.CostUSD,
                    ExecutionTime = result.ExecutionTime
                };

                lock (instance)
                {
                    instance.Outputs.Add(outputItem);
                    instance.Metrics.ProcessedCombinations++;
                    instance.Metrics.ActualCostUSD += result.CostUSD;

                    if (result.IsSuccess)
                    {
                        instance.Metrics.SuccessfulOutputs++;
                    }
                    else
                    {
                        instance.Metrics.FailedOutputs++;
                        instance.Errors.Add(new ErrorRecord
                        {
                            ErrorType = "AIExecutionError",
                            Message = result.FailureReason ?? "AI执行失败",
                            CombinationId = combination.CombinationId,
                            IsRetriable = true
                        });
                    }

                    instance.LastProcessedCombinationId = combination.CombinationId;
                }

                // 定期更新实例状态
                if (instance.Metrics.ProcessedCombinations % 10 == 0)
                {
                    _instancesCollection.Update(instance);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        #endregion
    }
} 