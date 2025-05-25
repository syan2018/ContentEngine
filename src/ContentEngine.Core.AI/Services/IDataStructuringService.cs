using ContentEngine.Core.DataPipeline.Models;
using LiteDB;

namespace ContentEngine.Core.AI.Services;

/// <summary>
/// 数据结构化服务接口
/// </summary>
public interface IDataStructuringService
{
    /// <summary>
    /// 从数据源提取结构化数据
    /// </summary>
    /// <param name="schema">目标Schema定义</param>
    /// <param name="dataSources">数据源列表</param>
    /// <param name="extractionMode">提取模式</param>
    /// <param name="fieldMappings">字段映射配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提取结果列表</returns>
    Task<List<ExtractionResult>> ExtractDataAsync(
        SchemaDefinition schema,
        List<DataSource> dataSources,
        ExtractionMode extractionMode,
        Dictionary<string, string> fieldMappings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证提取的数据是否符合Schema要求
    /// </summary>
    /// <param name="schema">Schema定义</param>
    /// <param name="records">要验证的记录</param>
    /// <returns>验证结果</returns>
    Task<List<ValidationResult>> ValidateRecordsAsync(
        SchemaDefinition schema,
        List<BsonDocument> records);
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 记录索引
    /// </summary>
    public int RecordIndex { get; set; }

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();
} 