namespace ContentEngine.Core.DataPipeline.Models;

/// <summary>
/// 定义 Schema 中的单个字段
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// 字段名称 (在 Schema 内应唯一)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段的数据类型
    /// </summary>
    public FieldType Type { get; set; }

    /// <summary>
    /// 字段是否为必填项
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 如果字段类型是 Reference，此属性指定引用的 Schema 名称
    /// </summary>
    public string? ReferenceSchemaName { get; set; }

    /// <summary>
    /// 字段备注/说明
    /// </summary>
    public string? Comment { get; set; }

    // 可以根据需要添加其他属性，例如：
    // public object DefaultValue { get; set; }
    // public string ValidationRegex { get; set; }
} 