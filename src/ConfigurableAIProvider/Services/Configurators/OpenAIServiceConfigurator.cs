using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Net.Http;

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Configures OpenAI services for the KernelBuilder.
/// </summary>
public class OpenAIServiceConfigurator(ILogger<OpenAIServiceConfigurator> logger) : IAIServiceConfigurator
{
    public ServiceType HandledServiceType => ServiceType.OpenAI;

    public void ConfigureService(IKernelBuilder builder, ModelDefinition modelDefinition, ConnectionConfig connectionConfig)
    {
        // Use properties from modelDefinition
        if (string.IsNullOrWhiteSpace(connectionConfig.ApiKey) || string.IsNullOrWhiteSpace(modelDefinition.ModelId))
        {
            logger.LogError("Cannot configure OpenAI service. ApiKey or ModelId is missing for connection '{ConnectionName}', model definition ID corresponding to '{ModelId}'.",
                             modelDefinition.Connection, modelDefinition.ModelId ?? "[Not Specified]");
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
                                 modelDefinition.Connection, connectionConfig.BaseUrl);
                // httpClient remains null, so SK uses default behavior
            }
        }
        // -----------------------------------------------------

        try
        {
            // Use modelDefinition.EndpointType and modelDefinition.ModelId
            switch (modelDefinition.EndpointType)
            {
                case EndpointType.ChatCompletion:
                    builder.AddOpenAIChatCompletion(
                        modelId: modelDefinition.ModelId!, 
                        apiKey: connectionConfig.ApiKey!, 
                        orgId: connectionConfig.OrgId, 
                        httpClient: httpClient // Pass the configured HttpClient (can be null)
                    );
                    logger.LogDebug("Added OpenAI Chat Completion: ModelId={ModelId}{CustomEndpoint}", 
                                     modelDefinition.ModelId, 
                                     httpClient != null ? $" (Endpoint: {httpClient.BaseAddress})" : " (Default Endpoint)");
                    break;
                case EndpointType.TextCompletion:
                    // If needed, implement TextCompletion similarly, passing the httpClient
                     logger.LogWarning("OpenAI Text Completion endpoint type specified for model '{ModelId}' but not added by configurator. Use Chat Completion or Embedding.", modelDefinition.ModelId);
                    break;
                default:
                    logger.LogWarning("Unsupported OpenAI endpoint type '{EndpointType}' for model '{ModelId}'.",
                                     modelDefinition.EndpointType, modelDefinition.ModelId);
                    break;
            }
        }
        catch (Exception ex)
        {
             // Log general errors during SK service addition
             logger.LogError(ex, "Failed to add OpenAI service for model '{ModelId}' using connection '{ConnectionName}'.", modelDefinition.ModelId, modelDefinition.Connection);
        }
        finally // Ensure HttpClient is disposed if created directly
        {
             httpClient?.Dispose();
        }
    }
} 