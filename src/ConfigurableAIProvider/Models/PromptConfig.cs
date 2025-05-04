using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.SemanticKernel; // Added
using System.Collections.Generic; // Added
using System.IO; // Added
using System; // Added

namespace ConfigurableAIProvider.Models; // Adjusted namespace

public class PromptConfig
{
    // Deserializer setup remains the same
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties() // Keep IgnoreUnmatchedProperties
            .Build();

    // Keep fields relevant to modern SK PromptTemplateConfig or mapping
    // schema, type, description, prompt, templateFormat, inputVariables, executionSettings
    
    [YamlMember(Alias = "schema")] // Keep for potential versioning info
    public int Schema { get; set; } = 1; 
    
    [YamlMember(Alias = "type")] // Informational, less critical for SK v1
    public string? Type { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty; // Keep PascalCase

    [YamlMember(Alias = "prompt")]
    public string Prompt { get; set; } = string.Empty; // Keep PascalCase
    
    [YamlMember(Alias = "templateFormat")] 
    public string TemplateFormat { get; set; } = "semantic-kernel"; // Keep PascalCase

    // Rename internal class to match reference structure conceptually
    [YamlMember(Alias = "inputVariables")] // Keep alias camelCase
    public InputConfig? Input { get; set; }
    
    // Update dictionary value type to the renamed inner class
    [YamlMember(Alias = "executionSettings")] 
    public Dictionary<string, ExecutionSettingDetails>? ExecutionSettings { get; set; }

    // Remove fields less relevant/directly mappable in SK v1 from reference:
    // Name (derived from file), Model (in execution settings), DefaultServices (in exec settings), IsSensitive
    // [YamlMember(Alias = "name")] public string? Name { get; set; }
    // [YamlMember(Alias = "model")] public string? Model { get; set; }
    // [YamlMember(Alias = "defaultServices")] public List<string>? DefaultServices { get; set; }
    // [YamlMember(Alias = "isSensitive")] public bool? IsSensitive { get; set; }

    // Inner class based on reference InputConfig/InputParameter
    public class InputConfig 
    {
        [YamlMember(Alias = "parameters")]
        public List<InputParameter>? Parameters { get; set; }
        
        public class InputParameter
        {
            [YamlMember(Alias = "name")]
            public string Name { get; set; } = string.Empty; // Keep PascalCase

            [YamlMember(Alias = "description")]
            public string Description { get; set; } = string.Empty; // Keep PascalCase

            [YamlMember(Alias = "defaultValue")]
            public string? DefaultValue { get; set; } // Keep PascalCase
            
            // Add IsRequired based on SK standard InputVariable
            [YamlMember(Alias = "isRequired")] 
            public bool IsRequired { get; set; } = false; // Default to false if not specified
        }
    }

    // Rename inner class from CompletionConfig to ExecutionSettingDetails
    /// <summary>
    /// Defines the specific execution parameters for a given AI service.
    /// </summary>
    public class ExecutionSettingDetails // Renamed from CompletionConfig
    {
        [YamlMember(Alias = "temperature")]
        public double? Temperature { get; set; }

        [YamlMember(Alias = "topP")]
        public double? TopP { get; set; }

        [YamlMember(Alias = "presencePenalty")]
        public double? PresencePenalty { get; set; }

        [YamlMember(Alias = "frequencyPenalty")]
        public double? FrequencyPenalty { get; set; }

        [YamlMember(Alias = "maxTokens")]
        public int? MaxTokens { get; set; }

        [YamlMember(Alias = "stopSequences")]
        public List<string>? StopSequences { get; set; }
        
        // Keep the mapping function within the renamed class
        public PromptExecutionSettings ToPromptExecutionSettings()
        {
            var settings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>() 
            };
            
            void AddIfNotNull(string key, object? value) {
                if (value != null) settings.ExtensionData[key] = value;
            }

            AddIfNotNull("temperature", Temperature);
            AddIfNotNull("top_p", TopP);
            AddIfNotNull("presence_penalty", PresencePenalty);
            AddIfNotNull("frequency_penalty", FrequencyPenalty);
            AddIfNotNull("max_tokens", MaxTokens);
            AddIfNotNull("stop_sequences", StopSequences);

            return settings;
        }
    }

    // Static methods remain the same
    public static PromptConfig FromYaml(string yaml)
    {
        return FromYamlInternal(yaml, "[YAML Content]");
    }

    public static PromptConfig FromFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("Prompt configuration file not found.", filePath);
        }
        var yamlContent = System.IO.File.ReadAllText(filePath);
        return FromYamlInternal(yamlContent, filePath);
    }
    
    private static PromptConfig FromYamlInternal(string yaml, string sourceIdentifier)
    {
        PromptConfig config;
        try
        {
            config = Deserializer.Deserialize<PromptConfig>(yaml);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidDataException($"Failed to parse YAML in prompt configuration source: {sourceIdentifier}. Error: {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(config.Prompt))
        {
            throw new InvalidDataException($"The 'prompt' field is missing or empty in configuration source: {sourceIdentifier}");
        }
        // Add other validation as needed

        return config;
    }
}
