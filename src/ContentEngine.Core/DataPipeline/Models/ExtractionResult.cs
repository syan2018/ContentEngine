using LiteDB;

namespace ContentEngine.Core.DataPipeline.Models;

/// <summary>
/// AI提取结果
/// </summary>
public class ExtractionResult
{
    /// <summary>
    /// 数据源ID
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// 提取的记录列表
    /// </summary>
    public List<BsonDocument> Records { get; set; } = new List<BsonDocument>();

    /// <summary>
    /// 提取状态
    /// </summary>
    public ExtractionStatus Status { get; set; } = ExtractionStatus.Pending;

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 处理开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 处理完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 提取状态枚举
/// </summary>
public enum ExtractionStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending,

    /// <summary>
    /// 处理中
    /// </summary>
    Processing,

    /// <summary>
    /// 成功
    /// </summary>
    Success,

    /// <summary>
    /// 失败
    /// </summary>
    Error
}

/// <summary>
/// 提取模式枚举
/// </summary>
public enum ExtractionMode
{
    /// <summary>
    /// 一对一提取（每个数据源生成一条记录）
    /// </summary>
    OneToOne,

    /// <summary>
    /// 批量提取（从每个数据源提取多条记录）
    /// </summary>
    Batch
} 