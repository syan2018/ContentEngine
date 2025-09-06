using System.ComponentModel;

namespace ContentEngine.Core.Inference.Models;

/// <summary>
/// 推理事务进度信息
/// </summary>
public class ReasoningProgress
{
    /// <summary>
    /// 实例ID
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前状态
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    
    /// <summary>
    /// 总组合数
    /// </summary>
    public int TotalCombinations { get; set; }
    
    /// <summary>
    /// 已完成数
    /// </summary>
    public int CompletedCount { get; set; }
    
    /// <summary>
    /// 失败数
    /// </summary>
    public int FailedCount { get; set; }
    
    /// <summary>
    /// 执行中数量
    /// </summary>
    public int ExecutingCount { get; set; }
    
    /// <summary>
    /// 排队中数量
    /// </summary>
    public int QueueingCount { get; set; }
    
    /// <summary>
    /// 待处理数量
    /// </summary>
    public int PendingCount { get; set; }
    
    /// <summary>
    /// 总成本（USD）
    /// </summary>
    public decimal TotalCost { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>
    /// 预估剩余时间（毫秒）
    /// </summary>
    public long? EstimatedTimeRemainingMs { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 完成时间（如果已完成）
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 计算完成百分比
    /// </summary>
    public double CompletionPercentage => 
        TotalCombinations == 0 ? 0 : (double)CompletedCount / TotalCombinations * 100;
        
    /// <summary>
    /// 计算总体进度百分比（包括执行中的）
    /// </summary>
    public double OverallProgress => 
        TotalCombinations == 0 ? 0 : (double)(CompletedCount + FailedCount + ExecutingCount * 0.5) / TotalCombinations * 100;
    
    /// <summary>
    /// 计算成功率
    /// </summary>
    public double SuccessRate => 
        (CompletedCount + FailedCount) == 0 ? 0 : (double)CompletedCount / (CompletedCount + FailedCount) * 100;
        
    /// <summary>
    /// 总执行时间
    /// </summary>
    public TimeSpan ElapsedTime => 
        (CompletedAt ?? DateTime.UtcNow) - StartedAt;

    /// <summary>
    /// 是否已完成（成功或失败）
    /// </summary>
    public bool IsCompleted => Status is TransactionStatus.Completed or TransactionStatus.Failed;
    
    /// <summary>
    /// 是否正在执行中（真正有任务在运行）
    /// </summary>
    public bool IsActive => ExecutingCount > 0 || QueueingCount > 0;
    
    /// <summary>
    /// 是否有未完成的工作（包括待处理任务）
    /// </summary>
    public bool HasPendingWork => PendingCount > 0 || FailedCount > 0;
    
    /// <summary>
    /// 是否可以继续执行（有未完成工作但当前不在执行）
    /// </summary>
    public bool CanContinue => !IsActive && HasPendingWork && CompletedCount > 0;

    /// <summary>
    /// 获取状态显示文本
    /// </summary>
    public string GetStatusDisplayText()
    {
        return Status switch
        {
            TransactionStatus.Pending => "等待开始",
            TransactionStatus.FetchingData => "获取数据中",
            TransactionStatus.CombiningData => "生成组合中",
            TransactionStatus.GeneratingOutputs => GetGeneratingStatusText(),
            TransactionStatus.Completed => "已完成",
            TransactionStatus.Failed => "执行失败",
            TransactionStatus.Paused => "已暂停",
            _ => "未知状态"
        };
    }

    /// <summary>
    /// 获取详细进度描述
    /// </summary>
    public string GetDetailedProgressText()
    {
        if (TotalCombinations == 0)
            return "暂无任务";
            
        return $"{CompletedCount}/{TotalCombinations} 已完成 ({CompletionPercentage:F1}%)";
    }

    private string GetGeneratingStatusText()
    {
        if (ExecutingCount > 0)
            return $"执行中 ({ExecutingCount} 个任务)";
        if (QueueingCount > 0)
            return $"排队中 ({QueueingCount} 个任务)";
        if (PendingCount > 0)
            return $"待处理 ({PendingCount} 个任务)";
        return "生成输出中";
    }
}

/// <summary>
/// 推理进度摘要
/// </summary>
public class ReasoningProgressSummary
{
    /// <summary>
    /// 实例ID
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;
    
    /// <summary>
    /// 定义名称
    /// </summary>
    public string DefinitionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前状态
    /// </summary>
    public TransactionStatus Status { get; set; }
    
    /// <summary>
    /// 完成百分比
    /// </summary>
    public double CompletionPercentage { get; set; }
    
    /// <summary>
    /// 状态描述
    /// </summary>
    public string StatusText { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否正在执行
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
