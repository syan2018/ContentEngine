using ContentEngine.Core.DataPipeline.Models;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 字段编辑服务接口，处理Schema字段的安全编辑和数据迁移
/// </summary>
public interface IFieldEditService
{
    /// <summary>
    /// 分析字段变更的影响
    /// </summary>
    /// <param name="originalField">原始字段定义</param>
    /// <param name="editedField">编辑后的字段定义</param>
    /// <param name="schemaName">Schema名称</param>
    /// <param name="recordCount">现有记录数</param>
    /// <returns>变更影响分析结果</returns>
    Task<FieldChangeAnalysis> AnalyzeFieldChangeAsync(
        FieldDefinition originalField, 
        FieldDefinition editedField, 
        string schemaName, 
        int recordCount);

    /// <summary>
    /// 获取数据变更预览
    /// </summary>
    /// <param name="originalField">原始字段定义</param>
    /// <param name="editedField">编辑后的字段定义</param>
    /// <param name="schemaName">Schema名称</param>
    /// <param name="previewCount">预览记录数量</param>
    /// <returns>数据变更预览</returns>
    Task<List<DataChangePreview>> GetDataChangePreviewAsync(
        FieldDefinition originalField,
        FieldDefinition editedField,
        string schemaName,
        int previewCount = 5);

    /// <summary>
    /// 执行字段变更和数据迁移
    /// </summary>
    /// <param name="originalField">原始字段定义</param>
    /// <param name="editedField">编辑后的字段定义</param>
    /// <param name="schemaName">Schema名称</param>
    /// <returns>变更执行结果</returns>
    Task<FieldChangeResult> ApplyFieldChangeAsync(
        FieldDefinition originalField,
        FieldDefinition editedField,
        string schemaName);

    /// <summary>
    /// 验证字段变更的兼容性
    /// </summary>
    /// <param name="fromType">原始字段类型</param>
    /// <param name="toType">目标字段类型</param>
    /// <returns>是否兼容</returns>
    bool IsCompatibleTypeConversion(FieldType fromType, FieldType toType);
}

/// <summary>
/// 字段变更分析结果
/// </summary>
public class FieldChangeAnalysis
{
    /// <summary>
    /// 变更详情列表
    /// </summary>
    public List<FieldChangeInfo> Changes { get; set; } = new();

    /// <summary>
    /// 是否为高风险操作
    /// </summary>
    public bool IsHighRisk { get; set; }

    /// <summary>
    /// 风险评估消息
    /// </summary>
    public string RiskAssessment { get; set; } = string.Empty;
}

/// <summary>
/// 字段变更信息
/// </summary>
public class FieldChangeInfo
{
    /// <summary>
    /// 变更标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 变更描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 潜在风险
    /// </summary>
    public string[] Risks { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 系统将执行的操作
    /// </summary>
    public string[] Actions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// 数据变更预览
/// </summary>
public class DataChangePreview
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// 当前值
    /// </summary>
    public string? CurrentValue { get; set; }

    /// <summary>
    /// 变更后的值
    /// </summary>
    public string NewValue { get; set; } = string.Empty;

    /// <summary>
    /// 是否会导致数据丢失
    /// </summary>
    public bool WillLoseData { get; set; }

    /// <summary>
    /// 转换状态
    /// </summary>
    public ConversionStatus Status { get; set; }

    /// <summary>
    /// 错误消息（如果转换失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 转换状态
/// </summary>
public enum ConversionStatus
{
    /// <summary>
    /// 成功
    /// </summary>
    Success,

    /// <summary>
    /// 数据丢失
    /// </summary>
    DataLoss,

    /// <summary>
    /// 转换失败
    /// </summary>
    Failed,

    /// <summary>
    /// 需要手动处理
    /// </summary>
    RequiresManualIntervention
}

/// <summary>
/// 字段变更执行结果
/// </summary>
public class FieldChangeResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 受影响的记录数
    /// </summary>
    public int AffectedRecords { get; set; }

    /// <summary>
    /// 转换失败的记录数
    /// </summary>
    public int FailedConversions { get; set; }

    /// <summary>
    /// 详细日志
    /// </summary>
    public List<string> Logs { get; set; } = new();
} 