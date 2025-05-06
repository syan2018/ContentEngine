namespace ContentEngine.Core.DataPipeline.Models;

/// <summary>
/// 定义 Schema 字段支持的数据类型
/// </summary>
public enum FieldType
{
    /// <summary>
    /// 文本字符串
    /// </summary>
    Text,

    /// <summary>
    /// 数值 (整数或浮点数)
    /// </summary>
    Number,

    /// <summary>
    /// 布尔值 (真/假)
    /// </summary>
    Boolean,

    /// <summary>
    /// 日期和时间
    /// </summary>
    Date,

    /// <summary>
    /// 引用另一个 Schema 的实例
    /// </summary>
    Reference
} 