using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Configures OpenAI services for the KernelBuilder.
/// </summary>
public class OpenAIServiceConfigurator : IAIServiceConfigurator
{
    private readonly ILogger<OpenAIServiceConfigurator> _logger;

    public OpenAIServiceConfigurator(ILogger<OpenAIServiceConfigurator> logger)
    {
        _logger = logger;
    }

    public ServiceType HandledServiceType => ServiceType.OpenAI;

    public void ConfigureService(IKernelBuilder builder, ModelDefinition modelDefinition, ConnectionConfig connectionConfig)
    {
        // Use properties from modelDefinition
        if (string.IsNullOrWhiteSpace(connectionConfig.ApiKey) || string.IsNullOrWhiteSpace(modelDefinition.ModelId))
        {
            _logger.LogError("Cannot configure OpenAI service. ApiKey or ModelId is missing for connection '{ConnectionName}', model definition ID corresponding to '{ModelId}'.",
                             modelDefinition.Connection, modelDefinition.ModelId ?? "[Not Specified]");
            return; 
        }

        try
        {
            // Use modelDefinition.EndpointType and modelDefinition.ModelId
            switch (modelDefinition.EndpointType)
            {
                case EndpointType.ChatCompletion:
                    builder.AddOpenAIChatCompletion(
                        modelId: modelDefinition.ModelId!, 
                        apiKey: connectionConfig.ApiKey!, 
                        orgId: connectionConfig.OrgId 
                    );
                    _logger.LogDebug("Added OpenAI Chat Completion: ModelId={ModelId}", modelDefinition.ModelId);
                    break;
                case EndpointType.TextCompletion:
                    _logger.LogWarning("OpenAI Text Completion endpoint type specified for model '{ModelId}' but not added by configurator. Use Chat Completion or Embedding.", modelDefinition.ModelId);
                    break;
                default:
                    _logger.LogWarning("Unsupported OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                     modelDefinition.EndpointType, modelDefinition.ModelId);
                    break;
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to add OpenAI service for model '{ModelId}' using connection '{ConnectionName}'.", modelDefinition.ModelId, modelDefinition.Connection);
        }
    }
} 