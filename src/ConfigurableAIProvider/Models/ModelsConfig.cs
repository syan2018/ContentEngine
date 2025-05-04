using ConfigurableAIProvider.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfigurableAIProvider.Models;

/// <summary>
/// Represents the entire collection of model definitions loaded from models.yaml.
/// </summary>
public class ModelsConfig
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
           .WithNamingConvention(CamelCaseNamingConvention.Instance)
           .IgnoreUnmatchedProperties()
           .Build();

    /// <summary>
    /// Dictionary mapping unique model definition IDs (e.g., "azure-gpt4o-mini-std")
    /// to their corresponding ModelConfig objects.
    /// </summary>
    [YamlMember(Alias = "models")]
    public Dictionary<string, ModelConfig>? Models { get; set; }

    public static ModelsConfig FromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Models configuration file not found: {filePath}");
        }
        return FromYaml(File.ReadAllText(filePath));
    }

    public static ModelsConfig FromYaml(string yaml)
    {
        try
        {
            return Deserializer.Deserialize<ModelsConfig>(yaml);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidDataException($"Error parsing models YAML: {ex.Message}", ex);
        }
    }
} 