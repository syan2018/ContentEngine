using System.Collections.Generic;

namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Represents the top-level configuration section for AI services.
/// </summary>
public class AIServiceOptions
{
    /// <summary>
    /// The name of the configuration section in appsettings.json.
    /// </summary>
    public const string SectionName = "AIServices";

    /// <summary>
    /// The identifier of the default AI configuration to use if none is specified.
    /// </summary>
    public string? DefaultServiceId { get; set; }

    /// <summary>
    /// A dictionary containing named AI service configurations.
    /// The key is the service identifier (e.g., "AzureGPT4o", "OpenAIGPT35").
    /// </summary>
    public Dictionary<string, AIConfiguration> Configurations { get; set; } = new();
} 