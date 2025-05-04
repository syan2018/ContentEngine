using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfigurableAIProvider.Models;

/// <summary>
/// Represents the configuration for a specific AI Agent, loaded from YAML.
/// Specifies which pre-defined models and plugins the agent uses.
/// </summary>
public class AgentConfig
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties() // Be more tolerant
        .Build();

    /// <summary>
    /// Maps a logical name used within the agent (e.g., "primaryChat") 
    /// to a model definition ID specified in the central models.yaml file (e.g., "azure-gpt4o-mini-std").
    /// </summary>
    [YamlMember(Alias = "models")]
    public Dictionary<string, string>? Models { get; set; }

    /// <summary>
    /// List of plugin configurations to load for this agent.
    /// Each string should be a path relative to the AgentsBasePath,
    /// pointing to a directory containing a plugin.yaml file.
    /// </summary>
    [YamlMember(Alias = "plugins")]
    public List<string>? Plugins { get; set; }

    [YamlMember(Alias = "logLevel")]
    public LogLevel? LogLevel { get; set; }

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

        if (envConfig != null)
        {
            if (envConfig.LogLevel.HasValue)
                defaultConfig.LogLevel = envConfig.LogLevel;

            // Simple dictionary merge/overwrite for models
            if (envConfig.Models != null) 
            {
                if (defaultConfig.Models == null) defaultConfig.Models = new Dictionary<string, string>();
                foreach (var kvp in envConfig.Models)
                {
                    defaultConfig.Models[kvp.Key] = kvp.Value;
                }
            }

            if (envConfig.Plugins != null) // Overwrite plugins list
                defaultConfig.Plugins = envConfig.Plugins;
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