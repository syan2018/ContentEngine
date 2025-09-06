using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services;

/// <summary>
/// 组合执行状态跟踪器
/// 用于实时跟踪组合的执行状态
/// </summary>
public interface ICombinationStatusTracker
{
    /// <summary>
    /// 更新组合状态
    /// </summary>
    /// <param name="combinationId">组合ID</param>
    /// <param name="status">新状态</param>
    /// <param name="statusMessage">状态描述</param>
    /// <param name="queuePosition">排队位置</param>
    /// <param name="estimatedWaitTimeMs">预估等待时间</param>
    void UpdateStatus(string combinationId, CombinationStatus status, 
        string? statusMessage = null, int? queuePosition = null, int? estimatedWaitTimeMs = null);

    /// <summary>
    /// 获取组合的当前状态
    /// </summary>
    /// <param name="combinationId">组合ID</param>
    /// <returns>状态信息，如果不存在返回null</returns>
    CombinationExecutionStatus? GetStatus(string combinationId);

    /// <summary>
    /// 获取所有组合的状态
    /// </summary>
    /// <returns>所有状态信息</returns>
    Dictionary<string, CombinationExecutionStatus> GetAllStatuses();

    /// <summary>
    /// 清理指定组合的状态
    /// </summary>
    /// <param name="combinationId">组合ID</param>
    void ClearStatus(string combinationId);

    /// <summary>
    /// 清理所有状态
    /// </summary>
    void ClearAllStatuses();

    /// <summary>
    /// 状态更新事件
    /// </summary>
    event Action<string, CombinationExecutionStatus>? StatusUpdated;
}
