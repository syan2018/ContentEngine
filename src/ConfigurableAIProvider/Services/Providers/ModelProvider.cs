using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigurableAIProvider.Services.Providers;

/// <summary>
/// Loads and caches ModelDefinition configurations from the central models.yaml file.
/// </summary>
public class ModelProvider : IModelProvider
{
    private readonly ConfigurableAIOptions _options;
    private readonly ILogger<ModelProvider> _logger;
    // Cache loaded definitions directly
    private readonly ConcurrentDictionary<string, ModelDefinition> _modelDefinitions = new();
    private ModelsConfig? _loadedConfig;
    private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

    public ModelProvider(IOptions<ConfigurableAIOptions> options, ILogger<ModelProvider> logger)
    {
        _options = options.Value;
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

             // Validate ModelsFilePath is set
            if (string.IsNullOrWhiteSpace(_options.ModelsFilePath))
            {
                 _logger.LogError("ModelsFilePath is not configured in ConfigurableAIOptions. Cannot load model definitions.");
                 throw new InvalidOperationException("ModelsFilePath configuration is missing.");
            }


            string configFilePath = Path.GetFullPath(_options.ModelsFilePath, AppContext.BaseDirectory);
            _logger.LogInformation("Loading model definitions configuration from: {FilePath}", configFilePath);

            if (!File.Exists(configFilePath))
            {
                _logger.LogError("Model definitions configuration file not found at {FilePath}", configFilePath);
                throw new FileNotFoundException("Model definitions configuration file not found.", configFilePath);
            }

            try
            {
                _loadedConfig = ModelsConfig.FromFile(configFilePath);
                _logger.LogInformation("Successfully loaded model definitions configuration.");
                
                // Populate cache immediately after loading
                if (_loadedConfig.Models != null)
                {
                     foreach(var kvp in _loadedConfig.Models)
                     {
                         // Perform basic validation
                         if(string.IsNullOrWhiteSpace(kvp.Value.Connection) || string.IsNullOrWhiteSpace(kvp.Value.ModelId))
                         {
                              _logger.LogWarning("Model definition '{ModelId}' is missing required 'connection' or 'modelId'. It will not be available.", kvp.Key);
                              continue; // Skip invalid entries
                         }
                          _modelDefinitions.TryAdd(kvp.Key, kvp.Value);
                     }
                }
                 _logger.LogInformation("Cached {Count} valid model definitions.", _modelDefinitions.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or parse model definitions file: {FilePath}", configFilePath);
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

    public async Task<ModelDefinition> GetModelDefinitionAsync(string modelDefinitionId)
    {
        await EnsureInitializedAsync();

        if (_modelDefinitions.TryGetValue(modelDefinitionId, out var definition))
        {
            return definition;
        }
        else
        {
             _logger.LogWarning("Model definition with ID '{ModelDefinitionId}' not found in the loaded configuration.", modelDefinitionId);
             // Check if it was present in the file but invalid during init? Log _loadedConfig state?
             // For simplicity, just throw KeyNotFound if not in the valid cache.
            throw new KeyNotFoundException($"Model definition with ID '{modelDefinitionId}' not found.");
        }
    }
} 