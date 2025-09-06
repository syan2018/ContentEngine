using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services;

/// <summary>
/// 推理进度管理器接口
/// 负责统一管理推理实例的状态和进度信息
/// </summary>
public interface IReasoningProgressManager
{
    /// <summary>
    /// 获取推理实例的当前进度
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>进度信息，如果实例不存在返回null</returns>
    Task<ReasoningProgress?> GetProgressAsync(string instanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 同步获取推理实例的当前进度（缓存）
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <returns>进度信息，如果实例不存在返回null</returns>
    ReasoningProgress? GetProgress(string instanceId);
    
    /// <summary>
    /// 获取所有推理实例的进度摘要
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>进度摘要列表</returns>
    Task<List<ReasoningProgressSummary>> GetAllProgressSummariesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 刷新推理实例的状态和进度
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有状态变化</returns>
    Task<bool> RefreshInstanceProgressAsync(string instanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查并更新所有活跃推理实例的状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>有状态变化的实例数量</returns>
    Task<int> RefreshAllActiveInstancesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 进度更新事件
    /// </summary>
    event Action<string, ReasoningProgress>? ProgressUpdated;
    
    /// <summary>
    /// 实例状态变化事件
    /// </summary>
    event Action<string, TransactionStatus, TransactionStatus>? StatusChanged;
}
