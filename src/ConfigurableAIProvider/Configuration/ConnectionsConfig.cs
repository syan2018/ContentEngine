using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic;
using System.IO; // Required for File.ReadAllText

namespace ConfigurableAIProvider.Configuration;

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

    [YamlMember(Alias = "endpoint")]
    public string? Endpoint { get; set; } // Primarily used by Azure, keep for potential other future types

    [YamlMember(Alias = "baseUrl")]
    public string? BaseUrl { get; set; } // Added: For OpenAI-compatible endpoints

    [YamlMember(Alias = "apiKey")]
    public string? ApiKey { get; set; } // Nullable, might be loaded from env var

    [YamlMember(Alias = "orgId")]
    public string? OrgId { get; set; } // Nullable, specific to OpenAI
    
    // TODO: Add other potential common properties if needed (e.g., API Version for Azure)
}

/// <summary>
/// Defines the types of AI services supported.
/// </summary>
public enum ServiceType
{
    OpenAI = 0,
    AzureOpenAI = 1
    // Add other service types like HuggingFace, local models, etc.
} 