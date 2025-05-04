using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Services.Loaders;     // Added
using ConfigurableAIProvider.Services.Providers;   // Added
using ConfigurableAIProvider.Services.Configurators; // Added
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; // Still needed for GlobalPluginsDirectory from ConfigurableAIOptions
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System;
using ConfigurableAIProvider.Extensions; // Ensure KernelExtensions are available

namespace ConfigurableAIProvider.Services;

/// <summary>
/// Default implementation of IAIKernelFactory that builds Kernels based on Agent configurations 
/// using pluggable service configurators.
/// </summary>
public class DefaultAIKernelFactory : IAIKernelFactory
{
    private readonly IAgentConfigLoader _agentConfigLoader;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IModelProvider _modelProvider;
    private readonly IEnumerable<IAIServiceConfigurator> _serviceConfigurators;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConfigurableAIOptions _providerOptions;
    private readonly ILogger<DefaultAIKernelFactory> _logger;

    public DefaultAIKernelFactory(
        IAgentConfigLoader agentConfigLoader,
        IConnectionProvider connectionProvider,
        IModelProvider modelProvider,
        IEnumerable<IAIServiceConfigurator> serviceConfigurators,
        IOptions<ConfigurableAIOptions> providerOptions,
        ILoggerFactory loggerFactory)
    {
        _agentConfigLoader = agentConfigLoader;
        _connectionProvider = connectionProvider;
        _modelProvider = modelProvider;
        _serviceConfigurators = serviceConfigurators;
        _providerOptions = providerOptions.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DefaultAIKernelFactory>();
    }

