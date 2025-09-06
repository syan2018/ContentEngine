using ContentEngine.Core.Inference.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ContentEngine.Core.Inference.Services;

/// <summary>
/// 推理进度管理器实现
/// </summary>
public class ReasoningProgressManager : IReasoningProgressManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICombinationStatusTracker _statusTracker;
    private readonly ILogger<ReasoningProgressManager> _logger;
    
    // 缓存推理进度信息
    private readonly ConcurrentDictionary<string, ReasoningProgress> _progressCache = new();
    private readonly object _lock = new object();

    public ReasoningProgressManager(
        IServiceProvider serviceProvider,
        ICombinationStatusTracker statusTracker,
        ILogger<ReasoningProgressManager> logger)
    {
        _serviceProvider = serviceProvider;
        _statusTracker = statusTracker;
        _logger = logger;
        
        // 订阅组合状态变化，用于更新推理进度
        _statusTracker.StatusUpdated += OnCombinationStatusUpdated;
    }

    /// <summary>
    /// 进度更新事件
    /// </summary>
    public event Action<string, ReasoningProgress>? ProgressUpdated;
    
    /// <summary>
    /// 实例状态变化事件
    /// </summary>
    public event Action<string, TransactionStatus, TransactionStatus>? StatusChanged;

    public ReasoningProgress? GetProgress(string instanceId)
    {
        return _progressCache.GetValueOrDefault(instanceId);
    }

    public async Task<ReasoningProgress?> GetProgressAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 先尝试从缓存获取
            if (_progressCache.TryGetValue(instanceId, out var cachedProgress) && 
                DateTime.UtcNow - cachedProgress.UpdatedAt < TimeSpan.FromSeconds(5))
            {
                return cachedProgress;
            }

            // 从数据库获取最新状态
            await RefreshInstanceProgressAsync(instanceId, cancellationToken);
            
            return _progressCache.TryGetValue(instanceId, out var updatedProgress) ? updatedProgress : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推理进度失败: {InstanceId}", instanceId);
            return null;
        }
    }

    public async Task<List<ReasoningProgressSummary>> GetAllProgressSummariesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var instanceService = scope.ServiceProvider.GetRequiredService<IReasoningInstanceService>();
            var definitionService = scope.ServiceProvider.GetRequiredService<IReasoningDefinitionService>();
            
            var instances = await instanceService.GetInstancesAsync(cancellationToken: cancellationToken);
            var summaries = new List<ReasoningProgressSummary>();
            
            foreach (var instance in instances.Take(50)) // 限制数量避免性能问题
            {
                var progress = await GetProgressAsync(instance.InstanceId, cancellationToken);
                if (progress != null)
                {
                    var definition = await definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
                    
                    summaries.Add(new ReasoningProgressSummary
                    {
                        InstanceId = instance.InstanceId,
                        DefinitionName = definition?.Name ?? "未知定义",
                        Status = progress.Status,
                        CompletionPercentage = progress.CompletionPercentage,
                        StatusText = progress.GetStatusDisplayText(),
                        IsActive = progress.IsActive,
                        UpdatedAt = progress.UpdatedAt
                    });
                }
            }
            
            return summaries.OrderByDescending(s => s.UpdatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有推理进度摘要失败");
            return new List<ReasoningProgressSummary>();
        }
    }

    public async Task<bool> RefreshInstanceProgressAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var instanceService = scope.ServiceProvider.GetRequiredService<IReasoningInstanceService>();
            
            var instance = await instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                _progressCache.TryRemove(instanceId, out _);
                return false;
            }

            var oldProgress = _progressCache.TryGetValue(instanceId, out var cached) ? cached : null;
            var oldStatus = oldProgress?.Status ?? TransactionStatus.Pending;

            // 获取组合状态统计
            var combinationStatuses = _statusTracker.GetAllStatuses();
            var instanceCombinations = instance.InputCombinations.Select(c => c.CombinationId).ToList();

            var executingCount = instanceCombinations.Count(id => 
                combinationStatuses.TryGetValue(id, out var status) && status.Status == CombinationStatus.Executing);
            var queuingCount = instanceCombinations.Count(id => 
                combinationStatuses.TryGetValue(id, out var status) && status.Status == CombinationStatus.Queuing);
            var completedCount = instance.Outputs.Count(o => o.IsSuccess);
            var failedCount = instance.Outputs.Count(o => !o.IsSuccess);
            var pendingCount = instanceCombinations.Count - completedCount - failedCount - executingCount - queuingCount;

            // 智能状态判断
            var newStatus = DetermineInstanceStatus(instance, executingCount, queuingCount, pendingCount, completedCount, failedCount);

            // 创建新的进度对象
            var progress = new ReasoningProgress
            {
                InstanceId = instanceId,
                Status = newStatus,
                TotalCombinations = instance.InputCombinations.Count,
                CompletedCount = completedCount,
                FailedCount = failedCount,
                ExecutingCount = executingCount,
                QueueingCount = queuingCount,
                PendingCount = Math.Max(0, pendingCount),
                TotalCost = instance.Outputs.Where(o => o.IsSuccess).Sum(o => o.CostUSD),
                StartedAt = instance.StartedAt,
                UpdatedAt = DateTime.UtcNow
            };

            // 更新缓存
            var hasChanges = oldProgress == null || !ProgressEquals(oldProgress, progress);
            _progressCache[instanceId] = progress;

            // 如果状态发生变化，更新数据库中的实例状态
            if (newStatus != instance.Status)
            {
                instance.Status = newStatus;
                if (newStatus is TransactionStatus.Completed or TransactionStatus.Failed && !instance.CompletedAt.HasValue)
                {
                    instance.CompletedAt = DateTime.UtcNow;
                    instance.Metrics.ElapsedTime = DateTime.UtcNow - instance.StartedAt;
                }

                await instanceService.UpdateInstanceAsync(instance, cancellationToken);
                
                _logger.LogInformation("推理实例状态更新: {InstanceId} {OldStatus} -> {NewStatus}", 
                    instanceId, oldStatus, newStatus);

                // 触发状态变化事件
                StatusChanged?.Invoke(instanceId, oldStatus, newStatus);
            }

            // 触发进度更新事件
            if (hasChanges)
            {
                ProgressUpdated?.Invoke(instanceId, progress);
            }

            return hasChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新推理实例进度失败: {InstanceId}", instanceId);
            return false;
        }
    }

    public async Task<int> RefreshAllActiveInstancesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var instanceService = scope.ServiceProvider.GetRequiredService<IReasoningInstanceService>();
            
            var activeInstances = await instanceService.GetInstancesAsync(cancellationToken: cancellationToken);
            var activeCandidates = activeInstances.Where(i => 
                i.Status != TransactionStatus.Completed && 
                i.Status != TransactionStatus.Failed).ToList();

            var changedCount = 0;
            foreach (var instance in activeCandidates)
            {
                if (await RefreshInstanceProgressAsync(instance.InstanceId, cancellationToken))
                {
                    changedCount++;
                }
            }

            return changedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新所有活跃推理实例失败");
            return 0;
        }
    }

    /// <summary>
    /// 组合状态更新时的回调
    /// </summary>
    private async void OnCombinationStatusUpdated(string combinationId, CombinationExecutionStatus status)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var instanceService = scope.ServiceProvider.GetRequiredService<IReasoningInstanceService>();
            
            // 查找包含此组合的推理实例
            var instances = await instanceService.GetInstancesAsync();
            var relevantInstance = instances.FirstOrDefault(i => 
                i.InputCombinations.Any(c => c.CombinationId == combinationId));
                
            if (relevantInstance != null)
            {
                // 异步刷新实例进度，避免阻塞
                _ = Task.Run(() => RefreshInstanceProgressAsync(relevantInstance.InstanceId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理组合状态更新时失败: {CombinationId}", combinationId);
        }
    }

    /// <summary>
    /// 智能判断推理实例状态
    /// </summary>
    private static TransactionStatus DetermineInstanceStatus(
        ReasoningTransactionInstance instance, 
        int executingCount, 
        int queuingCount, 
        int pendingCount, 
        int completedCount, 
        int failedCount)
    {
        var totalCombinations = instance.InputCombinations.Count;
        
        // 如果没有组合，保持原状态
        if (totalCombinations == 0)
            return instance.Status;

        // 如果所有组合都已完成（成功或失败）
        if (completedCount + failedCount == totalCombinations)
        {
            // 如果有任何成功的，认为是完成状态
            return completedCount > 0 ? TransactionStatus.Completed : TransactionStatus.Failed;
        }

        // 如果有任务正在执行或排队，状态为生成输出中
        if (executingCount > 0 || queuingCount > 0)
            return TransactionStatus.GeneratingOutputs;

        // 如果有已完成的任务，但还有待处理或失败的，且当前没有执行中的任务
        // 这种情况下应该保持为Pending状态，表示可以继续执行但当前暂停
        if (completedCount > 0 && (pendingCount > 0 || failedCount > 0))
        {
            return TransactionStatus.Pending; // 表示暂停状态，可以继续
        }

        // 如果只有待处理任务，还没开始执行
        if (pendingCount > 0 && completedCount == 0 && failedCount == 0)
        {
            return TransactionStatus.Pending; // 等待开始执行
        }

        // 其他情况保持原状态
        return instance.Status;
    }

    /// <summary>
    /// 比较两个进度对象是否相等
    /// </summary>
    private static bool ProgressEquals(ReasoningProgress old, ReasoningProgress current)
    {
        return old.Status == current.Status &&
               old.CompletedCount == current.CompletedCount &&
               old.FailedCount == current.FailedCount &&
               old.ExecutingCount == current.ExecutingCount &&
               old.QueueingCount == current.QueueingCount &&
               old.PendingCount == current.PendingCount &&
               Math.Abs(old.TotalCost - current.TotalCost) < 0.001m;
    }
}
