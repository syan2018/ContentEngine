namespace ContentEngine.Core.DataPipeline.Models;

/// <summary>
/// 表格导入数据
/// </summary>
public class TableImportData
{
    /// <summary>
    /// 表头列名
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// 数据行
    /// </summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>
    /// 工作表名称（Excel文件可能有多个工作表）
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 总行数（包含表头）
    /// </summary>
    public int TotalRows => Rows.Count + (Headers.Any() ? 1 : 0);

    /// <summary>
    /// 总列数
    /// </summary>
    public int TotalColumns => Headers.Count;
}

/// <summary>
/// 字段映射配置
/// </summary>
public class FieldMapping
{
    /// <summary>
    /// Schema字段名
    /// </summary>
    public string SchemaFieldName { get; set; } = string.Empty;

    /// <summary>
    /// 表格列名
    /// </summary>
    public string TableColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列索引
    /// </summary>
    public int ColumnIndex { get; set; } = -1;

    /// <summary>
    /// 是否映射
    /// </summary>
    public bool IsMapped { get; set; } = false;

    /// <summary>
    /// 数据转换规则（可选）
    /// </summary>
    public string? TransformRule { get; set; }
}

/// <summary>
/// 表格导入配置
/// </summary>
public class TableImportConfig
{
    /// <summary>
    /// 字段映射列表
    /// </summary>
    public List<FieldMapping> FieldMappings { get; set; } = new();

    /// <summary>
    /// 是否跳过第一行（表头）
    /// </summary>
    public bool SkipHeaderRow { get; set; } = true;

    /// <summary>
    /// 开始导入的行号（从0开始）
    /// </summary>
    public int StartRowIndex { get; set; } = 1;

    /// <summary>
    /// 结束导入的行号（-1表示到最后一行）
    /// </summary>
    public int EndRowIndex { get; set; } = -1;

    /// <summary>
    /// 是否忽略空行
    /// </summary>
    public bool IgnoreEmptyRows { get; set; } = true;
} 