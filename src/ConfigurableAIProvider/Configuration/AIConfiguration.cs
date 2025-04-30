namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Represents the configuration for a single AI service endpoint.
/// </summary>
public class AIConfiguration
{
    /// <summary>
    /// Type of the AI service (e.g., "AzureOpenAI", "OpenAI"). Determines how to connect.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Model ID (for OpenAI) or Deployment Name (for Azure OpenAI).
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Endpoint URL for Azure OpenAI services.
    /// </summary>
    public string? Endpoint { get; set; } // Azure specific

    /// <summary>
    /// API Key for the service. IMPORTANT: Use secure storage like User Secrets or environment variables.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Organization ID for OpenAI services (optional).
    /// </summary>
    public string? OrgId { get; set; } // OpenAI specific
} 