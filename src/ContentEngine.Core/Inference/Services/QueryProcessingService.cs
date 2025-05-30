using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.DataPipeline.Models;
using ContentEngine.Core.DataPipeline.Services;
using ContentEngine.Core.Storage;
using LiteDB;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ContentEngine.Core.Inference.Services;

/// <summary>
/// Query处理服务实现
/// </summary>
public class QueryProcessingService : IQueryProcessingService
{
    private readonly LiteDbContext _dbContext;
    private readonly ISchemaDefinitionService _schemaService;
    private readonly ILogger<QueryProcessingService> _logger;

    public QueryProcessingService(
        LiteDbContext dbContext,
        ISchemaDefinitionService schemaService,
        ILogger<QueryProcessingService> logger)
    {
        _dbContext = dbContext;
        _schemaService = schemaService;
        _logger = logger;
    }

    /// <summary>
    /// 生成和解析输入数据（包含视图和组合）
    /// </summary>
    public async Task<ProcessedInputData> GenerateAndResolveInputDataAsync(
        ReasoningTransactionDefinition definition, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ProcessedInputData();
        
        try
        {
            _logger.LogInformation("开始生成和解析输入数据，定义ID: {DefinitionId}", definition.Id);

            // 1. 执行所有查询，获取数据视图
            var totalRecords = 0;
            foreach (var query in definition.QueryDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var queryResult = await ExecuteQueryAsync(query, cancellationToken);
                var viewData = new ViewData
                {
                    ViewName = query.OutputViewName,
                    Items = queryResult
                };
                
                result.ResolvedViews[query.OutputViewName] = viewData;
                totalRecords += queryResult.Count;
                
                _logger.LogInformation("查询 {ViewName} 返回 {Count} 条记录", 
                    query.OutputViewName, queryResult.Count);
            }

            // 2. 根据组合规则生成叉积
            var allCombinations = new List<ReasoningInputCombination>();
            
            foreach (var rule in definition.DataCombinationRules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ruleCombinations = GenerateCombinationsForRule(rule, result.ResolvedViews);
                allCombinations.AddRange(ruleCombinations);
            }

            // 3. 如果没有定义组合规则，则创建默认的完全叉积
            if (!definition.DataCombinationRules.Any() && result.ResolvedViews.Any())
            {
                var defaultCombinations = GenerateDefaultCombinations(result.ResolvedViews);
                allCombinations.AddRange(defaultCombinations);
            }

            result.Combinations = allCombinations;

            // 4. 设置统计信息
            stopwatch.Stop();
            result.Statistics = new ProcessingStatistics
            {
                TotalViews = result.ResolvedViews.Count,
                TotalRecords = totalRecords,
                GeneratedCombinations = allCombinations.Count,
                ProcessingTime = stopwatch.Elapsed
            };

            _logger.LogInformation("输入数据生成完成，视图数: {ViewCount}, 记录数: {RecordCount}, 组合数: {CombinationCount}, 耗时: {ElapsedTime}ms",
                result.Statistics.TotalViews, result.Statistics.TotalRecords, 
                result.Statistics.GeneratedCombinations, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成和解析输入数据时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 根据推理定义生成所有可能的输入组合
    /// </summary>
    public async Task<List<ReasoningInputCombination>> GenerateInputCombinationsAsync(
        ReasoningTransactionDefinition definition, 
        CancellationToken cancellationToken = default)
    {
        var processedData = await GenerateAndResolveInputDataAsync(definition, cancellationToken);
        return processedData.Combinations;
    }

    /// <summary>
    /// 预估推理定义的组合数量
    /// </summary>
    public async Task<int> EstimateCombinationCountAsync(
        ReasoningTransactionDefinition definition, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始预估组合数量，定义ID: {DefinitionId}", definition.Id);

            // 1. 获取每个查询的统计信息
            var queryStats = new Dictionary<string, int>();
            foreach (var query in definition.QueryDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var stats = await GetQueryStatisticsAsync(query, cancellationToken);
                queryStats[query.OutputViewName] = stats.FilteredRecords;
                
                _logger.LogDebug("查询 {ViewName} 预估记录数: {RecordCount}", 
                    query.OutputViewName, stats.FilteredRecords);
            }

            // 2. 根据组合规则计算预估组合数
            var totalEstimatedCombinations = 0;
            
            foreach (var rule in definition.DataCombinationRules)
            {
                var estimatedForRule = EstimateCombinationsForRule(rule, queryStats);
                totalEstimatedCombinations += estimatedForRule;
            }

            // 3. 如果没有定义组合规则，使用默认估算
            if (!definition.DataCombinationRules.Any() && queryStats.Any())
            {
                totalEstimatedCombinations = EstimateDefaultCombinations(queryStats);
            }

            _logger.LogInformation("预估组合数量完成: {EstimatedCount}", totalEstimatedCombinations);
            return totalEstimatedCombinations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预估组合数量时发生错误");
            return 0;
        }
    }

    /// <summary>
    /// 只解析视图数据，不生成组合
    /// </summary>
    public async Task<Dictionary<string, ViewData>> ResolveViewDataAsync(
        ReasoningTransactionDefinition definition, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始解析视图数据，定义ID: {DefinitionId}", definition.Id);

            var resolvedViews = new Dictionary<string, ViewData>();
            
            // 执行所有查询，获取数据视图
            foreach (var query in definition.QueryDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var queryResult = await ExecuteQueryAsync(query, cancellationToken);
                var viewData = new ViewData
                {
                    ViewName = query.OutputViewName,
                    Items = queryResult
                };
                
                resolvedViews[query.OutputViewName] = viewData;
                
                _logger.LogInformation("查询 {ViewName} 返回 {Count} 条记录", 
                    query.OutputViewName, queryResult.Count);
            }

            _logger.LogInformation("视图数据解析完成，视图数: {ViewCount}", resolvedViews.Count);
            return resolvedViews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析视图数据时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 执行单个查询定义
    /// </summary>
    public async Task<List<BsonDocument>> ExecuteQueryAsync(
        QueryDefinition query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _dbContext.GetDataCollection(query.SourceSchemaName);
            
            // 构建查询
            var dbQuery = collection.Query();
            
            // 应用筛选条件
            if (!string.IsNullOrWhiteSpace(query.FilterExpression))
            {
                dbQuery = dbQuery.Where(query.FilterExpression);
            }
            
            // 执行查询
            var results = dbQuery.ToList();
            
            // 选择字段
            if (query.SelectFields.Any())
            {
                results = SelectFields(results, query.SelectFields);
            }

            _logger.LogInformation("查询 {OutputViewName} 从 {SourceSchemaName} 返回 {Count} 条记录", 
                query.OutputViewName, query.SourceSchemaName, results.Count);
            
            return await Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行查询 {QueryId} 时发生错误", query.QueryId);
            throw;
        }
    }

    /// <summary>
    /// 获取查询统计信息
    /// </summary>
    public async Task<QueryStatistics> GetQueryStatisticsAsync(
        QueryDefinition query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _dbContext.GetDataCollection(query.SourceSchemaName);
            
            var totalRecords = collection.Count();
            
            var filteredRecords = totalRecords;
            if (!string.IsNullOrWhiteSpace(query.FilterExpression))
            {
                filteredRecords = collection.Query().Where(query.FilterExpression).Count();
            }
            
            // 获取可用字段（从第一条记录中提取）
            var availableFields = new List<string>();
            var firstRecord = collection.Query().FirstOrDefault();
            if (firstRecord != null)
            {
                availableFields = firstRecord.Keys.ToList();
            }

            return await Task.FromResult(new QueryStatistics
            {
                TotalRecords = totalRecords,
                FilteredRecords = filteredRecords,
                AvailableFields = availableFields,
                LastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取查询统计信息时发生错误");
            return await Task.FromResult(new QueryStatistics());
        }
    }

    /// <summary>
    /// 预览查询结果
    /// </summary>
    public async Task<List<BsonDocument>> PreviewQueryAsync(
        QueryDefinition query, 
        int maxResults = 10, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _dbContext.GetDataCollection(query.SourceSchemaName);
            
            var dbQuery = collection.Query();
            
            if (!string.IsNullOrWhiteSpace(query.FilterExpression))
            {
                dbQuery = dbQuery.Where(query.FilterExpression);
            }
            
            var results = dbQuery.Limit(maxResults).ToList();
            
            if (query.SelectFields.Any())
            {
                results = SelectFields(results, query.SelectFields);
            }

            return await Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预览查询结果时发生错误");
            return await Task.FromResult(new List<BsonDocument>());
        }
    }

    /// <summary>
    /// 验证查询定义
    /// </summary>
    public async Task<QueryValidationResult> ValidateQueryAsync(
        QueryDefinition query, 
        CancellationToken cancellationToken = default)
    {
        var result = new QueryValidationResult { IsValid = true };

        try
        {
            // 检查基本字段
            if (string.IsNullOrWhiteSpace(query.OutputViewName))
            {
                result.Errors.Add("输出视图名称不能为空");
            }

            if (string.IsNullOrWhiteSpace(query.SourceSchemaName))
            {
                result.Errors.Add("源Schema名称不能为空");
            }

            // 检查Schema是否存在
            var schema = await _schemaService.GetSchemaByNameAsync(query.SourceSchemaName);
            if (schema == null)
            {
                result.Errors.Add($"Schema '{query.SourceSchemaName}' 不存在");
            }
            else
            {
                // 检查选择的字段是否存在
                var schemaFields = schema.Fields.Select(f => f.Name).ToList();
                var invalidFields = query.SelectFields.Where(f => !schemaFields.Contains(f)).ToList();
                
                if (invalidFields.Any())
                {
                    result.Warnings.Add($"以下字段在Schema中不存在: {string.Join(", ", invalidFields)}");
                }
            }

            // 测试筛选表达式
            if (!string.IsNullOrWhiteSpace(query.FilterExpression))
            {
                try
                {
                    var collection = _dbContext.GetDataCollection(query.SourceSchemaName);
                    collection.Query().Where(query.FilterExpression).Limit(1).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"筛选表达式无效: {ex.Message}");
                }
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            result.Errors.Add($"验证过程中发生错误: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    #region 私有方法

    /// <summary>
    /// 为指定规则生成组合
    /// </summary>
    private List<ReasoningInputCombination> GenerateCombinationsForRule(
        DataCombinationRule rule, 
        Dictionary<string, ViewData> viewData)
    {
        var combinations = new List<ReasoningInputCombination>();

        // 获取需要叉积的视图数据
        var crossProductViews = rule.ViewNamesToCrossProduct
            .Where(viewName => viewData.ContainsKey(viewName))
            .ToList();

        if (!crossProductViews.Any())
        {
            _logger.LogWarning("没有找到可用于叉积的视图数据");
            return combinations;
        }

        // 生成叉积组合
        var crossProductData = crossProductViews.Select(viewName => 
            viewData[viewName].Items.Select((doc, index) => new { ViewName = viewName, Data = doc, Index = index })
        ).ToList();

        var cartesianProduct = GetCartesianProduct(crossProductData);

        foreach (var combination in cartesianProduct.Take(rule.MaxCombinations))
        {
            var inputCombination = new ReasoningInputCombination
            {
                CombinationId = Guid.NewGuid().ToString(),
                DataMap = new Dictionary<string, BsonDocument>()
            };

            // 添加叉积数据
            foreach (var item in combination)
            {
                inputCombination.DataMap[item.ViewName] = item.Data;
            }

            // 添加单例上下文数据
            foreach (var singletonViewName in rule.SingletonViewNamesForContext)
            {
                if (viewData.ContainsKey(singletonViewName) && viewData[singletonViewName].Items.Any())
                {
                    inputCombination.DataMap[singletonViewName] = viewData[singletonViewName].Items.First();
                }
            }

            combinations.Add(inputCombination);
        }

        return combinations;
    }

    /// <summary>
    /// 生成默认组合（当没有指定组合规则时）
    /// </summary>
    private List<ReasoningInputCombination> GenerateDefaultCombinations(Dictionary<string, ViewData> viewData)
    {
        var combinations = new List<ReasoningInputCombination>();

        if (viewData.Count == 1)
        {
            // 单一视图，为每条记录创建一个组合
            var viewName = viewData.Keys.First();
            var documents = viewData[viewName].Items;

            foreach (var doc in documents)
            {
                combinations.Add(new ReasoningInputCombination
                {
                    CombinationId = Guid.NewGuid().ToString(),
                    DataMap = new Dictionary<string, BsonDocument> { [viewName] = doc }
                });
            }
        }
        else
        {
            // 多个视图，创建完全叉积（限制数量）
            var viewList = viewData.Select(kvp => 
                kvp.Value.Items.Select(doc => new { ViewName = kvp.Key, Data = doc })
            ).ToList();

            var cartesianProduct = GetCartesianProduct(viewList);

            foreach (var combination in cartesianProduct.Take(1000)) // 默认限制1000个组合
            {
                var inputCombination = new ReasoningInputCombination
                {
                    CombinationId = Guid.NewGuid().ToString(),
                    DataMap = combination.ToDictionary(item => item.ViewName, item => item.Data)
                };

                combinations.Add(inputCombination);
            }
        }

        return combinations;
    }

    /// <summary>
    /// 预估指定规则的组合数量
    /// </summary>
    private int EstimateCombinationsForRule(DataCombinationRule rule, Dictionary<string, int> queryStats)
    {
        if (!rule.ViewNamesToCrossProduct.Any())
        {
            return 1; // 如果没有叉积视图，只有一个组合
        }

        var estimatedCombinations = 1;
        foreach (var viewName in rule.ViewNamesToCrossProduct)
        {
            if (queryStats.TryGetValue(viewName, out var recordCount))
            {
                estimatedCombinations *= Math.Max(1, recordCount); // 避免乘以0
            }
        }

        return Math.Min(estimatedCombinations, rule.MaxCombinations);
    }

    /// <summary>
    /// 预估默认组合数量
    /// </summary>
    private int EstimateDefaultCombinations(Dictionary<string, int> queryStats)
    {
        if (queryStats.Count == 1)
        {
            return queryStats.Values.First();
        }
        else
        {
            var estimatedCombinations = 1;
            foreach (var recordCount in queryStats.Values)
            {
                estimatedCombinations *= Math.Max(1, recordCount);
            }
            return Math.Min(estimatedCombinations, 1000); // 默认限制1000个组合
        }
    }

    /// <summary>
    /// 获取笛卡尔积
    /// </summary>
    private static IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
    {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
        
        return sequences.Aggregate(emptyProduct, (accumulator, sequence) =>
            from accseq in accumulator
            from item in sequence
            select accseq.Concat(new[] { item })
        );
    }

    /// <summary>
    /// 选择指定字段
    /// </summary>
    private List<BsonDocument> SelectFields(List<BsonDocument> documents, List<string> selectFields)
    {
        var result = new List<BsonDocument>();
        
        foreach (var doc in documents)
        {
            var newDoc = new BsonDocument();
            
            foreach (var field in selectFields)
            {
                if (doc.ContainsKey(field))
                {
                    newDoc[field] = doc[field];
                }
            }
            
            result.Add(newDoc);
        }
        
        return result;
    }

    #endregion
} 