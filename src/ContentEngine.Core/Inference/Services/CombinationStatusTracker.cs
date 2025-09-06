using ContentEngine.Core.Inference.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ContentEngine.Core.Inference.Services;

/// <summary>
/// 组合执行状态跟踪器实现
/// </summary>
public class CombinationStatusTracker : ICombinationStatusTracker
{
    private readonly ConcurrentDictionary<string, CombinationExecutionStatus> _statuses = new();
    private readonly ILogger<CombinationStatusTracker> _logger;

    public CombinationStatusTracker(ILogger<CombinationStatusTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 状态更新事件
    /// </summary>
    public event Action<string, CombinationExecutionStatus>? StatusUpdated;

    public void UpdateStatus(string combinationId, CombinationStatus status, 
        string? statusMessage = null, int? queuePosition = null, int? estimatedWaitTimeMs = null)
    {
        var now = DateTime.UtcNow;
        
        var executionStatus = _statuses.AddOrUpdate(combinationId, 
            // 新建状态
            new CombinationExecutionStatus
            {
                CombinationId = combinationId,
                Status = status,
                UpdatedAt = now,
                StartedAt = status == CombinationStatus.Executing ? now : null,
                CompletedAt = status is CombinationStatus.Completed or CombinationStatus.Failed ? now : null,
                StatusMessage = statusMessage,
                QueuePosition = queuePosition,
                EstimatedWaitTimeMs = estimatedWaitTimeMs
            },
            // 更新现有状态
            (_, existing) =>
            {
                existing.Status = status;
                existing.UpdatedAt = now;
                existing.StatusMessage = statusMessage;
                existing.QueuePosition = queuePosition;
                existing.EstimatedWaitTimeMs = estimatedWaitTimeMs;
                
                // 设置开始执行时间
                if (status == CombinationStatus.Executing && !existing.StartedAt.HasValue)
                {
                    existing.StartedAt = now;
                }
                
                // 设置完成时间
                if (status is CombinationStatus.Completed or CombinationStatus.Failed && !existing.CompletedAt.HasValue)
                {
                    existing.CompletedAt = now;
                }
                
                return existing;
            });

        _logger.LogDebug("组合状态更新: {CombinationId} -> {Status} ({Message})", 
            combinationId, status, statusMessage);

        // 触发状态更新事件
        StatusUpdated?.Invoke(combinationId, executionStatus);
    }

    public CombinationExecutionStatus? GetStatus(string combinationId)
    {
        return _statuses.TryGetValue(combinationId, out var status) ? status : null;
    }

    public Dictionary<string, CombinationExecutionStatus> GetAllStatuses()
    {
        return new Dictionary<string, CombinationExecutionStatus>(_statuses);
    }

    public void ClearStatus(string combinationId)
    {
        if (_statuses.TryRemove(combinationId, out _))
        {
            _logger.LogDebug("已清理组合状态: {CombinationId}", combinationId);
        }
    }

    public void ClearAllStatuses()
    {
        _statuses.Clear();
        _logger.LogInformation("已清理所有组合状态");
    }
}
