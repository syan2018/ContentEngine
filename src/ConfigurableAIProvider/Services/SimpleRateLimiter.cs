using ConfigurableAIProvider.Services.Providers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ConfigurableAIProvider.Services;

/// <summary>
/// 简单的模型级别RPM限制器实现
/// </summary>
public class SimpleRateLimiter : ISimpleRateLimiter
{
    private readonly IModelProvider _modelProvider;
    private readonly ILogger<SimpleRateLimiter> _logger;
    
    // 每个模型的请求时间戳记录
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestTimes = new();
    private readonly object _lock = new object();

    /// <summary>
    /// 初始化简单RPM限制器
    /// </summary>
    /// <param name="modelProvider">模型提供程序</param>
    /// <param name="logger">日志记录器</param>
    public SimpleRateLimiter(IModelProvider modelProvider, ILogger<SimpleRateLimiter> logger)
    {
        _modelProvider = modelProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task WaitForPermissionAsync(string modelDefinitionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取模型配置
            var modelConfig = await _modelProvider.GetModelDefinitionAsync(modelDefinitionId);
            
            // 如果没有设置RPM限制，直接返回
            if (modelConfig.RequestsPerMinute <= 0)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                bool canProceed = false;
                TimeSpan waitTime = TimeSpan.Zero;

                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    var oneMinuteAgo = now.AddMinutes(-1);

                    // 获取或创建该模型的请求记录队列
                    var requestQueue = _requestTimes.GetOrAdd(modelDefinitionId, _ => new Queue<DateTime>());

                    // 移除1分钟前的记录
                    while (requestQueue.Count > 0 && requestQueue.Peek() < oneMinuteAgo)
                    {
                        requestQueue.Dequeue();
                    }

                    // 检查是否可以执行
                    if (requestQueue.Count < modelConfig.RequestsPerMinute)
                    {
                        // 记录这次请求
                        requestQueue.Enqueue(now);
                        canProceed = true;
                        
                        _logger.LogDebug("模型 {ModelId} 请求许可已授予: {Current}/{Max}", 
                            modelDefinitionId, requestQueue.Count, modelConfig.RequestsPerMinute);
                    }
                    else
                    {
                        // 计算需要等待的时间
                        var oldestRequest = requestQueue.Peek();
                        var waitUntil = oldestRequest.AddMinutes(1).AddSeconds(1); // 多等1秒确保安全
                        waitTime = waitUntil - now;
                        
                        if (waitTime <= TimeSpan.Zero)
                        {
                            waitTime = TimeSpan.FromSeconds(1); // 最少等1秒
                        }

                        _logger.LogDebug("模型 {ModelId} RPM限制，需等待: {WaitTime}ms, 当前: {Current}/{Max}", 
                            modelDefinitionId, waitTime.TotalMilliseconds, requestQueue.Count, modelConfig.RequestsPerMinute);
                    }
                }

                if (canProceed)
                {
                    return; // 获得许可，退出
                }

                // 等待指定时间后重试
                await Task.Delay(waitTime, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("模型 {ModelId} RPM等待被取消", modelDefinitionId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "等待模型 {ModelId} RPM许可时发生错误，允许执行", modelDefinitionId);
            // 出错时默认允许继续
        }
    }
}
