using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.DataPipeline.Models;
using ContentEngine.Core.DataPipeline.Services;
using ContentEngine.Core.Storage;
using LiteDB;
using Microsoft.Extensions.Logging;

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
    /// 根据推理定义生成所有可能的输入组合
    /// </summary>
    public async Task<List<ReasoningInputCombination>> GenerateInputCombinationsAsync(ReasoningTransactionDefinition definition)
    {
        var combinations = new List<ReasoningInputCombination>();
        
        try
        {
            // 1. 执行所有查询，获取数据视图
            var viewData = new Dictionary<string, List<BsonDocument>>();
            
            foreach (var query in definition.QueryDefinitions)
            {
                var queryResult = await ExecuteQueryAsync(query);
                viewData[query.OutputViewName] = queryResult;
                _logger.LogInformation($"查询 {query.OutputViewName} 返回 {queryResult.Count} 条记录");
            }

            // 2. 根据组合规则生成叉积
            foreach (var rule in definition.DataCombinationRules)
            {
                var ruleCombinations = GenerateCombinationsForRule(rule, viewData);
                combinations.AddRange(ruleCombinations);
            }

            // 3. 如果没有定义组合规则，则创建默认的完全叉积
            if (!definition.DataCombinationRules.Any() && viewData.Any())
            {
                var defaultCombinations = GenerateDefaultCombinations(viewData);
                combinations.AddRange(defaultCombinations);
            }

            _logger.LogInformation($"总共生成 {combinations.Count} 个输入组合");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成输入组合时发生错误");
            throw;
        }

        return combinations;
    }

    /// <summary>
    /// 执行单个查询定义
    /// </summary>
    public async Task<List<BsonDocument>> ExecuteQueryAsync(QueryDefinition query)
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
            
            return results;
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
    public async Task<QueryStatistics> GetQueryStatisticsAsync(QueryDefinition query)
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

            return new QueryStatistics
            {
                TotalRecords = totalRecords,
                FilteredRecords = filteredRecords,
                AvailableFields = availableFields,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取查询统计信息时发生错误");
            return new QueryStatistics();
        }
    }

    /// <summary>
    /// 预览查询结果
    /// </summary>
    public async Task<List<BsonDocument>> PreviewQueryAsync(QueryDefinition query, int maxResults = 10)
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

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预览查询结果时发生错误");
            return new List<BsonDocument>();
        }
    }

    /// <summary>
    /// 验证查询定义
    /// </summary>
    public async Task<QueryValidationResult> ValidateQueryAsync(QueryDefinition query)
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
        Dictionary<string, List<BsonDocument>> viewData)
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
            viewData[viewName].Select((doc, index) => new { ViewName = viewName, Data = doc, Index = index })
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
                if (viewData.ContainsKey(singletonViewName) && viewData[singletonViewName].Any())
                {
                    inputCombination.DataMap[singletonViewName] = viewData[singletonViewName].First();
                }
            }

            combinations.Add(inputCombination);
        }

        return combinations;
    }

    /// <summary>
    /// 生成默认组合（当没有指定组合规则时）
    /// </summary>
    private List<ReasoningInputCombination> GenerateDefaultCombinations(Dictionary<string, List<BsonDocument>> viewData)
    {
        var combinations = new List<ReasoningInputCombination>();

        if (viewData.Count == 1)
        {
            // 单一视图，为每条记录创建一个组合
            var viewName = viewData.Keys.First();
            var documents = viewData[viewName];

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
                kvp.Value.Select(doc => new { ViewName = kvp.Key, Data = doc })
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