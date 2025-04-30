using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;

// Update namespace
namespace ConfigurableAIProvider.Services.Loaders;

/// <summary>
/// Loads and caches agent configurations from subdirectories.
/// </summary>
public class AgentConfigLoader : IAgentConfigLoader
{
    private readonly ConfigurableAIOptions _options;
    private readonly ILogger<AgentConfigLoader> _logger;
    private readonly ConcurrentDictionary<string, AgentConfig> _cachedAgentConfigs = new();

    public AgentConfigLoader(IOptions<ConfigurableAIOptions> options, ILogger<AgentConfigLoader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<AgentConfig> LoadConfigAsync(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException("Agent name cannot be null or whitespace.", nameof(agentName));
        }

        // Cache key combines agent name and environment to ensure correct caching
        string cacheKey = $"{agentName}:{_options.Environment}"; 

        if (_cachedAgentConfigs.TryGetValue(cacheKey, out var cachedConfig))
        {
             _logger.LogDebug("Returning cached configuration for agent '{AgentName}' in environment '{Environment}'.", agentName, _options.Environment);
            return Task.FromResult(cachedConfig);
        }

        string agentDirectoryPath = Path.Combine(AppContext.BaseDirectory, _options.AgentsDirectory, agentName);
        agentDirectoryPath = Path.GetFullPath(agentDirectoryPath); // Normalize path

         _logger.LogInformation("Loading configuration for agent '{AgentName}' from directory: {DirectoryPath}", agentName, agentDirectoryPath);


        if (!Directory.Exists(agentDirectoryPath))
        {
             _logger.LogError("Agent configuration directory not found for agent '{AgentName}': {DirectoryPath}", agentName, agentDirectoryPath);
            throw new DirectoryNotFoundException($"Agent configuration directory not found for agent '{agentName}'. Looked in: {agentDirectoryPath}");
        }

        try
        {
            // AgentConfig.FromDirectory handles merging agent.yaml and agent.{env}.yaml
            var agentConfig = AgentConfig.FromDirectory(agentDirectoryPath, _options.Environment);

             _logger.LogInformation("Successfully loaded configuration for agent '{AgentName}' in environment '{Environment}'.", agentName, _options.Environment);

            // Add to cache
            _cachedAgentConfigs.TryAdd(cacheKey, agentConfig);

            return Task.FromResult(agentConfig);
        }
        catch (FileNotFoundException ex)
        {
             _logger.LogError(ex, "Required configuration file (agent.yaml) missing for agent '{AgentName}' in directory {DirectoryPath}.", agentName, agentDirectoryPath);
             throw; // Re-throw standard exceptions after logging
        }
        catch (Exception ex) // Catch other potential errors during loading/parsing
        {
             _logger.LogError(ex, "Failed to load configuration for agent '{AgentName}' from directory {DirectoryPath}.", agentName, agentDirectoryPath);
             throw new InvalidOperationException($"Failed to load configuration for agent '{agentName}'. See inner exception.", ex);
        }
    }
} 