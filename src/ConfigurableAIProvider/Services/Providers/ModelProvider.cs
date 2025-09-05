using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Services.Loaders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ConfigurableAIProvider.Models;

namespace ConfigurableAIProvider.Services.Providers;

/// <summary>
/// Loads and caches ModelConfig configurations from the central models.yaml file.
/// </summary>
public class ModelProvider : IModelProvider
{
    private readonly IModelsConfigLoader _modelsConfigLoader;
    private readonly ILogger<ModelProvider> _logger;
    // Cache loaded definitions directly
    private readonly ConcurrentDictionary<string, ModelConfig> _modelDefinitions = new();
    private ModelsConfig? _loadedConfig;
    private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

    public ModelProvider(IModelsConfigLoader modelsConfigLoader, ILogger<ModelProvider> logger)
    {
        _modelsConfigLoader = modelsConfigLoader;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        // Use loadedConfig presence for initialization check
        if (_loadedConfig != null) return;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_loadedConfig != null) return; // Double-check lock

            try
            {
                _loadedConfig = await _modelsConfigLoader.LoadConfigAsync();
                _logger.LogInformation("成功加载模型定义配置。");
                
                // Populate cache immediately after loading
                if (_loadedConfig.Models != null)
                {
                     foreach(var kvp in _loadedConfig.Models)
                     {
                         // Perform basic validation
                         if(string.IsNullOrWhiteSpace(kvp.Value.Connection) || string.IsNullOrWhiteSpace(kvp.Value.ModelId))
                         {
                              _logger.LogWarning("模型定义 '{ModelId}' 缺少必需的 'connection' 或 'modelId'。该模型将不可用。", kvp.Key);
                              continue; // Skip invalid entries
                         }
                         _modelDefinitions.TryAdd(kvp.Key, kvp.Value);
                     }
                }
                _logger.LogInformation("已缓存 {Count} 个有效的模型定义。", _modelDefinitions.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载或解析模型定义文件失败。");
                // Set loadedConfig to something non-null but empty to prevent retries on failure? Or let it retry? Let it retry for now.
                _loadedConfig = null; 
                _modelDefinitions.Clear(); // Clear any partial cache
                throw; // Re-throw after logging
            }
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<ModelConfig> GetModelDefinitionAsync(string modelDefinitionId)
    {
        await EnsureInitializedAsync();

        if (_modelDefinitions.TryGetValue(modelDefinitionId, out var definition))
        {
            return definition;
        }

        _logger.LogWarning("未在已加载的配置中找到 ID 为 '{ModelDefinitionId}' 的模型定义。", modelDefinitionId);
        // Check if it was present in the file but invalid during init? Log _loadedConfig state?
        // For simplicity, just throw KeyNotFound if not in the valid cache.
        throw new KeyNotFoundException($"未找到 ID 为 '{modelDefinitionId}' 的模型定义。");
    }
} 