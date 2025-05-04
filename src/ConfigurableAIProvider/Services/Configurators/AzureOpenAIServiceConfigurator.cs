using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using ConfigurableAIProvider.Models;

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Configures Azure OpenAI services for the KernelBuilder.
/// </summary>
public class AzureOpenAIServiceConfigurator(ILogger<AzureOpenAIServiceConfigurator> logger) : IAIServiceConfigurator
{
    public ServiceType HandledServiceType => ServiceType.AzureOpenAI;

    public void ConfigureService(IKernelBuilder builder, ModelConfig modelConfig, ConnectionConfig connectionConfig)
    {
        // Use properties from modelConfig
        if (string.IsNullOrWhiteSpace(connectionConfig.Endpoint) || string.IsNullOrWhiteSpace(connectionConfig.ApiKey) || string.IsNullOrWhiteSpace(modelConfig.ModelId))
        {
            logger.LogError("Cannot configure Azure OpenAI service. Endpoint, ApiKey, or ModelId is missing for connection '{ConnectionName}', model definition ID corresponding to '{ModelId}'.",
                             modelConfig.Connection, modelConfig.ModelId ?? "[Not Specified]");
            return; // Skip configuration if essential info is missing
        }

        try
        {
            // Use modelConfig.EndpointType and modelConfig.ModelId
            switch (modelConfig.EndpointType)
            {
                case EndpointType.ChatCompletion:
                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: modelConfig.ModelId!, // Null check done above
                        endpoint: connectionConfig.Endpoint!,
                        apiKey: connectionConfig.ApiKey!
                        // serviceId: modelConfig.Connection // Optional: Use connection name as serviceId? Consider if needed.
                    );
                    logger.LogDebug("Added Azure OpenAI Chat Completion: Deployment={Deployment}, Endpoint={Endpoint}", modelConfig.ModelId, connectionConfig.Endpoint);
                    break;
                case EndpointType.TextEmbedding: // Corrected embedding logic
#pragma warning disable SKEXP0010 // Suppress preview warning for AddAzureOpenAITextEmbeddingGeneration
                    builder.AddAzureOpenAITextEmbeddingGeneration(
                        deploymentName: modelConfig.ModelId!,
                        endpoint: connectionConfig.Endpoint!,
                        apiKey: connectionConfig.ApiKey!
                    );
#pragma warning restore SKEXP0010
                     logger.LogDebug("Added Azure OpenAI Text Embedding: Deployment={Deployment}, Endpoint={Endpoint}", modelConfig.ModelId, connectionConfig.Endpoint);
                     break;
                case EndpointType.TextCompletion:
                    logger.LogWarning("Azure OpenAI Text Completion endpoint type specified for model '{ModelId}' but not added by configurator. Use Chat Completion or Embedding.", modelConfig.ModelId);
                    break;
                default:
                    logger.LogWarning("Unsupported Azure OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                     modelConfig.EndpointType, modelConfig.ModelId);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add Azure OpenAI service for model '{ModelId}' using connection '{ConnectionName}'.", modelConfig.ModelId, modelConfig.Connection);
            // Optionally re-throw or handle specific exceptions if needed
        }
    }
} 