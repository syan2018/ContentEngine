using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Generic; // Added
using System.IO; // Added
using System; // Added

namespace ConfigurableAIProvider.Models; // Adjusted namespace

public class PluginConfig
{
    // Deserializer setup remains the same
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties() // Keep IgnoreUnmatchedProperties
            .Build();

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "functions")] // Use lowercase 'f' consistent with reference YAML alias
    public FunctionsNode? Functions { get; set; }

    // Renamed inner class to avoid conflict with System.Functions
    public class FunctionsNode 
    {
        [YamlMember(Alias = "semanticFunctions")]
        public List<SemanticFunctionConfig>? SemanticFunctions { get; set; } // Use List<> instead of array

        [YamlMember(Alias = "cSharpFunctions")]
        public List<CSharpFunctionConfig>? CSharpFunctions { get; set; } // Use List<> instead of array

        // Renamed inner class
        public class SemanticFunctionConfig 
        {
            [YamlMember(Alias = "path")]
            public string? Path { get; set; } // Use PascalCase for property

            // ExposeToPlanner removed for simplicity, aligning with reference base
            // [YamlMember(Alias = "exposeToPlanner")]
            // public bool ExposeToPlanner { get; set; } = true;
        }

        // Renamed inner class
        public class CSharpFunctionConfig 
        {
            [YamlMember(Alias = "dll")]
            public string? Dll { get; set; } // Use PascalCase for property

            [YamlMember(Alias = "className")]
            public string? ClassName { get; set; } // Use PascalCase for property

            // ExposeToPlanner removed for simplicity
            // [YamlMember(Alias = "exposeToPlanner")]
            // public bool? ExposeToPlanner { get; set; }
        }
    }

    // Static methods remain mostly the same, ensure validation is present
    public static PluginConfig FromFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("Plugin configuration file not found.", filePath);
        }
        var yamlContent = System.IO.File.ReadAllText(filePath);
        return FromYamlInternal(yamlContent, filePath);
    }

    public static PluginConfig FromYaml(string yaml)
    {
        return FromYamlInternal(yaml, "[YAML Content]");
    }
    
    private static PluginConfig FromYamlInternal(string yaml, string sourceIdentifier)
    {
        PluginConfig config;
        try
        {
            config = Deserializer.Deserialize<PluginConfig>(yaml);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidDataException($"Failed to parse YAML in plugin configuration source: {sourceIdentifier}. Error: {ex.Message}", ex);
        }

        // Basic validation (can be expanded)
        if (config.Functions == null || 
            (config.Functions.SemanticFunctions == null && config.Functions.CSharpFunctions == null) ||
            (config.Functions.SemanticFunctions?.Count == 0 && config.Functions.CSharpFunctions?.Count == 0))
        {
             // Log a warning if a plugin defines no functions, as it might be unintentional.
             // Consider using proper logging if available instead of Console.WriteLine
             Console.WriteLine($"Warning: Plugin configuration source '{sourceIdentifier}' defines a plugin '{config.Name ?? "(unknown)"}' with no functions.");
        }
        
        return config;
    }
}
