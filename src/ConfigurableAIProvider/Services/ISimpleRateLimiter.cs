namespace ConfigurableAIProvider.Services;

/// <summary>
/// 简单的模型级别RPM限制器
/// </summary>
public interface ISimpleRateLimiter
{
    /// <summary>
    /// 等待获取执行许可（会排队等待）
    /// </summary>
    /// <param name="modelDefinitionId">模型定义ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>等待完成后获得许可</returns>
    Task WaitForPermissionAsync(string modelDefinitionId, CancellationToken cancellationToken = default);
}
