using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using System.Collections.Generic;

namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Represents the configuration for a specific AI Agent, loaded from YAML.
/// </summary>
public class AgentConfig
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties() // Be more tolerant
        .Build();

    [YamlMember(Alias = "models")]
    public Dictionary<string, ModelConfig>? Models { get; set; }

    [YamlMember(Alias = "plugins")]
    public List<string>? Plugins { get; set; }

    [YamlMember(Alias = "logLevel")]
    public LogLevel? LogLevel { get; set; }

    // --- Nested Model Configuration ---
    public class ModelConfig
    {
        [YamlMember(Alias = "connection")]
        public string? Connection { get; set; } // Name of the connection in connections.yaml

        [YamlMember(Alias = "modelId")]
        public string? ModelId { get; set; }

        [YamlMember(Alias = "endpointType")]
        public EndpointType? EndpointType { get; set; }
    }

    // --- Static Loading Methods ---

    /// <summary>
    /// Loads and merges agent configuration from a directory.
    /// Reads agent.yaml and agent.{environment}.yaml.
    /// </summary>
    /// <param name="directory">The agent's configuration directory.</param>
    /// <param name="environment">The environment (e.g., "dev", "prod").</param>
    /// <returns>The merged AgentConfig.</returns>
    /// <exception cref="FileNotFoundException">If agent.yaml is not found.</exception>
    /// <exception cref="DirectoryNotFoundException">If the directory does not exist.</exception>
    public static AgentConfig FromDirectory(string directory, string environment = "dev")
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Agent configuration directory not found: {directory}");
        }

        var defaultConfigFile = Path.Combine(directory, "agent.yaml");
        if (!File.Exists(defaultConfigFile))
        {
            throw new FileNotFoundException($"Base agent configuration file 'agent.yaml' not found in {directory}");
        }

        var defaultConfig = FromFile(defaultConfigFile);

        var envConfigFile = Path.Combine(directory, $"agent.{environment}.yaml");
        var envConfig = File.Exists(envConfigFile) ? FromFile(envConfigFile) : null;

        // Basic merge: Environment config overrides default config properties.
        // For dictionaries like Models, we might want a deeper merge, but simple override is often sufficient.
        // For lists like Plugins, environment replaces default.
        if (envConfig != null)
        {
            if (envConfig.LogLevel.HasValue)
                defaultConfig.LogLevel = envConfig.LogLevel;

            if (envConfig.Models != null) // Overwrite models if specified in env file
                defaultConfig.Models = envConfig.Models;

            if (envConfig.Plugins != null) // Overwrite plugins if specified in env file
                defaultConfig.Plugins = envConfig.Plugins;
            
            // Add more specific merging logic here if needed (e.g., deep merge dictionaries)
        }


        return defaultConfig;
    }

    public static AgentConfig FromFile(string filePath)
    {
        return FromYaml(File.ReadAllText(filePath));
    }

    public static AgentConfig FromYaml(string yaml)
    {
        return Deserializer.Deserialize<AgentConfig>(yaml);
    }
}

// Enums might already exist elsewhere, ensure consistency or move them to a shared location.
// If they exist in the semantic-kernel-from-config reference's Models, reuse those.
// Otherwise, define them here or in a dedicated Enums file.
// Let's assume they might need to be defined here for now.

public enum EndpointType
{
    TextCompletion = 0,
    ChatCompletion = 1,
    TextEmbedding = 2 // Added for embedding models
    // Add other types like ImageGeneration if needed
} 