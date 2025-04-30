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
/// Loads connection configurations from a YAML file and resolves placeholders.
/// Caches loaded configurations.
/// </summary>
public class ConnectionProvider : IConnectionProvider
{
    private readonly ConfigurableAIOptions _options;
    private readonly ILogger<ConnectionProvider> _logger;
    private readonly ConcurrentDictionary<string, ConnectionConfig> _resolvedConnections = new();
    private ConnectionsConfig? _loadedConfig;
    private static readonly Regex PlaceholderRegex = new Regex(@"\{\{(.+?)\}\}", RegexOptions.Compiled); // Corrected Regex
    private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1); // Ensure thread-safe initialization

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

            string configFilePath = Path.GetFullPath(_options.ConnectionsFilePath, AppContext.BaseDirectory);
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
                ServiceType = rawConfig.ServiceType,
                Endpoint = ResolvePlaceholders(rawConfig.Endpoint, connectionName),
                ApiKey = ResolvePlaceholders(rawConfig.ApiKey, connectionName, isSensitive: true), // Mark API key as sensitive for logging
                OrgId = ResolvePlaceholders(rawConfig.OrgId, connectionName)
            };
            
            // Validate required fields after resolution
            if (string.IsNullOrWhiteSpace(newResolvedConfig.ApiKey) && newResolvedConfig.ServiceType != ServiceType.AzureOpenAI && rawConfig.ApiKey != null) // Only warn/error if ApiKey was expected (i.e., placeholder existed)
            {
                 _logger.LogError("API Key for connection '{ConnectionName}' could not be resolved from placeholder.", connectionName);
                 throw new InvalidOperationException($"API Key for connection '{connectionName}' could not be resolved.");
            }
            if (string.IsNullOrWhiteSpace(newResolvedConfig.Endpoint) && newResolvedConfig.ServiceType == ServiceType.AzureOpenAI && rawConfig.Endpoint != null) // Only warn/error if Endpoint was expected
            {
                _logger.LogError("Endpoint for Azure connection '{ConnectionName}' could not be resolved from placeholder.", connectionName);
                 throw new InvalidOperationException($"Endpoint for Azure connection '{connectionName}' could not be resolved.");
            }


            _resolvedConnections.TryAdd(connectionName, newResolvedConfig);
            _logger.LogDebug("Resolved and cached connection '{ConnectionName}'.", connectionName);
            return newResolvedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve placeholders for connection '{ConnectionName}'.", connectionName);
            throw new InvalidOperationException($"Failed to resolve placeholders for connection '{connectionName}'. See inner exception for details.", ex);
        }
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
                _logger.LogError(message);
                // Throw an error because resolution failed for an expected variable
                throw new InvalidOperationException(message);
            }
            
            if(isSensitive)
                 _logger.LogDebug("Resolved sensitive placeholder '{{{{{PlaceholderName}}}}}' for connection '{ConnectionName}' from environment variable.", envVarName, connectionName);
            else
                _logger.LogDebug("Resolved placeholder '{{{{{PlaceholderName}}}}}' to '{ResolvedValue}' for connection '{ConnectionName}' from environment variable.", envVarName, envVarValue, connectionName);


            return envVarValue;
        });
    }
} 