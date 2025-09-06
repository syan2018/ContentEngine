using Microsoft.AspNetCore.SignalR;
using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.Inference.Services;

namespace ContentEngine.WebApp.Components.Hubs;

/// <summary>
/// 推理进度实时更新Hub
/// 负责向前端推送实时的推理进度和状态变化
/// </summary>
public class ReasoningProgressHub : Hub
{
    private readonly IReasoningProgressManager _progressManager;
    private readonly ILogger<ReasoningProgressHub> _logger;

    public ReasoningProgressHub(
        IReasoningProgressManager progressManager,
        ILogger<ReasoningProgressHub> logger)
    {
        _progressManager = progressManager;
        _logger = logger;
    }

    /// <summary>
    /// 连接时的处理
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogDebug("推理进度Hub连接建立: {ConnectionId}", connectionId);
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 断开连接时的处理
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogDebug("推理进度Hub连接断开: {ConnectionId}, 异常: {Exception}", 
            connectionId, exception?.Message);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 订阅特定推理实例的进度更新
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    public async Task SubscribeToInstance(string instanceId)
    {
        try
        {
            var groupName = GetInstanceGroupName(instanceId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogDebug("客户端 {ConnectionId} 订阅了实例 {InstanceId} 的进度更新", 
                Context.ConnectionId, instanceId);

            // 立即发送当前进度
            var progress = await _progressManager.GetProgressAsync(instanceId);
            if (progress != null)
            {
                await Clients.Caller.SendAsync("ProgressUpdated", instanceId, progress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅实例进度失败: {InstanceId}, 连接: {ConnectionId}", 
                instanceId, Context.ConnectionId);
        }
    }

    /// <summary>
    /// 取消订阅特定推理实例的进度更新
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    public async Task UnsubscribeFromInstance(string instanceId)
    {
        try
        {
            var groupName = GetInstanceGroupName(instanceId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogDebug("客户端 {ConnectionId} 取消订阅实例 {InstanceId} 的进度更新", 
                Context.ConnectionId, instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅实例进度失败: {InstanceId}, 连接: {ConnectionId}", 
                instanceId, Context.ConnectionId);
        }
    }

    /// <summary>
    /// 订阅所有推理实例的进度摘要
    /// </summary>
    public async Task SubscribeToAllProgress()
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AllProgress");
            
            _logger.LogDebug("客户端 {ConnectionId} 订阅了所有实例的进度摘要", Context.ConnectionId);

            // 立即发送当前的所有进度摘要
            var summaries = await _progressManager.GetAllProgressSummariesAsync();
            await Clients.Caller.SendAsync("AllProgressUpdated", summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅所有进度摘要失败, 连接: {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// 取消订阅所有推理实例的进度摘要
    /// </summary>
    public async Task UnsubscribeFromAllProgress()
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AllProgress");
            
            _logger.LogDebug("客户端 {ConnectionId} 取消订阅所有实例的进度摘要", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅所有进度摘要失败, 连接: {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// 订阅特定组合的状态变化
    /// </summary>
    /// <param name="combinationId">组合ID</param>
    public async Task SubscribeToCombination(string combinationId)
    {
        try
        {
            var groupName = GetCombinationGroupName(combinationId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogDebug("客户端 {ConnectionId} 订阅组合 {CombinationId} 的状态变化", Context.ConnectionId, combinationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅组合状态失败, 连接: {ConnectionId}, 组合: {CombinationId}", Context.ConnectionId, combinationId);
        }
    }

    /// <summary>
    /// 取消订阅特定组合的状态变化
    /// </summary>
    /// <param name="combinationId">组合ID</param>
    public async Task UnsubscribeFromCombination(string combinationId)
    {
        try
        {
            var groupName = GetCombinationGroupName(combinationId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogDebug("客户端 {ConnectionId} 取消订阅组合 {CombinationId} 的状态变化", Context.ConnectionId, combinationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅组合状态失败, 连接: {ConnectionId}, 组合: {CombinationId}", Context.ConnectionId, combinationId);
        }
    }

    /// <summary>
    /// 获取实例组名称
    /// </summary>
    private static string GetInstanceGroupName(string instanceId)
    {
        return $"Instance_{instanceId}";
    }

    /// <summary>
    /// 获取组合组名称
    /// </summary>
    private static string GetCombinationGroupName(string combinationId)
    {
        return $"Combination_{combinationId}";
    }
}

/// <summary>
/// SignalR通知服务
/// 将ReasoningProgressManager的事件转发给SignalR客户端
/// </summary>
public class SignalRProgressNotificationService : IHostedService
{
    private readonly IReasoningProgressManager _progressManager;
    private readonly ICombinationStatusTracker _statusTracker;
    private readonly IHubContext<ReasoningProgressHub> _hubContext;
    private readonly ILogger<SignalRProgressNotificationService> _logger;

    public SignalRProgressNotificationService(
        IReasoningProgressManager progressManager,
        ICombinationStatusTracker statusTracker,
        IHubContext<ReasoningProgressHub> hubContext,
        ILogger<SignalRProgressNotificationService> logger)
    {
        _progressManager = progressManager;
        _statusTracker = statusTracker;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 订阅进度管理器的事件
        _progressManager.ProgressUpdated += OnProgressUpdated;
        _progressManager.StatusChanged += OnStatusChanged;
        
        // 订阅组合状态跟踪器的事件
        _statusTracker.StatusUpdated += OnCombinationStatusUpdated;
        
        _logger.LogInformation("SignalR推理进度通知服务已启动");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // 取消订阅事件
        _progressManager.ProgressUpdated -= OnProgressUpdated;
        _progressManager.StatusChanged -= OnStatusChanged;
        _statusTracker.StatusUpdated -= OnCombinationStatusUpdated;
        
        _logger.LogInformation("SignalR推理进度通知服务已停止");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理进度更新事件
    /// </summary>
    private async void OnProgressUpdated(string instanceId, ReasoningProgress progress)
    {
        try
        {
            var groupName = $"Instance_{instanceId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ProgressUpdated", instanceId, progress);
            
            // 同时通知订阅了全局进度的客户端
            var summary = new ReasoningProgressSummary
            {
                InstanceId = instanceId,
                Status = progress.Status,
                CompletionPercentage = progress.CompletionPercentage,
                StatusText = progress.GetStatusDisplayText(),
                IsActive = progress.IsActive,
                UpdatedAt = progress.UpdatedAt
            };
            
            await _hubContext.Clients.Group("AllProgress").SendAsync("ProgressSummaryUpdated", instanceId, summary);
            
            _logger.LogDebug("已推送进度更新: {InstanceId}, 状态: {Status}, 进度: {Progress:F1}%", 
                instanceId, progress.Status, progress.CompletionPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送进度更新失败: {InstanceId}", instanceId);
        }
    }

    /// <summary>
    /// 处理状态变化事件
    /// </summary>
    private async void OnStatusChanged(string instanceId, TransactionStatus oldStatus, TransactionStatus newStatus)
    {
        try
        {
            var groupName = $"Instance_{instanceId}";
            await _hubContext.Clients.Group(groupName).SendAsync("StatusChanged", instanceId, oldStatus, newStatus);
            
            _logger.LogDebug("已推送状态变化: {InstanceId}, {OldStatus} -> {NewStatus}", 
                instanceId, oldStatus, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送状态变化失败: {InstanceId}", instanceId);
        }
    }

    /// <summary>
    /// 处理组合状态更新事件
    /// </summary>
    private async void OnCombinationStatusUpdated(string combinationId, CombinationExecutionStatus status)
    {
        try
        {
            var groupName = $"Combination_{combinationId}";
            await _hubContext.Clients.Group(groupName).SendAsync("CombinationStatusUpdated", combinationId, status);
            
            _logger.LogDebug("已推送组合状态更新: {CombinationId}, 状态: {Status}", 
                combinationId, status.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送组合状态更新失败: {CombinationId}", combinationId);
        }
    }
}
