using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.DataPipeline.Models;
using LiteDB;

namespace ContentEngine.Core.Inference.Services;

/// <summary>
/// Query处理服务接口
/// 负责查询执行、组合生成和数据处理
/// </summary>
public interface IQueryProcessingService
{
    /// <summary>
    /// 根据推理定义生成所有可能的输入组合
    /// </summary>
    /// <param name="definition">推理定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>输入组合列表</returns>
    Task<List<ReasoningInputCombination>> GenerateInputCombinationsAsync(
        ReasoningTransactionDefinition definition, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成和解析输入数据（包含视图和组合）
    /// </summary>
    /// <param name="definition">推理定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理后的输入数据</returns>
    Task<ProcessedInputData> GenerateAndResolveInputDataAsync(
        ReasoningTransactionDefinition definition, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行单个查询定义，获取数据
    /// </summary>
    /// <param name="query">查询定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果数据</returns>
    Task<List<BsonDocument>> ExecuteQueryAsync(
        QueryDefinition query, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取查询的数据统计信息
    /// </summary>
    /// <param name="query">查询定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<QueryStatistics> GetQueryStatisticsAsync(
        QueryDefinition query, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 预览查询结果（限制数量）
    /// </summary>
    /// <param name="query">查询定义</param>
    /// <param name="maxResults">最大结果数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预览数据</returns>
    Task<List<BsonDocument>> PreviewQueryAsync(
        QueryDefinition query, 
        int maxResults = 10, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 验证查询定义的有效性
    /// </summary>
    /// <param name="query">查询定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<QueryValidationResult> ValidateQueryAsync(
        QueryDefinition query, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 预估推理定义的组合数量
    /// </summary>
    /// <param name="definition">推理定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预估的组合数量</returns>
    Task<int> EstimateCombinationCountAsync(
        ReasoningTransactionDefinition definition, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 查询统计信息
/// </summary>
public class QueryStatistics
{
    public int TotalRecords { get; set; }
    public int FilteredRecords { get; set; }
    public List<string> AvailableFields { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// 查询验证结果
/// </summary>
public class QueryValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
} 