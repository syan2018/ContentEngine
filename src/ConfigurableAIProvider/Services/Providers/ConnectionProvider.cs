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

// Update namespace
namespace ConfigurableAIProvider.Services.Providers;

/// <summary>
/// Loads connection configurations from a YAML file (relative to base directory) and resolves placeholders.
/// Caches loaded configurations.
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
        if (_loadedConfig != null) return; // Already initialized

        await _initSemaphore.WaitAsync();
        try
        {
            if (_loadedConfig != null) return; // Double-check lock

            string configFilePath = Path.Combine(AppContext.BaseDirectory, _options.ConnectionsFilePath);
            configFilePath = Path.GetFullPath(configFilePath); // Normalize path

            _logger.LogInformation("Loading connections configuration from: {FilePath}", configFilePath);

            if (!File.Exists(configFilePath))
            {
                _logger.LogError("Connections configuration file not found at {FilePath}", configFilePath);
                throw new FileNotFoundException("Connections configuration file not found.", configFilePath);
            }

            try
            {
                _loadedConfig = ConnectionsConfig.FromFile(configFilePath);
                _logger.LogInformation("Successfully loaded connections configuration.");
                // Optionally load environment override file here (e.g., connections.dev.yaml) and merge
                // string envFilePath = Path.Combine(Path.GetDirectoryName(configFilePath), $"connections.{_options.Environment}.yaml");
                // ... load and merge logic ...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or parse connections configuration file: {FilePath}", configFilePath);
                throw; // Re-throw after logging
            }
        }
        finally
        {
            _initSemaphore.Release();
        }
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