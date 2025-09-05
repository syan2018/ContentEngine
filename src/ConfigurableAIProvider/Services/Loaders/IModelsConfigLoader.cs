using ConfigurableAIProvider.Models;

namespace ConfigurableAIProvider.Services.Loaders;

/// <summary>
/// 负责加载和缓存模型配置，支持环境特定的配置文件合并。
/// </summary>
public interface IModelsConfigLoader
{
    /// <summary>
    /// 异步加载模型配置，支持环境特定的配置文件合并。
    /// 会自动合并 models.yaml 和 models.{environment}.yaml 文件。
    /// </summary>
    /// <returns>合并后的模型配置对象</returns>
    Task<ModelsConfig> LoadConfigAsync();
}
