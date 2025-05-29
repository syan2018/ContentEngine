using ContentEngine.Core.Inference.Models;
using Microsoft.Extensions.Logging;
using ContentEngine.Core.Inference.Utils;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理执行服务实现
    /// </summary>
    public class ReasoningExecutionService : IReasoningExecutionService
    {
        private readonly IReasoningDefinitionService _definitionService;
        private readonly IReasoningInstanceService _instanceService;
        private readonly IReasoningEstimationService _estimationService;
        private readonly IQueryProcessingService _queryProcessingService;
        private readonly IPromptExecutionService _promptExecutionService;
        private readonly ILogger<ReasoningExecutionService> _logger;

        public ReasoningExecutionService(
            IReasoningDefinitionService definitionService,
            IReasoningInstanceService instanceService,
            IReasoningEstimationService estimationService,
            IQueryProcessingService queryProcessingService,
            IPromptExecutionService promptExecutionService,
            ILogger<ReasoningExecutionService> logger)
        {
            _definitionService = definitionService;
            _instanceService = instanceService;
            _estimationService = estimationService;
            _queryProcessingService = queryProcessingService;
            _promptExecutionService = promptExecutionService;
            _logger = logger;
        }

        // TODO: 重构为Instance驱动
        public async Task<ReasoningTransactionInstance> ExecuteTransactionAsync(
            string instanceId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            // 创建实例
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");
            }
            
            // 执行前检查
            var preCheckResult = await PreCheckExecutionAsync(instance.DefinitionId, executionParams, cancellationToken);
            if (!preCheckResult.CanExecute)
            {
                throw new InvalidOperationException($"执行前检查失败: {string.Join("; ", preCheckResult.Errors)}");
            }
            
            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);

            
            _logger.LogInformation("开始执行推理事务: {DefinitionName} (实例ID: {InstanceId})", definition.Name, instance.InstanceId);
            
            try
            {
                instance.Metrics.EstimatedCostUSD = preCheckResult.EstimatedCostUSD;

                // 处理数据
                await ProcessDataAsync(instance, definition, cancellationToken);
                
                // 生成AI输出
                await GenerateOutputsAsync(instance, definition, cancellationToken);

                // 标记完成
                instance.Status = TransactionStatus.Completed;
                instance.CompletedAt = DateTime.UtcNow;
                instance.Metrics.ElapsedTime = DateTime.UtcNow - instance.StartedAt;

                await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
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

                await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
                _logger.LogError(ex, "推理事务执行失败: {InstanceId}", instance.InstanceId);

                throw;
            }
        }

        public async Task<ExecutionPreCheckResult> PreCheckExecutionAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new ExecutionPreCheckResult { CanExecute = true };

            try
            {
                var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
                if (definition == null)
                {
                    result.Errors.Add($"推理事务定义不存在: {definitionId}");
                    result.CanExecute = false;
                    return result;
                }

                // 预估成本、时间和组合数量
                result.EstimatedCostUSD = await _estimationService.EstimateExecutionCostAsync(definitionId, executionParams, cancellationToken);
                result.EstimatedTime = await _estimationService.EstimateExecutionTimeAsync(definitionId, executionParams, cancellationToken);
                result.EstimatedCombinations = await _estimationService.EstimateCombinationCountAsync(definitionId, executionParams, cancellationToken);

                // 检查成本限制
                if (result.EstimatedCostUSD > definition.ExecutionConstraints.MaxEstimatedCostUSD)
                {
                    result.Errors.Add($"预估成本 ${result.EstimatedCostUSD:F2} 超过限制 ${definition.ExecutionConstraints.MaxEstimatedCostUSD:F2}");
                }

                // 检查组合数量
                if (result.EstimatedCombinations == 0)
                {
                    result.Warnings.Add("预估组合数量为0，可能没有匹配的数据");
                }

                result.CanExecute = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"执行前检查时发生错误: {ex.Message}");
                result.CanExecute = false;
            }

            return result;
        }

        public async Task<List<ReasoningTransactionInstance>> GetRunningTransactionsAsync(
            CancellationToken cancellationToken = default)
        {
            var runningStatuses = new[] { TransactionStatus.Pending, TransactionStatus.FetchingData, 
                                        TransactionStatus.CombiningData, TransactionStatus.GeneratingOutputs };
            
            var allInstances = await _instanceService.GetInstancesAsync(cancellationToken: cancellationToken);
            return allInstances.Where(i => runningStatuses.Contains(i.Status)).ToList();
        }

        // 暂未实现的方法
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

        public Task<BatchCancelResult> BatchCancelTransactionsAsync(List<string> instanceIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("批量取消功能将在后续版本实现");
        }

        #region 私有辅助方法

        private async Task ProcessDataAsync(ReasoningTransactionInstance instance, ReasoningTransactionDefinition definition, CancellationToken cancellationToken)
        {
            instance.Status = TransactionStatus.FetchingData;
            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);

            _logger.LogInformation("开始处理数据: {InstanceId}", instance.InstanceId);

            // 使用QueryProcessingService一次性完成数据获取和组合
            var processedData = await _queryProcessingService.GenerateAndResolveInputDataAsync(definition, cancellationToken);
            
            instance.ResolvedViews = processedData.ResolvedViews;
            instance.InputCombinations = processedData.Combinations;
            instance.Metrics.TotalCombinations = processedData.Combinations.Count;

            instance.Status = TransactionStatus.CombiningData;
            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);

            _logger.LogInformation("数据处理完成: {InstanceId}, 视图数: {ViewCount}, 组合数: {CombinationCount}", 
                instance.InstanceId, processedData.Statistics.TotalViews, processedData.Statistics.GeneratedCombinations);
        }

        private async Task GenerateOutputsAsync(ReasoningTransactionInstance instance, ReasoningTransactionDefinition definition, CancellationToken cancellationToken)
        {
            instance.Status = TransactionStatus.GeneratingOutputs;
            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);

            _logger.LogInformation("开始生成AI输出: {InstanceId}, 组合数: {CombinationCount}", 
                instance.InstanceId, instance.InputCombinations.Count);

            // 这里可以调用 ReasoningCombinationService 来处理组合执行
            // 为了简化，暂时跳过具体实现
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

            _logger.LogInformation("AI输出生成完成: {InstanceId}", instance.InstanceId);
        }
        
        /// <summary>
        /// 处理单个组合
        /// </summary>
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
                    await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
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