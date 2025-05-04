using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConfigurableAIProvider.Models;

// Update namespace
namespace ConfigurableAIProvider.Services.Providers;

/// <summary>
/// Loads connection configurations from YAML files (base and environment-specific) relative to the base directory,
/// merges them, and resolves placeholders. Caches loaded configurations.
/// </summary>
public class ConnectionProvider : IConnectionProvider
{
    private readonly ConfigurableAIOptions _options;
    private readonly ILogger<ConnectionProvider> _logger;
    private readonly ConcurrentDictionary<string, ConnectionConfig> _resolvedConnections = new();
    private ConnectionsConfig? _loadedConfig;
    private static readonly Regex PlaceholderRegex = new Regex(@"\{\{(.+?)\}\}", RegexOptions.Compiled);
    private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

    public ConnectionProvider(IOptions<ConfigurableAIOptions> options, ILogger<ConnectionProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_loadedConfig != null) return;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_loadedConfig != null) return;

            string baseConfigFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _options.ConnectionsFilePath));
            _logger.LogInformation("Loading base connections configuration from: {FilePath}", baseConfigFilePath);

            if (!File.Exists(baseConfigFilePath))
            {
                _logger.LogError("Base connections configuration file not found at {FilePath}", baseConfigFilePath);
                throw new FileNotFoundException("Base connections configuration file not found.", baseConfigFilePath);
            }

            // Load base configuration
            ConnectionsConfig baseConfig = LoadConnectionsFromFile(baseConfigFilePath, "base");

            // Load environment-specific configuration if it exists
            string configFileName = Path.GetFileNameWithoutExtension(_options.ConnectionsFilePath);
            string configFileExtension = Path.GetExtension(_options.ConnectionsFilePath);
            string envFileName = $"{configFileName}.{_options.Environment}{configFileExtension}";
            string envConfigFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(baseConfigFilePath) ?? AppContext.BaseDirectory, envFileName));

            ConnectionsConfig mergedConfig = baseConfig; // Start with base config

            if (File.Exists(envConfigFilePath))
            {
                _logger.LogInformation("Found environment-specific connections configuration: {FilePath}", envConfigFilePath);
                ConnectionsConfig envConfig = LoadConnectionsFromFile(envConfigFilePath, $"environment ('{_options.Environment}')");
                
                // Merge environment config into base config
                mergedConfig = MergeConnections(baseConfig, envConfig);
                _logger.LogInformation("Successfully merged environment-specific connections configuration.");
            }
            else
            {
                _logger.LogDebug("No environment-specific connections configuration file found at {FilePath}. Using base configuration only.", envConfigFilePath);
            }

            _loadedConfig = mergedConfig; // Store the final merged config
        }
        finally
        {
            _initSemaphore.Release();
        }
    }
    
    private ConnectionsConfig LoadConnectionsFromFile(string filePath, string typeDescription)
    {
        try
        {
            return ConnectionsConfig.FromFile(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load or parse {TypeDescription} connections configuration file: {FilePath}", typeDescription, filePath);
            throw; 
        }
    }

    private ConnectionsConfig MergeConnections(ConnectionsConfig baseConfig, ConnectionsConfig overrideConfig)
    {
        // Start with a shallow copy of the base config's connections or an empty dictionary
        var mergedConnections = new Dictionary<string, ConnectionConfig>(baseConfig.Connections ?? new Dictionary<string, ConnectionConfig>());

        if (overrideConfig.Connections != null)
        {
            foreach (var kvp in overrideConfig.Connections)
            {
                string key = kvp.Key;
                ConnectionConfig overrideConnection = kvp.Value;

                if (mergedConnections.TryGetValue(key, out ConnectionConfig? baseConnection))
                {
                    // Key exists in both: Update existing base connection with non-null values from override
                    // Note: This is a shallow merge of properties within ConnectionConfig.
                    // You might want a deeper merge if ConnectionConfig becomes more complex.
                     baseConnection.ServiceType = overrideConnection.ServiceType; // Override ServiceType 
                     if (overrideConnection.Endpoint != null) baseConnection.Endpoint = overrideConnection.Endpoint;
                     if (overrideConnection.BaseUrl != null) baseConnection.BaseUrl = overrideConnection.BaseUrl;
                     if (overrideConnection.ApiKey != null) baseConnection.ApiKey = overrideConnection.ApiKey;
                     if (overrideConnection.OrgId != null) baseConnection.OrgId = overrideConnection.OrgId;
                    // Add other properties here if needed
                    _logger.LogDebug("Merging connection '{ConnectionName}': Overriding properties from environment config.", key);
                }
                else
                {
                    // Key only in override: Add it to the merged dictionary
                    mergedConnections.Add(key, overrideConnection);
                     _logger.LogDebug("Merging connection '{ConnectionName}': Adding new connection from environment config.", key);
                }
            }
        }

        return new ConnectionsConfig { Connections = mergedConnections };
    }

    public async Task<ConnectionConfig> GetResolvedConnectionAsync(string connectionName)
    {
        await EnsureInitializedAsync();

        if (_resolvedConnections.TryGetValue(connectionName, out var resolvedConfig))
        {
            return resolvedConfig;
        }

        if (_loadedConfig?.Connections == null || !_loadedConfig.Connections.TryGetValue(connectionName, out var rawConfig))
        {
            _logger.LogWarning("Connection configuration named '{ConnectionName}' not found.", connectionName);
            throw new KeyNotFoundException($"Connection configuration named '{connectionName}' not found.");
        }

        // Resolve placeholders
        try
        {
            var newResolvedConfig = new ConnectionConfig
            {
                ServiceId = connectionName,
                ServiceType = rawConfig.ServiceType,
                Endpoint = ResolvePlaceholders(rawConfig.Endpoint, connectionName),
                BaseUrl = ResolvePlaceholders(rawConfig.BaseUrl, connectionName),
                ApiKey = ResolvePlaceholders(rawConfig.ApiKey, connectionName, isSensitive: true),
                OrgId = ResolvePlaceholders(rawConfig.OrgId, connectionName)
            };
            
            // Validate required fields based on ServiceType AFTER resolution
            ValidateResolvedConfig(newResolvedConfig, connectionName, rawConfig);

            _resolvedConnections.TryAdd(connectionName, newResolvedConfig);
            _logger.LogDebug("Resolved and cached connection '{ConnectionName}'.", connectionName);
            return newResolvedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve placeholders or validate connection '{ConnectionName}'.", connectionName);
            // Wrap the original exception for better diagnostics
            throw new InvalidOperationException($"Failed to get resolved connection '{connectionName}'. See inner exception.", ex); 
        }
    }

    private void ValidateResolvedConfig(ConnectionConfig resolvedConfig, string connectionName, ConnectionConfig rawConfig)
    {
        switch (resolvedConfig.ServiceType)
        {
            case ServiceType.OpenAI:
                // API Key is generally required for OpenAI unless using specific auth methods not yet handled here.
                if (string.IsNullOrWhiteSpace(resolvedConfig.ApiKey) && rawConfig.ApiKey != null) // Check rawConfig.ApiKey != null to ensure it was intended to be set
                {
                    ThrowValidationError($"API Key for OpenAI connection '{connectionName}' could not be resolved.", connectionName);
                }
                // BaseUrl is optional for OpenAI (defaults in SK), but if specified, it should be valid.
                // OrgId is optional.
                break;
            
            case ServiceType.AzureOpenAI:
                if (string.IsNullOrWhiteSpace(resolvedConfig.ApiKey) && rawConfig.ApiKey != null)
                {
                     ThrowValidationError($"API Key for Azure OpenAI connection '{connectionName}' could not be resolved.", connectionName);
                }
                if (string.IsNullOrWhiteSpace(resolvedConfig.Endpoint) && rawConfig.Endpoint != null)
                {
                    ThrowValidationError($"Endpoint for Azure OpenAI connection '{connectionName}' could not be resolved.", connectionName);
                }
                // Add validation for DeploymentName/ModelId if it were part of ConnectionConfig
                break;

            case ServiceType.Ollama:
                if (string.IsNullOrWhiteSpace(resolvedConfig.BaseUrl) && rawConfig.BaseUrl != null)
                {
                    ThrowValidationError($"BaseUrl for Ollama connection '{connectionName}' could not be resolved or is empty.", connectionName);
                }
                // API Key is not typically used for standard Ollama setups.
                break;
                
            // Add cases for other service types as needed

            default:
                 _logger.LogWarning("Validation logic not implemented for ServiceType '{ServiceType}' on connection '{ConnectionName}'.", resolvedConfig.ServiceType, connectionName);
                 break;
        }
    }
    
    private void ThrowValidationError(string message, string connectionName)
    {
        _logger.LogError(message);
        throw new InvalidOperationException(message);
    }

    private string? ResolvePlaceholders(string? value, string connectionName, bool isSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return PlaceholderRegex.Replace(value, match =>
        {
            string envVarName = match.Groups[1].Value.Trim();
            string? envVarValue = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(envVarValue))
            {
                string message = $"Environment variable '{envVarName}' used in placeholder '{{{{{envVarName}}}}}' for connection '{connectionName}' not found or is empty.";
                 ThrowValidationError(message, connectionName); // Use the helper method
            }
            
            if(isSensitive)
                 _logger.LogDebug("Resolved sensitive placeholder '{{{{{PlaceholderName}}}}}' for connection '{ConnectionName}' from environment variable.", envVarName, connectionName);
            else
                _logger.LogDebug("Resolved placeholder '{{{{{PlaceholderName}}}}}' to '{ResolvedValue}' for connection '{ConnectionName}' from environment variable.", envVarName, envVarValue ?? "[null]", connectionName); // Handle null envVarValue in log


            return envVarValue;
        });
    }
} 