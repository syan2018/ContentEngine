using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Configures Azure OpenAI services for the KernelBuilder.
/// </summary>
public class AzureOpenAIServiceConfigurator(ILogger<AzureOpenAIServiceConfigurator> logger) : IAIServiceConfigurator
{
    public ServiceType HandledServiceType => ServiceType.AzureOpenAI;

    public void ConfigureService(IKernelBuilder builder, ModelDefinition modelDefinition, ConnectionConfig connectionConfig)
    {
        // Use properties from modelDefinition
        if (string.IsNullOrWhiteSpace(connectionConfig.Endpoint) || string.IsNullOrWhiteSpace(connectionConfig.ApiKey) || string.IsNullOrWhiteSpace(modelDefinition.ModelId))
        {
            logger.LogError("Cannot configure Azure OpenAI service. Endpoint, ApiKey, or ModelId is missing for connection '{ConnectionName}', model definition ID corresponding to '{ModelId}'.",
                             modelDefinition.Connection, modelDefinition.ModelId ?? "[Not Specified]");
            return; // Skip configuration if essential info is missing
        }

        try
        {
            // Use modelDefinition.EndpointType and modelDefinition.ModelId
            switch (modelDefinition.EndpointType)
            {
                case EndpointType.ChatCompletion:
                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: modelDefinition.ModelId!, // Null check done above
                        endpoint: connectionConfig.Endpoint!,
                        apiKey: connectionConfig.ApiKey!
                        // serviceId: modelDefinition.Connection // Optional: Use connection name as serviceId? Consider if needed.
                    );
                    logger.LogDebug("Added Azure OpenAI Chat Completion: Deployment={Deployment}, Endpoint={Endpoint}", modelDefinition.ModelId, connectionConfig.Endpoint);
                    break;
                case EndpointType.TextEmbedding: // Corrected embedding logic
#pragma warning disable SKEXP0010 // Suppress preview warning for AddAzureOpenAITextEmbeddingGeneration
                    builder.AddAzureOpenAITextEmbeddingGeneration(
                        deploymentName: modelDefinition.ModelId!,
                        endpoint: connectionConfig.Endpoint!,
                        apiKey: connectionConfig.ApiKey!
                    );
#pragma warning restore SKEXP0010
                     logger.LogDebug("Added Azure OpenAI Text Embedding: Deployment={Deployment}, Endpoint={Endpoint}", modelDefinition.ModelId, connectionConfig.Endpoint);
                     break;
                case EndpointType.TextCompletion:
                    logger.LogWarning("Azure OpenAI Text Completion endpoint type specified for model '{ModelId}' but not added by configurator. Use Chat Completion or Embedding.", modelDefinition.ModelId);
                    break;
                default:
                    logger.LogWarning("Unsupported Azure OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                     modelDefinition.EndpointType, modelDefinition.ModelId);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add Azure OpenAI service for model '{ModelId}' using connection '{ConnectionName}'.", modelDefinition.ModelId, modelDefinition.Connection);
            // Optionally re-throw or handle specific exceptions if needed
        }
    }
} 