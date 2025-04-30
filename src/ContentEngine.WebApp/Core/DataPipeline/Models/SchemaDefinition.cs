namespace ContentEngine.WebApp.Core.DataPipeline.Models;

using System.Collections.Generic;

/// <summary>
/// 定义一个用户可定制的数据结构模板 (Schema)
/// </summary>
public class SchemaDefinition
{
    /// <summary>
    /// LiteDB 的唯一标识符
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Schema 的名称 (应唯一，用作集合名称和引用标识)
    /// 例如："CharacterCard", "EventTemplate"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Schema 的描述信息
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 定义此 Schema 包含的字段列表
    /// </summary>
    public List<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();

    /// <summary>
    /// Schema 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Schema 最后修改时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 可以根据需要添加其他元数据，例如：
    // public int Version { get; set; } = 1;
    // public string Icon { get; set; } // 用于 UI 显示的图标
} 