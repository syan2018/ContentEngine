using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama; // Requires Microsoft.SemanticKernel.Connectors.Ollama NuGet package
using System;
using System.Net.Http;
using ConfigurableAIProvider.Models;
using OllamaSharp; // Required for HttpClient if used explicitly

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Configures Ollama services for the KernelBuilder based on provided configuration.
/// </summary>
public class OllamaServiceConfigurator(ILogger<OllamaServiceConfigurator> logger) : IAIServiceConfigurator
{
    /// <summary>
    /// Gets the service type handled by this configurator.
    /// </summary>
    public ServiceType HandledServiceType => ServiceType.Ollama;

    /// <summary>
    /// Configures the Ollama service for the kernel builder.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="modelConfig">The model definition specifying ModelId and EndpointType.</param>
    /// <param name="connectionConfig">The connection configuration containing the BaseUrl.</param>
    public void ConfigureService(IKernelBuilder builder, ModelConfig modelConfig, ConnectionConfig connectionConfig)
    {
        // Validate essential configuration
        if (string.IsNullOrWhiteSpace(connectionConfig.BaseUrl) || string.IsNullOrWhiteSpace(modelConfig.ModelId))
        {
            logger.LogError("Cannot configure Ollama service. BaseUrl in connection '{ConnectionName}' or ModelId in model definition is missing. Model ID specified was '{ModelId}'. BaseUrl specified was '{BaseUrl}'.",
                             modelConfig.Connection, modelConfig.ModelId ?? "[Not Specified]", connectionConfig.BaseUrl ?? "[Not Specified]");
            return;
        }

        try
        {
            OllamaApiClient ollamaClient = new OllamaApiClient(connectionConfig.BaseUrl); // Can use OllamaClient directly if needed
            
            ollamaClient.SelectedModel = modelConfig.ModelId; // Set the selected model ID

            // Add the appropriate Ollama service based on the endpoint type
            switch (modelConfig.EndpointType)
            {
                case EndpointType.ChatCompletion:
#pragma warning disable SKEXP0070 // Suppress preview warning for AddAzureOpenAITextEmbeddingGeneration
                    builder.AddOllamaChatCompletion(
                        ollamaClient: ollamaClient, // Pass OllamaClient directly
                        serviceId: connectionConfig.ServiceId // Optional: Use connection name as serviceId? Consider if needed.
                    );
                    logger.LogInformation("Added Ollama Chat Completion: ModelId={ModelId}, BaseUrl={BaseUrl}",
                                         modelConfig.ModelId, connectionConfig.BaseUrl);
                    break;
#pragma warning restore SKEXP0010
                case EndpointType.TextEmbedding:
                    builder.AddOllamaTextEmbeddingGeneration(
                        ollamaClient: ollamaClient, // Pass OllamaClient directly
                        serviceId: connectionConfig.ServiceId // Optional: Use connection name as serviceId? Consider if needed.
                    );
                     logger.LogInformation("Added Ollama Text Embedding Generation: ModelId={ModelId}, BaseUrl={BaseUrl}",
                                         modelConfig.ModelId, connectionConfig.BaseUrl);
                    break;

                // Add cases for other Ollama endpoint types if supported by SK connector
                // case EndpointType.TextGeneration: // Example if SK adds text generation support
                //     builder.AddOllamaTextGeneration(...);
                //     _logger.LogInformation("Added Ollama Text Generation...");
                //     break;

                default:
                    logger.LogWarning("Unsupported Ollama endpoint type '{EndpointType}' specified for model '{ModelId}'. Only ChatCompletion and TextEmbeddingGeneration are currently configured.",
                                     modelConfig.EndpointType, modelConfig.ModelId);
                    break;
            }
        }
        catch (Exception ex) // Catch potential exceptions during SK service registration
        {
            logger.LogError(ex, "Failed to configure Ollama service for model '{ModelId}' using connection '{ConnectionName}' (BaseUrl: {BaseUrl}).",
                             modelConfig.ModelId, modelConfig.Connection, connectionConfig.BaseUrl);
        }
        // finally // If explicit HttpClient was created, ensure it's disposed
        // {
        //     httpClient?.Dispose();
        // }
    }
} 