using System.ComponentModel;

namespace ContentEngine.Core.Inference.Models;

/// <summary>
/// 组合执行状态的扩展信息
/// </summary>
public class CombinationExecutionStatus
{
    /// <summary>
    /// 组合ID
    /// </summary>
    public string CombinationId { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前状态
    /// </summary>
    public CombinationStatus Status { get; set; } = CombinationStatus.Pending;
    
    /// <summary>
    /// 状态更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 开始执行时间
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// 排队位置（用于RPM限制等待）
    /// </summary>
    public int? QueuePosition { get; set; }
    
    /// <summary>
    /// 预估等待时间（毫秒）
    /// </summary>
    public int? EstimatedWaitTimeMs { get; set; }
    
    /// <summary>
    /// 状态描述信息
    /// </summary>
    public string? StatusMessage { get; set; }
    
    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 获取状态的友好显示名称
    /// </summary>
    public string GetStatusDisplayName()
    {
        return Status switch
        {
            CombinationStatus.Pending => "待处理",
            CombinationStatus.Queuing => "排队中",
            CombinationStatus.Executing => "执行中",
            CombinationStatus.Completed => "已完成",
            CombinationStatus.Failed => "执行失败",
            CombinationStatus.Retrying => "重试中",
            _ => "未知状态"
        };
    }
    
    /// <summary>
    /// 获取详细的状态描述
    /// </summary>
    public string GetDetailedStatus()
    {
        var baseStatus = GetStatusDisplayName();
        
        if (!string.IsNullOrEmpty(StatusMessage))
        {
            return $"{baseStatus} - {StatusMessage}";
        }
        
        return Status switch
        {
            CombinationStatus.Queuing when QueuePosition.HasValue => 
                $"{baseStatus} (第{QueuePosition}位)",
            CombinationStatus.Queuing when EstimatedWaitTimeMs.HasValue => 
                $"{baseStatus} (约{EstimatedWaitTimeMs/1000}秒)",
            CombinationStatus.Executing when StartedAt.HasValue => 
                $"{baseStatus} ({(DateTime.UtcNow - StartedAt.Value).TotalSeconds:F0}秒)",
            _ => baseStatus
        };
    }
}

/// <summary>
/// 扩展的组合状态枚举
/// </summary>
public enum CombinationStatus
{
    [Description("待处理")]
    Pending = 0,
    
    [Description("排队中")]
    Queuing = 1,
    
    [Description("执行中")]
    Executing = 2,
    
    [Description("已完成")]
    Completed = 3,
    
    [Description("执行失败")]
    Failed = 4,
    
    [Description("重试中")]
    Retrying = 5
}
