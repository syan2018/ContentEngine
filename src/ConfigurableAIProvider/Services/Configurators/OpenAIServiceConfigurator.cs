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

    public void ConfigureService(IKernelBuilder builder, AgentConfig.ModelConfig modelConfig, ConnectionConfig connectionConfig)
    {
         // Ensure required fields are present
        if (string.IsNullOrWhiteSpace(connectionConfig.ApiKey) || string.IsNullOrWhiteSpace(modelConfig.ModelId))
        {
            _logger.LogError("Cannot configure OpenAI service. ApiKey or ModelId is missing for connection '{ConnectionName}', model '{ModelId}'.",
                             modelConfig.Connection, modelConfig.ModelId ?? "[Not Specified]");
            return; // Skip configuration if essential info is missing
        }

        try
        {
            switch (modelConfig.EndpointType)
            {
                case EndpointType.ChatCompletion:
                    builder.AddOpenAIChatCompletion(
                        modelId: modelConfig.ModelId!, // Null check done above
                        apiKey: connectionConfig.ApiKey!,
                        orgId: connectionConfig.OrgId // Optional, can be null
                        // serviceId: modelConfig.Connection
                    );
                    _logger.LogDebug("Added OpenAI Chat Completion: ModelId={ModelId}", modelConfig.ModelId);
                    break;
                case EndpointType.TextCompletion:
                    _logger.LogWarning("OpenAI Text Completion endpoint type specified for model '{ModelId}' but not added by configurator. Use Chat Completion or Embedding.", modelConfig.ModelId);
                    break;
                default:
                    _logger.LogWarning("Unsupported OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                     modelConfig.EndpointType, modelConfig.ModelId);
                    break;
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to add OpenAI service for model '{ModelId}' using connection '{ConnectionName}'.", modelConfig.ModelId, modelConfig.Connection);
            // Optionally re-throw or handle specific exceptions if needed
        }
    }
} 