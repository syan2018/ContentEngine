using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ConfigurableAIProvider.Services.Loaders;

/// <summary>
/// 负责加载和缓存模型配置，支持环境特定的配置文件合并。
/// </summary>
public class ModelsConfigLoader : IModelsConfigLoader
{
    private readonly ConfigurableAIOptions _options;
    private readonly ILogger<ModelsConfigLoader> _logger;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private ModelsConfig? _cachedConfig;

    public ModelsConfigLoader(IOptions<ConfigurableAIOptions> options, ILogger<ModelsConfigLoader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ModelsConfig> LoadConfigAsync()
    {
        if (_cachedConfig != null)
        {
            _logger.LogDebug("返回缓存的模型配置，环境: {Environment}", _options.Environment);
            return _cachedConfig;
        }

        await _loadSemaphore.WaitAsync();
        try
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            // 验证 ModelsFilePath 是否已配置
            if (string.IsNullOrWhiteSpace(_options.ModelsFilePath))
            {
                _logger.LogError("ModelsFilePath 未在 ConfigurableAIOptions 中配置。无法加载模型定义。");
                throw new InvalidOperationException("ModelsFilePath 配置缺失。");
            }

            string baseFilePath = Path.GetFullPath(_options.ModelsFilePath, AppContext.BaseDirectory);
            _logger.LogInformation("正在从以下路径加载模型配置: {FilePath}", baseFilePath);

            if (!File.Exists(baseFilePath))
            {
                _logger.LogError("在路径 {FilePath} 未找到模型配置文件", baseFilePath);
                throw new FileNotFoundException("未找到模型配置文件。", baseFilePath);
            }

            try
            {
                // 使用 ModelsConfig 的新方法来处理环境文件合并
                _cachedConfig = ModelsConfig.FromFileWithEnvironment(baseFilePath, _options.Environment);
                _logger.LogInformation("成功加载模型配置，环境: {Environment}", _options.Environment);

                return _cachedConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载或解析模型配置文件失败: {FilePath}", baseFilePath);
                throw;
            }
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }
}
