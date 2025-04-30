using ConfigurableAIProvider.Configuration;
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
/// Default implementation of IAIKernelFactory that builds Kernels based on Agent configurations.
/// </summary>
public class DefaultAIKernelFactory : IAIKernelFactory
{
    private readonly IAgentConfigLoader _agentConfigLoader;
    private readonly IConnectionProvider _connectionProvider;
    private readonly ILoggerFactory _loggerFactory; // Use ILoggerFactory to create Kernel logger
    private readonly ILogger<DefaultAIKernelFactory> _logger;
    private readonly ConfigurableAIOptions _providerOptions; // Keep options for global plugin path etc.


    // Remove IOptions<AIServiceOptions> and IPluginLoader dependencies
    public DefaultAIKernelFactory(
        IAgentConfigLoader agentConfigLoader,
        IConnectionProvider connectionProvider,
        ILoggerFactory loggerFactory, // Inject ILoggerFactory
        IOptions<ConfigurableAIOptions> providerOptions, // Inject provider options
        ILogger<DefaultAIKernelFactory> logger)
    {
        _agentConfigLoader = agentConfigLoader;
        _connectionProvider = connectionProvider;
        _loggerFactory = loggerFactory;
        _providerOptions = providerOptions.Value;
        _logger = logger;
    }

