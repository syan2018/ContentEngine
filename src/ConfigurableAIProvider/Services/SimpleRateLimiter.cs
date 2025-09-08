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

    // 按模型维护“下次可用时间”，用于平滑间隔发放许可
    private class RateState
    {
        public DateTime NextAvailableUtc = DateTime.MinValue;
        public SemaphoreSlim Mutex = new SemaphoreSlim(1, 1);
    }

    private readonly ConcurrentDictionary<string, RateState> _rateStates = new();

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

            // 计算最小间隔（例如 60 RPM => 1 秒/次）
            var rpm = modelConfig.RequestsPerMinute;
            var minInterval = TimeSpan.FromSeconds(60.0 / rpm);

            // 为该模型获取状态
            var state = _rateStates.GetOrAdd(modelDefinitionId, _ => new RateState());

            DateTime grantTimeUtc;

            // 使用“预约”策略，确保并发请求被均匀分配到时间线上
            await state.Mutex.WaitAsync(cancellationToken);
            try
            {
                var nowUtc = DateTime.UtcNow;
                grantTimeUtc = nowUtc >= state.NextAvailableUtc ? nowUtc : state.NextAvailableUtc;
                state.NextAvailableUtc = grantTimeUtc + minInterval; // 为下一个请求预留时间片
            }
            finally
            {
                state.Mutex.Release();
            }

            var delay = grantTimeUtc - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                _logger.LogDebug("模型 {ModelId} RPM平滑限速，需等待: {WaitMs}ms (间隔 {IntervalMs}ms)",
                    modelDefinitionId, delay.TotalMilliseconds, minInterval.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }

            // 到达预约时间点，允许执行
            _logger.LogDebug("模型 {ModelId} 请求许可已授予 (平滑)", modelDefinitionId);
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
