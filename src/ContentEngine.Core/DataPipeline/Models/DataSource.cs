namespace ContentEngine.Core.DataPipeline.Models;

/// <summary>
/// 定义数据录入的数据源类型
/// </summary>
public class DataSource
{
    /// <summary>
    /// 数据源唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 数据源类型
    /// </summary>
    public DataSourceType Type { get; set; }

    /// <summary>
    /// 数据源名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数据源内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// MIME类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// URL地址（当类型为URL时）
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 数据源类型枚举
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// 文件
    /// </summary>
    File,

    /// <summary>
    /// 网页URL
    /// </summary>
    Url,

    /// <summary>
    /// 文本内容
    /// </summary>
    Text
} 