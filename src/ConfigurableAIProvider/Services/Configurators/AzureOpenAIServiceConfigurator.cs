using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Configures Azure OpenAI services for the KernelBuilder.
/// </summary>
public class AzureOpenAIServiceConfigurator : IAIServiceConfigurator
{
    private readonly ILogger<AzureOpenAIServiceConfigurator> _logger;

    public AzureOpenAIServiceConfigurator(ILogger<AzureOpenAIServiceConfigurator> logger)
    {
        _logger = logger;
    }

    public ServiceType HandledServiceType => ServiceType.AzureOpenAI;

    public void ConfigureService(IKernelBuilder builder, AgentConfig.ModelConfig modelConfig, ConnectionConfig connectionConfig)
    {
        // Ensure required fields are present
        if (string.IsNullOrWhiteSpace(connectionConfig.Endpoint) || string.IsNullOrWhiteSpace(connectionConfig.ApiKey) || string.IsNullOrWhiteSpace(modelConfig.ModelId))
        {
            _logger.LogError("Cannot configure Azure OpenAI service. Endpoint, ApiKey, or ModelId is missing for connection '{ConnectionName}', model '{ModelId}'.",
                             modelConfig.Connection, modelConfig.ModelId ?? "[Not Specified]");
            return; // Skip configuration if essential info is missing
        }

        try
        {
            switch (modelConfig.EndpointType)
            {
                case EndpointType.ChatCompletion:
                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: modelConfig.ModelId!, // Null check done above
                        endpoint: connectionConfig.Endpoint!,
                        apiKey: connectionConfig.ApiKey!
                        // serviceId: modelConfig.Connection // Optional: Use connection name as serviceId? Consider if needed.
                    );
                    _logger.LogDebug("Added Azure OpenAI Chat Completion: Deployment={Deployment}, Endpoint={Endpoint}", modelConfig.ModelId, connectionConfig.Endpoint);
                    break;
                case EndpointType.TextCompletion:
                    _logger.LogWarning("Azure OpenAI Text Completion endpoint type specified for model '{ModelId}' but not added by configurator. Use Chat Completion or Embedding.", modelConfig.ModelId);
                    break;
                default:
                    _logger.LogWarning("Unsupported Azure OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                     modelConfig.EndpointType, modelConfig.ModelId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add Azure OpenAI service for model '{ModelId}' using connection '{ConnectionName}'.", modelConfig.ModelId, modelConfig.Connection);
            // Optionally re-throw or handle specific exceptions if needed
        }
    }
} 