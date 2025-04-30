using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Services.Loaders;     // Added
using ConfigurableAIProvider.Services.Providers;   // Added
using ConfigurableAIProvider.Services.Configurators; // Added
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.IO; // Required for Path operations
using System.Linq; // Required for LINQ methods like Any
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; // Still needed for GlobalPluginsDirectory from ConfigurableAIOptions

namespace ConfigurableAIProvider.Services;

/// <summary>
/// Default implementation of IAIKernelFactory that builds Kernels based on Agent configurations 
/// using pluggable service configurators.
/// </summary>
public class DefaultAIKernelFactory : IAIKernelFactory
{
    private readonly IAgentConfigLoader _agentConfigLoader;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IEnumerable<IAIServiceConfigurator> _serviceConfigurators; // Changed
    private readonly ILoggerFactory _loggerFactory; // Use ILoggerFactory to create Kernel logger
    private readonly ILogger<DefaultAIKernelFactory> _logger;
    private readonly ConfigurableAIOptions _providerOptions; // Keep options for global plugin path etc.


    // Remove IOptions<AIServiceOptions> and IPluginLoader dependencies
    public DefaultAIKernelFactory(
        IAgentConfigLoader agentConfigLoader,
        IConnectionProvider connectionProvider,
        IEnumerable<IAIServiceConfigurator> serviceConfigurators, // Changed
        ILoggerFactory loggerFactory, // Inject ILoggerFactory
        IOptions<ConfigurableAIOptions> providerOptions, // Inject provider options
        ILogger<DefaultAIKernelFactory> logger)
    {
        _agentConfigLoader = agentConfigLoader;
        _connectionProvider = connectionProvider;
        _serviceConfigurators = serviceConfigurators; // Changed
        _loggerFactory = loggerFactory;
        _providerOptions = providerOptions.Value;
        _logger = logger;
    }

    public async Task<Kernel> BuildKernelAsync(string agentName)
    {
        _logger.LogInformation("Building Kernel for agent: {AgentName}", agentName);

        AgentConfig agentConfig = await LoadAgentConfigurationAsync(agentName);

        var builder = Kernel.CreateBuilder();

        ConfigureKernelLogging(builder, agentConfig);

        await ConfigureAiServicesAsync(builder, agentConfig, agentName);

        var kernel = builder.Build();

        await LoadPluginsAsync(kernel, agentConfig, agentName);
        
        _logger.LogInformation("Successfully built Kernel for agent: {AgentName}", agentName);
        return kernel;
    }

    // --- Private Helper Methods ---

    private async Task<AgentConfig> LoadAgentConfigurationAsync(string agentName)
    {
        try
        {
            return await _agentConfigLoader.LoadConfigAsync(agentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration for agent: {AgentName}", agentName);
            throw; // Re-throw exceptions related to config loading
        }
    }

    private void ConfigureKernelLogging(IKernelBuilder builder, AgentConfig agentConfig)
    {
         if (agentConfig.LogLevel.HasValue)
         {
             // Note: As mentioned before, SK's builder logging config might change.
             _logger.LogWarning("LogLevel from agent.yaml is currently informational and may not directly control Kernel's internal logging level through KernelBuilder. Kernel will use the globally configured logger factory.");
             // You might need more specific configuration depending on the SK version and logging setup.
             // builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory); // Example if needed
         }
         else
         {
              // Use the host application's logging by default
              builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory); 
         }
    }

    private async Task ConfigureAiServicesAsync(IKernelBuilder builder, AgentConfig agentConfig, string agentName)
    {
        if (agentConfig.Models == null || !agentConfig.Models.Any())
        { 
            _logger.LogWarning("No models configured for agent '{AgentName}'. Kernel will be built without AI services.", agentName);
            return;
        }

        foreach (var modelEntry in agentConfig.Models)
        {
            var modelName = modelEntry.Key;
            var modelConfig = modelEntry.Value;

            if (string.IsNullOrWhiteSpace(modelConfig.Connection) || string.IsNullOrWhiteSpace(modelConfig.ModelId))
            {
                _logger.LogWarning("Skipping model '{ModelName}' for agent '{AgentName}' due to missing connection name or model ID.", modelName, agentName);
                continue;
            }

            try
            {
                ConnectionConfig connectionConfig = await _connectionProvider.GetResolvedConnectionAsync(modelConfig.Connection);
                
                // Find the appropriate configurator for this service type
                var configurator = _serviceConfigurators.FirstOrDefault(c => c.HandledServiceType == connectionConfig.ServiceType);

                if (configurator != null)
                {
                     _logger.LogDebug("Using {ConfiguratorType} to configure AI service for model '{ModelName}' (ID: {ModelId}) using connection '{ConnectionName}' for agent '{AgentName}'.",
                                     configurator.GetType().Name, modelName, modelConfig.ModelId, modelConfig.Connection, agentName);
                    configurator.ConfigureService(builder, modelConfig, connectionConfig);
                }
                else
                {
                    _logger.LogWarning("No service configurator found for ServiceType '{ServiceType}' used by connection '{ConnectionName}'. Skipping model '{ModelName}'.",
                                     connectionConfig.ServiceType, modelConfig.Connection, modelName);
                }
            }
            catch (KeyNotFoundException knfex)
            {
                _logger.LogError(knfex, "Connection '{ConnectionName}' required by model '{ModelName}' not found for agent '{AgentName}'. Skipping this model.",
                                 modelConfig.Connection, modelName, agentName);
            }
            catch (Exception ex)
            {
                // Catch errors during connection resolution or service configuration
                _logger.LogError(ex, "Failed to configure AI service for model '{ModelName}' (Connection: {ConnectionName}) for agent '{AgentName}'. Skipping this model.",
                                 modelName, modelConfig.Connection, agentName);
            }
        }
    }

