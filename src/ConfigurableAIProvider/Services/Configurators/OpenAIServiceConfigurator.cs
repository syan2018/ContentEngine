using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Net.Http;
using ConfigurableAIProvider.Models;

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Configures OpenAI services for the KernelBuilder.
/// </summary>
public class OpenAIServiceConfigurator(ILogger<OpenAIServiceConfigurator> logger) : IAIServiceConfigurator
{
    public ServiceType HandledServiceType => ServiceType.OpenAI;

    public void ConfigureService(IKernelBuilder builder, ModelConfig modelConfig, ConnectionConfig connectionConfig)
    {
        // Use properties from modelConfig
        if (string.IsNullOrWhiteSpace(connectionConfig.ApiKey) || string.IsNullOrWhiteSpace(modelConfig.ModelId))
        {
            logger.LogError("Cannot configure OpenAI service. ApiKey or ModelId is missing for connection '{ConnectionName}', model definition ID corresponding to '{ModelId}'.",
                             modelConfig.Connection, modelConfig.ModelId ?? "[Not Specified]");
            return; 
        }

        // --- HttpClient Configuration for BaseUrl Override ---
        HttpClient? httpClient = null;
        if (!string.IsNullOrWhiteSpace(connectionConfig.BaseUrl))
        {
            try
            {
                // IMPORTANT: In high-load scenarios or ASP.NET Core, consider using IHttpClientFactory 
                // injected into this configurator for better HttpClient lifetime management.
                // For simplicity here, we create it directly.
                httpClient = new HttpClient { BaseAddress = new Uri(connectionConfig.BaseUrl) };
                logger.LogInformation("Configuring OpenAI client to use custom BaseUrl: {BaseUrl}", connectionConfig.BaseUrl);
            }
            catch (UriFormatException ex)
            {
                logger.LogError(ex, "Invalid BaseUrl format specified for connection '{ConnectionName}': {BaseUrl}. OpenAI service will use the default endpoint.", 
                                 modelConfig.Connection, connectionConfig.BaseUrl);
                // httpClient remains null, so SK uses default behavior
            }
        }
        // -----------------------------------------------------

        try
        {
            // Use modelConfig.EndpointType and modelConfig.ModelId
            switch (modelConfig.EndpointType)
            {
                case EndpointType.ChatCompletion:
                    builder.AddOpenAIChatCompletion(
                        modelId: modelConfig.ModelId!, 
                        apiKey: connectionConfig.ApiKey!, 
                        orgId: connectionConfig.OrgId, 
                        httpClient: httpClient // Pass the configured HttpClient (can be null)
                    );
                    logger.LogDebug("Added OpenAI Chat Completion: ModelId={ModelId}{CustomEndpoint}", 
                                     modelConfig.ModelId, 
                                     httpClient != null ? $" (Endpoint: {httpClient.BaseAddress})" : " (Default Endpoint)");
                    break;
                case EndpointType.TextCompletion:
                    // If needed, implement TextCompletion similarly, passing the httpClient
                     logger.LogWarning("OpenAI Text Completion endpoint type specified for model '{ModelId}' but not added by configurator. Use Chat Completion or Embedding.", modelConfig.ModelId);
                    break;
                default:
                    logger.LogWarning("Unsupported OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                     modelConfig.EndpointType, modelConfig.ModelId);
                    break;
            }
        }
        catch (Exception ex)
        {
             // Log general errors during SK service addition
             logger.LogError(ex, "Failed to add OpenAI service for model '{ModelId}' using connection '{ConnectionName}'.", modelConfig.ModelId, modelConfig.Connection);
        }
    }
} 