    /// <inheritdoc/>
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
             _logger.LogWarning("Agent-specific LogLevel ({AgentLogLevel}) in agent.yaml is currently informational. Kernel logging uses the ILoggerFactory provided during construction.");
             // Semantic Kernel's internal logging level is primarily controlled by the configured ILoggerFactory.
             // Setting a minimum level *here* on the builder might not have the intended effect on all SK logs.
         }
         else
         {
              // Use the host application's logging by default
              builder.Services.AddSingleton(_loggerFactory); 
         }
    }

    private async Task ConfigureAiServicesAsync(IKernelBuilder builder, AgentConfig agentConfig, string agentName)
    {
        if (agentConfig.Models == null || !agentConfig.Models.Any())
        { 
            _logger.LogWarning("No models specified for agent '{AgentName}'. Kernel will be built without AI services.", agentName);
            return;
        }

        foreach (var modelEntry in agentConfig.Models)
        {
            var logicalModelName = modelEntry.Key;
            var modelDefinitionId = modelEntry.Value;

            if (string.IsNullOrWhiteSpace(modelDefinitionId))
            {
                _logger.LogWarning("Skipping logical model '{LogicalModelName}' for agent '{AgentName}' due to missing model definition ID reference.", logicalModelName, agentName);
                continue;
            }

            try
            {
                // 1. Get the full Model Definition from the ID
                ModelDefinition modelDefinition = await _modelProvider.GetModelDefinitionAsync(modelDefinitionId);

                // 2. Get the resolved Connection Configuration
                if (string.IsNullOrWhiteSpace(modelDefinition.Connection))
                {
                     _logger.LogWarning("Skipping model definition '{ModelDefinitionId}' referenced by agent '{AgentName}' (logical name '{LogicalModelName}') because it's missing a 'connection' property.", 
                                      modelDefinitionId, agentName, logicalModelName);
                    continue;
                }
                ConnectionConfig connectionConfig = await _connectionProvider.GetResolvedConnectionAsync(modelDefinition.Connection);
                
                // 3. Find the appropriate configurator based on the *connection's* service type
                var configurator = _serviceConfigurators.FirstOrDefault(c => c.HandledServiceType == connectionConfig.ServiceType);

                if (configurator != null)
                {
                    // 4. Configure the service using the ModelDefinition and ConnectionConfig
                    _logger.LogDebug("Using {ConfiguratorType} to configure AI service for model definition '{ModelDefinitionId}' (Actual Model ID: {ActualModelId}) using connection '{ConnectionName}' for agent '{AgentName}'.",
                                    configurator.GetType().Name, modelDefinitionId, modelDefinition.ModelId, modelDefinition.Connection, agentName);
                    configurator.ConfigureService(builder, modelDefinition, connectionConfig);
                }
                else
                {
                    _logger.LogWarning("No service configurator found for ServiceType '{ServiceType}' used by connection '{ConnectionName}' (referenced by model definition '{ModelDefinitionId}'). Skipping logical model '{LogicalModelName}'.",
                                     connectionConfig.ServiceType, modelDefinition.Connection, modelDefinitionId, logicalModelName);
                }
            }
            catch (KeyNotFoundException knfex)
            {
                // Handle cases where ModelDefinition ID or Connection Name isn't found
                _logger.LogError(knfex, "Configuration lookup failed for logical model '{LogicalModelName}' (referenced Model Definition ID '{ModelDefinitionId}' or Connection '{ConnectionName}') in agent '{AgentName}'. Check models.yaml and connections.yaml. Skipping this model.",
                                 logicalModelName, modelDefinitionId, agentConfig.Models.GetValueOrDefault(logicalModelName), agentName);
            }
            catch (Exception ex)
            {
                // Catch other errors during resolution or configuration
                _logger.LogError(ex, "Failed to configure AI service for logical model '{LogicalModelName}' (Definition ID: {ModelDefinitionId}) for agent '{AgentName}'. Skipping this model.",
                                 logicalModelName, modelDefinitionId, agentName);
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

        // --- Determine Base Paths ---
        // Agent-specific plugins are expected within the agent's config directory.
        // IAgentConfigLoader should provide the base path for the *loaded* agent config.
        string agentOutputDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _providerOptions.AgentsDirectory, agentName));
        
        // Global plugins path from options (relative to AppContext.BaseDirectory)
        string? globalPluginsOutputDirectory = !string.IsNullOrWhiteSpace(_providerOptions.GlobalPluginsDirectory)
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _providerOptions.GlobalPluginsDirectory))
            : null;
        _logger.LogDebug("Global plugins base directory: {GlobalDir}", globalPluginsOutputDirectory ?? "N/A");
        // --- End Base Paths ---


        _logger.LogInformation("Loading plugins for agent '{AgentName}'.", agentName);

        foreach (var pluginReference in agentConfig.Plugins)
        {
            if (string.IsNullOrWhiteSpace(pluginReference)) continue;

            string? resolvedPluginConfigPath = null;
            try
            {
                // Resolve the path to the plugin directory first
                string? pluginDirectoryPath = ResolvePluginDirectoryPath(pluginReference, agentOutputDirectory, globalPluginsOutputDirectory);

                if (pluginDirectoryPath != null)
                {
                     resolvedPluginConfigPath = Path.Combine(pluginDirectoryPath, "plugin.yaml"); // Expected config file name

                     if (File.Exists(resolvedPluginConfigPath))
                     {
                         _logger.LogInformation("Attempting to load plugin for agent '{AgentName}' from config: {PluginConfigPath}", agentName, resolvedPluginConfigPath);
                        
                         // Use the ImportPluginFromConfig extension method
                         var plugin = ConfigurableAIProvider.Extensions.KernelExtensions.ImportPluginFromConfig(
                             kernel, 
                             resolvedPluginConfigPath, 
                             _loggerFactory.CreateLogger(nameof(ConfigurableAIProvider.Extensions.KernelExtensions)));
                        
                         // KernelExtensions.ImportPluginFromConfig returns the plugin. 
                         // In SK 1.x, KernelPluginFactory.CreateFromFunctions doesn't automatically add it to kernel.Plugins.
                         // We might need to add it manually if it's not accessible otherwise.
                         if (plugin != null && !kernel.Plugins.Contains(plugin.Name))
                         {
                              kernel.Plugins.Add(plugin); // Manually add the created plugin to the kernel's collection
                             _logger.LogDebug("Successfully imported and added plugin '{PluginName}' via config for agent '{AgentName}'.", plugin.Name, agentName);
                         }
                         else if (plugin != null)
                         {
                             _logger.LogDebug("Plugin '{PluginName}' was already present in the kernel after import from config.", plugin.Name);
                         }
                         // If plugin is null, ImportPluginFromConfig would have logged the error.
                     }
                     else
                     {
                          _logger.LogWarning("Plugin configuration file 'plugin.yaml' not found in resolved directory: {PluginDirectoryPath} (from reference '{PluginReference}') for agent '{AgentName}'. Cannot load plugin via config. Skipping.", 
                                           pluginDirectoryPath, pluginReference, agentName);
                     }
                }
                else
                {
                     _logger.LogWarning("Could not resolve plugin directory for reference '{PluginReference}' for agent '{AgentName}'. Looked relative to agent config dir ('{AgentBasePath}') and global dir ('{GlobalBasePath}'). Skipping plugin.", 
                                      pluginReference, agentName, agentOutputDirectory, globalPluginsOutputDirectory ?? "N/A");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from reference '{PluginReference}' (resolved config path: {ResolvedPath}) for agent '{AgentName}'. Skipping this plugin.", 
                                 pluginReference, resolvedPluginConfigPath ?? "[Resolution Failed]", agentName);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves the full path to a plugin directory based on a reference string.
    /// Checks relative to the agent's config directory first, then the global directory.
    /// </summary>
    private string? ResolvePluginDirectoryPath(string pluginReference, string agentConfigDirectory, string? globalPluginsDirectory)
    {
        // Clean the reference (remove leading ./ or /)
        var cleanedReference = pluginReference.TrimStart('.').Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // 1. Check relative to the *agent's configuration* directory
        string potentialAgentRelativePath = Path.Combine(agentConfigDirectory, cleanedReference);
        if (Directory.Exists(potentialAgentRelativePath))
        {
            _logger.LogDebug("Resolved plugin reference '{Reference}' relative to agent config directory: {FullPath}", pluginReference, Path.GetFullPath(potentialAgentRelativePath));
            return Path.GetFullPath(potentialAgentRelativePath);
        }

        // 2. Check relative to the global plugins directory (if configured)
        if (globalPluginsDirectory != null)
        {
             string potentialGlobalRelativePath = Path.Combine(globalPluginsDirectory, cleanedReference);
             if (Directory.Exists(potentialGlobalRelativePath))
             {
                 _logger.LogDebug("Resolved plugin reference '{Reference}' relative to global plugins directory: {FullPath}", pluginReference, Path.GetFullPath(potentialGlobalRelativePath));
                 return Path.GetFullPath(potentialGlobalRelativePath);
             }
        }

        // 3. Log if not found in either expected location
        _logger.LogTrace("Plugin reference '{Reference}' not found relative to agent config directory ({AgentDir}) or global plugins directory ({GlobalDir})", 
                         pluginReference, agentConfigDirectory, globalPluginsDirectory ?? "N/A");

        return null; // Path not found
    }
} 