    public async Task<Kernel> BuildKernelAsync(string agentName)
    {
        _logger.LogInformation("Building Kernel for agent: {AgentName}", agentName);

        AgentConfig agentConfig;
        try
        {
            agentConfig = await _agentConfigLoader.LoadConfigAsync(agentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration for agent: {AgentName}", agentName);
            throw; // Re-throw exceptions related to config loading
        }

        var builder = Kernel.CreateBuilder();

        // Configure logging for the Kernel itself using the injected factory
         if (agentConfig.LogLevel.HasValue)
         {
             // TODO: SK logging configuration changed. Need to adapt.
             // The old builder.Services.AddLogging(...) might not be the primary way.
             // Kernels now often accept an ILoggerFactory in their constructor or builder methods.
             // Let's add the logger factory directly if possible.
             // builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory); // This might be needed depending on SK version
             _logger.LogWarning("Semantic Kernel logging configuration has changed. LogLevel from agent.yaml might not be directly applied via KernelBuilder in recent versions. Kernel will use the globally configured logger factory.");
         }
         else
         {
              builder.Services.AddSingleton<ILoggerFactory>(_loggerFactory); // Use host app's logging by default
         }


        // Configure AI Services (Models)
        if (agentConfig.Models != null && agentConfig.Models.Any())
        {
            foreach (var modelEntry in agentConfig.Models)
            {
                var modelName = modelEntry.Key; // Logical name within the agent config
                var modelConfig = modelEntry.Value;

                if (string.IsNullOrWhiteSpace(modelConfig.Connection) || string.IsNullOrWhiteSpace(modelConfig.ModelId))
                {
                    _logger.LogWarning("Skipping model '{ModelName}' for agent '{AgentName}' due to missing connection name or model ID.", modelName, agentName);
                    continue;
                }

                try
                {
                    ConnectionConfig connectionConfig = await _connectionProvider.GetResolvedConnectionAsync(modelConfig.Connection);

                    _logger.LogDebug("Adding AI service for model '{ModelName}' (ID: {ModelId}) using connection '{ConnectionName}' for agent '{AgentName}'.",
                                     modelName, modelConfig.ModelId, modelConfig.Connection, agentName);

                    switch (connectionConfig.ServiceType)
                    {
                        case ServiceType.AzureOpenAI:
                            AddAzureOpenAIService(builder, modelConfig, connectionConfig);
                            break;
                        case ServiceType.OpenAI:
                            AddOpenAIService(builder, modelConfig, connectionConfig);
                            break;
                        default:
                            _logger.LogWarning("Unsupported service type '{ServiceType}' configured for connection '{ConnectionName}'.",
                                             connectionConfig.ServiceType, modelConfig.Connection);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to configure AI service for model '{ModelName}' (Connection: {ConnectionName}) for agent '{AgentName}'. Skipping this model.",
                                     modelName, modelConfig.Connection, agentName);
                    // Continue trying to build with other models/plugins
                }
            }
        }
        else
        {
            _logger.LogWarning("No models configured for agent '{AgentName}'. Kernel will be built without AI services.", agentName);
        }

        var kernel = builder.Build();

        // Load Plugins
        if (agentConfig.Plugins != null && agentConfig.Plugins.Any())
        {
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
                    // Determine plugin path:
                    // 1. Try relative to agent directory
                    // 2. Try relative to global plugins directory (if configured)
                    // 3. Treat as potentially just a plugin name (if loading native plugins or from assembly in future)

                    string potentialAgentRelativePath = Path.GetFullPath(Path.Combine(agentDirectory, pluginReference));
                    string potentialGlobalRelativePath = globalPluginsDirectory != null ? Path.GetFullPath(Path.Combine(globalPluginsDirectory, pluginReference)) : null;

                    string pluginPathToLoad;
                    string pluginName;


                     if (Directory.Exists(potentialAgentRelativePath)) // Assume directory-based prompt plugin relative to agent
                     {
                        pluginPathToLoad = potentialAgentRelativePath;
                        pluginName = Path.GetFileName(pluginPathToLoad.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                         _logger.LogDebug("Attempting to load plugin '{PluginName}' from agent-relative directory: {Path}", pluginName, pluginPathToLoad);
                        kernel.ImportPluginFromPromptDirectory(pluginPathToLoad, pluginName);
                     }
                     else if (potentialGlobalRelativePath != null && Directory.Exists(potentialGlobalRelativePath)) // Assume directory-based prompt plugin relative to global dir
                     {
                        pluginPathToLoad = potentialGlobalRelativePath;
                        pluginName = Path.GetFileName(pluginPathToLoad.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        _logger.LogDebug("Attempting to load plugin '{PluginName}' from global directory: {Path}", pluginName, pluginPathToLoad);
                        kernel.ImportPluginFromPromptDirectory(pluginPathToLoad, pluginName);
                     }
                     // TODO: Add support for OpenAPI plugins (.yaml/.json) or Native plugins (by Type name) here if needed
                     // else if (File.Exists(...) && (pluginReference.EndsWith(".yaml") || pluginReference.EndsWith(".json"))) { ... ImportPluginFromOpenApi ... }
                     // else { ... try loading native plugin by name ... kernel.ImportPluginFromType<T>(pluginReference) ... }
                     else
                     {
                         _logger.LogWarning("Could not find plugin directory or supported file for reference '{PluginReference}' for agent '{AgentName}'. Looked in agent dir and global dir (if configured).", pluginReference, agentName);
                     }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin reference '{PluginReference}' for agent '{AgentName}'.", pluginReference, agentName);
                    // Continue trying to load other plugins
                }
            }
        }
         else
        {
             _logger.LogInformation("No plugins specified for agent '{AgentName}'.", agentName);
        }

        _logger.LogInformation("Successfully built Kernel for agent: {AgentName}", agentName);
        return kernel;
    }


    // --- Helper methods for adding services ---

    private void AddAzureOpenAIService(IKernelBuilder builder, AgentConfig.ModelConfig modelConfig, ConnectionConfig connectionConfig)
    {
        // Ensure required fields are present (already partially validated in ConnectionProvider)
         if (string.IsNullOrWhiteSpace(connectionConfig.Endpoint) || string.IsNullOrWhiteSpace(connectionConfig.ApiKey))
         {
             _logger.LogError("Cannot add Azure OpenAI service for model '{ModelId}'. Endpoint or ApiKey is missing after resolution for connection '{ConnectionName}'.",
                              modelConfig.ModelId, modelConfig.Connection);
             return;
         }


        switch (modelConfig.EndpointType)
        {
            case EndpointType.ChatCompletion:
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: modelConfig.ModelId!, // Should not be null here based on prior checks
                    endpoint: connectionConfig.Endpoint,
                    apiKey: connectionConfig.ApiKey
                    // serviceId: modelConfig.Connection // Optional: use connection name as serviceId?
                );
                 _logger.LogDebug("Added Azure OpenAI Chat Completion: Deployment={Deployment}, Endpoint={Endpoint}", modelConfig.ModelId, connectionConfig.Endpoint);
                break;
            case EndpointType.TextCompletion:
                 // Add Text Completion if needed - often superseded by Chat
                _logger.LogWarning("Azure OpenAI Text Completion endpoint type specified but not implemented in factory. Use Chat Completion or Embedding.");
                 break;
            default:
                _logger.LogWarning("Unsupported Azure OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                 modelConfig.EndpointType, modelConfig.ModelId);
                break;
        }
    }

    private void AddOpenAIService(IKernelBuilder builder, AgentConfig.ModelConfig modelConfig, ConnectionConfig connectionConfig)
    {
        if (string.IsNullOrWhiteSpace(connectionConfig.ApiKey))
        {
            _logger.LogError("Cannot add OpenAI service for model '{ModelId}'. ApiKey is missing after resolution for connection '{ConnectionName}'.",
                             modelConfig.ModelId, modelConfig.Connection);
            return;
        }

        switch (modelConfig.EndpointType)
        {
            case EndpointType.ChatCompletion:
                builder.AddOpenAIChatCompletion(
                    modelId: modelConfig.ModelId!,
                    apiKey: connectionConfig.ApiKey,
                    orgId: connectionConfig.OrgId // Optional
                    // serviceId: modelConfig.Connection
                );
                _logger.LogDebug("Added OpenAI Chat Completion: ModelId={ModelId}", modelConfig.ModelId);
                break;
             case EndpointType.TextCompletion:
                 // Add Text Completion if needed
                _logger.LogWarning("OpenAI Text Completion endpoint type specified but not implemented in factory. Use Chat Completion or Embedding.");
                 break;
            default:
                _logger.LogWarning("Unsupported OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                 modelConfig.EndpointType, modelConfig.ModelId);
                break;
        }
    }
} 