    private Task LoadPluginsAsync(Kernel kernel, AgentConfig agentConfig, string agentName)
    {
        if (agentConfig.Plugins == null || !agentConfig.Plugins.Any())
        {
            _logger.LogInformation("No plugins specified for agent '{AgentName}'.", agentName);
            return Task.CompletedTask;
        }

        string agentDirectory = Path.GetFullPath(Path.Combine(_providerOptions.AgentsDirectory, agentName), AppContext.BaseDirectory);
        string? globalPluginsDirectory = !string.IsNullOrWhiteSpace(_providerOptions.GlobalPluginsDirectory)
            ? Path.GetFullPath(_providerOptions.GlobalPluginsDirectory, AppContext.BaseDirectory)
            : null;

        _logger.LogInformation("Loading plugins for agent '{AgentName}'. Agent Dir: {AgentDir}, Global Plugin Dir: {GlobalDir}",
                             agentName, agentDirectory, globalPluginsDirectory ?? "N/A");

        foreach (var pluginReference in agentConfig.Plugins)
        {
            if (string.IsNullOrWhiteSpace(pluginReference)) continue;

            try
            {
                string? resolvedPath = ResolvePluginPath(pluginReference, agentDirectory, globalPluginsDirectory);

                 if (resolvedPath != null && Directory.Exists(resolvedPath)) // Only handle prompt directories for now
                 {
                    string pluginName = Path.GetFileName(resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    _logger.LogDebug("Importing plugin '{PluginName}' from directory: {Path}", pluginName, resolvedPath);
                    kernel.ImportPluginFromPromptDirectory(resolvedPath, pluginName);
                 }
                 // TODO: Add support for OpenAPI plugins (.yaml/.json) or Native plugins (by Type name) here if needed
                 // else if (resolvedPath != null && File.Exists(resolvedPath) && (resolvedPath.EndsWith(".yaml") || resolvedPath.EndsWith(".json")))
                 // {
                 //     string pluginName = Path.GetFileNameWithoutExtension(resolvedPath);
                 //     _logger.LogDebug("Importing OpenAPI plugin '{PluginName}' from file: {Path}", pluginName, resolvedPath);
                 //     // Note: OpenAPI import might require different parameters or async handling
                 //     // await kernel.ImportPluginFromOpenApiAsync(...);
                 // }
                 // else
                 // {
                 //     _logger.LogWarning("Could not find plugin directory or supported file for reference '{PluginReference}' for agent '{AgentName}'. Looked in: {ResolvedPath}", pluginReference, agentName, resolvedPath ?? pluginReference);
                 // }
                 else
                 {
                      _logger.LogWarning("Could not find plugin directory for reference '{PluginReference}' for agent '{AgentName}'. Looked in agent dir ('{AgentDir}') and global dir ('{GlobalDir}'). Prompt directory plugins are currently supported.", 
                                       pluginReference, agentName, agentDirectory, globalPluginsDirectory ?? "N/A");
                 }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin reference '{PluginReference}' for agent '{AgentName}'.", pluginReference, agentName);
                // Continue trying to load other plugins
            }
        }

        return Task.CompletedTask;
    }

    private string? ResolvePluginPath(string pluginReference, string agentDirectory, string? globalPluginsDirectory)
    {
        // Simplistic resolution: Check agent-relative first, then global-relative.
        // Assumes pluginReference is a directory name or relative path to a directory.

        // Clean the reference (e.g., remove leading './')
        var cleanedReference = pluginReference.TrimStart('.', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string potentialAgentRelativePath = Path.Combine(agentDirectory, cleanedReference);
        if (Directory.Exists(potentialAgentRelativePath))
        {
            return Path.GetFullPath(potentialAgentRelativePath);
        }

        if (globalPluginsDirectory != null)
        {
             string potentialGlobalRelativePath = Path.Combine(globalPluginsDirectory, cleanedReference);
             if (Directory.Exists(potentialGlobalRelativePath))
             {
                 return Path.GetFullPath(potentialGlobalRelativePath);
             }
        }

        return null; // Path not found as a directory in either location
    }
} 