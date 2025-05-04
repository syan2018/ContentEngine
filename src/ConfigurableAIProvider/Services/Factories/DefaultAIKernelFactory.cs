using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Models;
using ConfigurableAIProvider.Services.Configurators;
using ConfigurableAIProvider.Services.Loaders;
using ConfigurableAIProvider.Services.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using CaipExtensions = ConfigurableAIProvider.Extensions;

// Ensure KernelExtensions are available

namespace ConfigurableAIProvider.Services.Factories;

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
                ModelConfig modelConfig = await _modelProvider.GetModelDefinitionAsync(modelDefinitionId);

                // 2. Get the resolved Connection Configuration
                if (string.IsNullOrWhiteSpace(modelConfig.Connection))
                {
                     _logger.LogWarning("Skipping model definition '{ModelDefinitionId}' referenced by agent '{AgentName}' (logical name '{LogicalModelName}') because it's missing a 'connection' property.", 
                                      modelDefinitionId, agentName, logicalModelName);
                    continue;
                }
                ConnectionConfig connectionConfig = await _connectionProvider.GetResolvedConnectionAsync(modelConfig.Connection);
                
                // 3. Find the appropriate configurator based on the *connection's* service type
                var configurator = _serviceConfigurators.FirstOrDefault(c => c.HandledServiceType == connectionConfig.ServiceType);

                if (configurator != null)
                {
                    // 4. Configure the service using the ModelConfig and ConnectionConfig
                    _logger.LogDebug("Using {ConfiguratorType} to configure AI service for model definition '{ModelDefinitionId}' (Actual Model ID: {ActualModelId}) using connection '{ConnectionName}' for agent '{AgentName}'.",
                                    configurator.GetType().Name, modelDefinitionId, modelConfig.ModelId, modelConfig.Connection, agentName);
                    configurator.ConfigureService(builder, modelConfig, connectionConfig);
                }
                else
                {
                    _logger.LogWarning("No service configurator found for ServiceType '{ServiceType}' used by connection '{ConnectionName}' (referenced by model definition '{ModelDefinitionId}'). Skipping logical model '{LogicalModelName}'.",
                                     connectionConfig.ServiceType, modelConfig.Connection, modelDefinitionId, logicalModelName);
                }
            }
            catch (KeyNotFoundException knfex)
            {
                // Handle cases where ModelConfig ID or Connection Name isn't found
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

    /// <summary>
    /// Loads plugins specified in the agent configuration into the kernel.
    /// It resolves plugin paths relative to the agent's directory and the global plugins directory.
    /// It expects each plugin directory to contain a PluginName.yaml configuration file.
    /// </summary>
    private async Task LoadPluginsAsync(Kernel kernel, AgentConfig agentConfig, string agentName)
    {
        if (agentConfig.Plugins == null || !agentConfig.Plugins.Any())
        {   
            _logger.LogInformation("No plugins specified in configuration for agent '{AgentName}'.", agentName);
            await Task.CompletedTask; // Use await Task.CompletedTask for async method without async ops
            return;
        }

        // Determine base paths for plugin resolution
        string agentOutputDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _providerOptions.AgentsDirectory, agentName));
        string? globalPluginsOutputDirectory = !string.IsNullOrWhiteSpace(_providerOptions.GlobalPluginsDirectory)
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _providerOptions.GlobalPluginsDirectory))
            : null;
        _logger.LogDebug("Agent-specific config base directory: {AgentDir}", agentOutputDirectory);
        _logger.LogDebug("Global plugins base directory: {GlobalDir}", globalPluginsOutputDirectory ?? "N/A");

        _logger.LogInformation("Loading {PluginCount} plugin reference(s) for agent '{AgentName}'.", agentConfig.Plugins.Count, agentName);

        // Use the logger factory to create logger for the aliased extensions type
        var kernelExtensionsLogger = _loggerFactory.CreateLogger(typeof(CaipExtensions.KernelExtensions).FullName ?? "CaipKernelExtensions");

        foreach (var pluginReference in agentConfig.Plugins)
        {
            if (string.IsNullOrWhiteSpace(pluginReference)) 
            {
                _logger.LogWarning("Empty plugin reference found for agent '{AgentName}', skipping.", agentName);
                continue;
            }

            string? resolvedPluginDirectoryPath = null;
            string? resolvedPluginConfigPath = null;
            string pluginLogPrefix = $"Agent '{agentName}', Plugin Ref '{pluginReference}'"; // Prefix for logs related to this reference

            try
            {
                // 1. Resolve the plugin directory path
                resolvedPluginDirectoryPath = ResolvePluginDirectoryPath(pluginReference, agentOutputDirectory, globalPluginsOutputDirectory);

                if (resolvedPluginDirectoryPath == null)
                {
                    _logger.LogWarning("{PluginLogPrefix}: Could not resolve plugin directory. Looked relative to agent config dir ('{AgentBasePath}') and global dir ('{GlobalBasePath}'). Skipping plugin.", 
                                     pluginLogPrefix, agentOutputDirectory, globalPluginsOutputDirectory ?? "N/A");
                    continue; // Skip if directory not found
                }

                // 2. Determine the expected configuration file path (PluginName.yaml)
                string potentialPluginName = Path.GetFileName(resolvedPluginDirectoryPath); // Infer name from dir
                resolvedPluginConfigPath = Path.Combine(resolvedPluginDirectoryPath, $"{potentialPluginName}.yaml"); 
                _logger.LogDebug("{PluginLogPrefix}: Resolved directory to '{ResolvedDir}'. Expecting config file at '{ResolvedConfigPath}'.", 
                                 pluginLogPrefix, resolvedPluginDirectoryPath, resolvedPluginConfigPath);


                // 3. Check if the config file exists
                if (!File.Exists(resolvedPluginConfigPath))
                {
                    _logger.LogWarning("{PluginLogPrefix}: Expected plugin configuration file not found at '{ResolvedConfigPath}'. Skipping plugin load via this reference.", 
                                     pluginLogPrefix, resolvedPluginConfigPath);
                    continue; // Skip if config file doesn't exist
                }

                // 4. Attempt to load the plugin using the Kernel Extension method (using the alias)
                _logger.LogInformation("{PluginLogPrefix}: Attempting to load plugin from config: {PluginConfigPath}", pluginLogPrefix, resolvedPluginConfigPath);
                
                KernelPlugin? plugin = CaipExtensions.KernelExtensions.ImportPluginFromConfig(
                    kernel, 
                    resolvedPluginConfigPath, 
                    resolvedPluginDirectoryPath, 
                    kernelExtensionsLogger); 

                // 5. Register the loaded plugin with the kernel if successful
                if (plugin != null)
                {
                    if (!kernel.Plugins.Contains(plugin.Name))
                    {
                        kernel.Plugins.Add(plugin); // Add the loaded plugin to the kernel's collection
                        _logger.LogInformation("{PluginLogPrefix}: Successfully imported and added plugin '{PluginName}' ({FunctionCount} functions) to kernel.", 
                                             pluginLogPrefix, plugin.Name, plugin.Count());
                    }
                    else
                    {
                        // This might happen if different references lead to the same plugin name being loaded
                        _logger.LogWarning("{PluginLogPrefix}: Plugin with name '{PluginName}' was already present in the kernel. Skipping duplicate addition from config '{ResolvedConfigPath}'.", 
                                         pluginLogPrefix, plugin.Name, resolvedPluginConfigPath);
                    }
                }
                // If plugin is null, the KernelExtension method already logged the reason for failure.
            }
            catch (Exception ex) // Catch unexpected errors during the process for this plugin reference
            {
                _logger.LogError(ex, "{PluginLogPrefix}: An unexpected error occurred while processing plugin reference (Resolved Dir: {ResolvedDir}, Config: {ResolvedConfig}). Skipping this plugin reference.", 
                                 pluginLogPrefix, resolvedPluginDirectoryPath ?? "[Dir Resolution Failed]", resolvedPluginConfigPath ?? "[Cfg Resolution Failed]");
            }
        }
        await Task.CompletedTask; // Added await here
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