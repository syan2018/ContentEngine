using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Services.Loaders;     // Added
using ConfigurableAIProvider.Services.Providers;   // Added
using ConfigurableAIProvider.Services.Configurators; // Added
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; // Still needed for GlobalPluginsDirectory from ConfigurableAIOptions

namespace ConfigurableAIProvider.Services;

/// <summary>
/// Default implementation of IAIKernelFactory that builds Kernels based on Agent configurations 
/// using pluggable service configurators.
/// </summary>
public class DefaultAIKernelFactory(
    IAgentConfigLoader agentConfigLoader,
    IConnectionProvider connectionProvider,
    IModelProvider modelProvider,
    IEnumerable<IAIServiceConfigurator> serviceConfigurators, // Changed
    ILoggerFactory loggerFactory, // Inject ILoggerFactory
    IOptions<ConfigurableAIOptions> providerOptions, // Inject provider options
    ILogger<DefaultAIKernelFactory> logger)
    : IAIKernelFactory
{

    // Use ILoggerFactory to create Kernel logger
    private readonly ConfigurableAIOptions _providerOptions = providerOptions.Value; // Keep options for global plugin path etc.


    // Remove IOptions<AIServiceOptions> and IPluginLoader dependencies

    public async Task<Kernel> BuildKernelAsync(string agentName)
    {
        logger.LogInformation("Building Kernel for agent: {AgentName}", agentName);

        AgentConfig agentConfig = await LoadAgentConfigurationAsync(agentName);

        var builder = Kernel.CreateBuilder();

        ConfigureKernelLogging(builder, agentConfig);

        await ConfigureAiServicesAsync(builder, agentConfig, agentName);

        var kernel = builder.Build();

        await LoadPluginsAsync(kernel, agentConfig, agentName);
        
        logger.LogInformation("Successfully built Kernel for agent: {AgentName}", agentName);
        return kernel;
    }

    // --- Private Helper Methods ---

    private async Task<AgentConfig> LoadAgentConfigurationAsync(string agentName)
    {
        try
        {
            return await agentConfigLoader.LoadConfigAsync(agentName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load configuration for agent: {AgentName}", agentName);
            throw; // Re-throw exceptions related to config loading
        }
    }

    private void ConfigureKernelLogging(IKernelBuilder builder, AgentConfig agentConfig)
    {
         if (agentConfig.LogLevel.HasValue)
         {
             // Note: As mentioned before, SK's builder logging config might change.
             logger.LogWarning("LogLevel from agent.yaml is currently informational and may not directly control Kernel's internal logging level through KernelBuilder. Kernel will use the globally configured logger factory.");
             // You might need more specific configuration depending on the SK version and logging setup.
             // builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory); // Example if needed
         }
         else
         {
              // Use the host application's logging by default
              builder.Services.AddSingleton<ILoggerFactory>(loggerFactory); 
         }
    }

    private async Task ConfigureAiServicesAsync(IKernelBuilder builder, AgentConfig agentConfig, string agentName)
    {
        if (agentConfig.Models == null || !agentConfig.Models.Any())
        { 
            logger.LogWarning("No models specified for agent '{AgentName}'. Kernel will be built without AI services.", agentName);
            return;
        }

        foreach (var modelEntry in agentConfig.Models)
        {
            var logicalModelName = modelEntry.Key;
            var modelDefinitionId = modelEntry.Value;

            if (string.IsNullOrWhiteSpace(modelDefinitionId))
            {
                logger.LogWarning("Skipping logical model '{LogicalModelName}' for agent '{AgentName}' due to missing model definition ID reference.", logicalModelName, agentName);
                continue;
            }

            try
            {
                // 1. Get the full Model Definition from the ID
                ModelDefinition modelDefinition = await modelProvider.GetModelDefinitionAsync(modelDefinitionId);

                // 2. Get the resolved Connection Configuration
                if (string.IsNullOrWhiteSpace(modelDefinition.Connection))
                {
                     logger.LogWarning("Skipping model definition '{ModelDefinitionId}' referenced by agent '{AgentName}' (logical name '{LogicalModelName}') because it's missing a 'connection' property.", 
                                      modelDefinitionId, agentName, logicalModelName);
                    continue;
                }
                ConnectionConfig connectionConfig = await connectionProvider.GetResolvedConnectionAsync(modelDefinition.Connection);
                
                // 3. Find the appropriate configurator based on the *connection's* service type
                var configurator = serviceConfigurators.FirstOrDefault(c => c.HandledServiceType == connectionConfig.ServiceType);

                if (configurator != null)
                {
                    // 4. Configure the service using the ModelDefinition and ConnectionConfig
                    logger.LogDebug("Using {ConfiguratorType} to configure AI service for logical model '{LogicalModelName}' (Definition ID: '{ModelDefinitionId}', Actual Model ID: {ActualModelId}) using connection '{ConnectionName}' for agent '{AgentName}'.",
                                    configurator.GetType().Name, logicalModelName, modelDefinitionId, modelDefinition.ModelId, modelDefinition.Connection, agentName);
                    configurator.ConfigureService(builder, modelDefinition, connectionConfig);
                }
                else
                {
                    logger.LogWarning("No service configurator found for ServiceType '{ServiceType}' used by connection '{ConnectionName}' (referenced by model definition '{ModelDefinitionId}'). Skipping logical model '{LogicalModelName}'.",
                                     connectionConfig.ServiceType, modelDefinition.Connection, modelDefinitionId, logicalModelName);
                }
            }
            catch (KeyNotFoundException knfex)
            {
                // Handle cases where ModelDefinition ID or Connection Name isn't found
                logger.LogError(knfex, "Configuration lookup failed for logical model '{LogicalModelName}' (referenced ID '{ModelDefinitionId}') in agent '{AgentName}'. Check models.yaml and connections.yaml. Skipping this model.",
                                 logicalModelName, modelDefinitionId, agentName);
            }
            catch (Exception ex)
            {
                // Catch other errors during resolution or configuration
                logger.LogError(ex, "Failed to configure AI service for logical model '{LogicalModelName}' (Definition ID: {ModelDefinitionId}) for agent '{AgentName}'. Skipping this model.",
                                 logicalModelName, modelDefinitionId, agentName);
            }
        }
    }

    private Task LoadPluginsAsync(Kernel kernel, AgentConfig agentConfig, string agentName)
    {
        if (agentConfig.Plugins == null || !agentConfig.Plugins.Any())
        {
            logger.LogInformation("No plugins specified for agent '{AgentName}'.", agentName);
            return Task.CompletedTask;
        }

        string agentDirectory = Path.GetFullPath(Path.Combine(_providerOptions.AgentsDirectory, agentName), AppContext.BaseDirectory);
        string? globalPluginsDirectory = !string.IsNullOrWhiteSpace(_providerOptions.GlobalPluginsDirectory)
            ? Path.GetFullPath(_providerOptions.GlobalPluginsDirectory, AppContext.BaseDirectory)
            : null;

        logger.LogInformation("Loading plugins for agent '{AgentName}'. Agent Dir: {AgentDir}, Global Plugin Dir: {GlobalDir}",
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
                    logger.LogDebug("Importing plugin '{PluginName}' from directory: {Path}", pluginName, resolvedPath);
                    kernel.ImportPluginFromPromptDirectory(resolvedPath, pluginName);
                 }
                 else
                 {
                      logger.LogWarning("Could not find plugin directory for reference '{PluginReference}' for agent '{AgentName}'. Looked in agent dir ('{AgentDir}') and global dir ('{GlobalDir}'). Prompt directory plugins are currently supported.", 
                                       pluginReference, agentName, agentDirectory, globalPluginsDirectory ?? "N/A");
                 }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load plugin reference '{PluginReference}' for agent '{AgentName}'.", pluginReference, agentName);
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
        var cleanedReference = pluginReference.TrimStart('.').TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

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