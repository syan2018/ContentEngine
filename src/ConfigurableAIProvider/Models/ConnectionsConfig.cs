using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace ConfigurableAIProvider.Models;

/// <summary>
/// Represents the overall connections configuration loaded from connections.yaml.
/// </summary>
public class ConnectionsConfig
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
           .WithNamingConvention(CamelCaseNamingConvention.Instance)
           .IgnoreUnmatchedProperties()
           .Build();

    [YamlMember(Alias = "connections")]
    public Dictionary<string, ConnectionConfig>? Connections { get; set; }

    public static ConnectionsConfig FromFile(string filePath)
    {
        // Check if file exists before reading
        if (!File.Exists(filePath))
        {
             throw new FileNotFoundException($"Connections configuration file not found: {filePath}");
        }
        return FromYaml(File.ReadAllText(filePath));
    }

    public static ConnectionsConfig FromYaml(string yaml)
    {
        try
        {
            return Deserializer.Deserialize<ConnectionsConfig>(yaml);
        }
        catch (YamlDotNet.Core.YamlException ex) // Catch specific YAML parsing errors
        {
            // Consider logging the error details here
            throw new InvalidDataException($"Error parsing connections YAML: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Represents configuration for a single AI service connection.
/// </summary>
public class ConnectionConfig
{
    [YamlMember(Alias = "serviceType")]
    public ServiceType ServiceType { get; set; } // Changed to non-nullable, should always be present

    /// <summary>
    /// The API key for the AI service.
    /// </summary>
    [YamlMember(Alias = "apiKey")]
    public string? ApiKey { get; set; } // Nullable, might be loaded from env var

    /// <summary>
    /// The base URL for the AI service.
    /// such as: https://api.openai.com/v1/
    /// https://api.openai.com/v1/chat/completions --> {baseUrl}/{endpoint}
    /// </summary>
    [YamlMember(Alias = "baseUrl")]
    public string? BaseUrl { get; set; } // Added: For OpenAI-compatible endpoints

    /// <summary>
    /// The endpoint for the AI service.
    /// </summary>
    [YamlMember(Alias = "endpoint")]
    public string? Endpoint { get; set; } // Primarily used by Azure, keep for potential other future types

    /// <summary>
    /// The organization ID for the OpenAI service.
    /// for OpenAI
    /// </summary>
    [YamlMember(Alias = "orgId")]
    public string? OrgId { get; set; } // Nullable, specific to OpenAI
    
    /// <summary>
    /// The service ID for the AI service.
    /// for Ollama
    /// </summary>
    [YamlMember(Alias = "serviceId")]
    public string? ServiceId { get; set; } 
    
    // TODO: Add other potential common properties if needed (e.g., API Version for Azure)
}

/// <summary>
/// Defines the types of AI services supported.
/// </summary>
public enum ServiceType
{
    OpenAI = 0,
    AzureOpenAI = 1,
    Ollama = 2 // Added Ollama service type
    // Add other service types like HuggingFace, local models, etc.
} 