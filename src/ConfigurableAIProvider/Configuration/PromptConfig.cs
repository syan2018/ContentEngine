using System.Collections.Generic;
using System.IO;
using System.Linq; // Added for LINQ operations
using Microsoft.SemanticKernel; // Added for PromptExecutionSettings
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Represents the configuration for a Semantic Kernel semantic function,
/// typically loaded from a .prompt.yaml file.
/// </summary>
public class PromptConfig
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// The schema version of the configuration file.
    /// </summary>
    [YamlMember(Alias = "schema")]
    public int Schema { get; set; } = 1;

    /// <summary>
    /// The type of the function (e.g., "completion", "embedding").
    /// Currently informational, validation might be added later.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "completion"; // Default to completion

    /// <summary>
    /// Description of the function.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The prompt template string.
    /// </summary>
    [YamlMember(Alias = "prompt")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// The format of the prompt template (e.g., "semantic-kernel", "handlebars").
    /// </summary>
    [YamlMember(Alias = "template_format")] // Use snake_case to match reference
    public string TemplateFormat { get; set; } = "semantic-kernel"; // Default to SK format

    /// <summary>
    /// List of input variables for the function.
    /// </summary>
    [YamlMember(Alias = "input_variables")]
    public List<InputVariableConfig>? InputVariables { get; set; }

    /// <summary>
    /// Execution settings for the function (e.g., temperature, max_tokens).
    /// Maps service ID (or "default") to specific settings.
    /// </summary>
    [YamlMember(Alias = "execution_settings")]
    public Dictionary<string, ExecutionSettingConfig>? ExecutionSettings { get; set; }

    /// <summary>
    /// Represents the configuration for a single input variable.
    /// </summary>
    public class InputVariableConfig
    {
        /// <summary>
        /// Name of the input variable.
        /// </summary>
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the input variable.
        /// </summary>
        [YamlMember(Alias = "description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Default value for the input variable.
        /// </summary>
        [YamlMember(Alias = "defaultValue")] // Match case in reference
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Indicates if the input variable is required.
        /// </summary>
        [YamlMember(Alias = "isRequired")] // Match case in reference
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// Represents the execution settings for a specific AI service.
    /// </summary>
    public class ExecutionSettingConfig
    {
        /// <summary>
        /// The maximum number of tokens to generate.
        /// </summary>
        [YamlMember(Alias = "max_tokens", ApplyNamingConventions = false)]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Controls randomness in the generation (0.0 to 1.0).
        /// </summary>
        [YamlMember(Alias = "temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Controls diversity via nucleus sampling (0.0 to 1.0).
        /// </summary>
        [YamlMember(Alias = "top_p", ApplyNamingConventions = false)]
        public double? TopP { get; set; }

        /// <summary>
        /// Penalizes new tokens based on presence in the text so far.
        /// </summary>
        [YamlMember(Alias = "presence_penalty", ApplyNamingConventions = false)]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Penalizes new tokens based on frequency in the text so far.
        /// </summary>
        [YamlMember(Alias = "frequency_penalty", ApplyNamingConventions = false)]
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Sequences where the AI will stop generating further tokens.
        /// </summary>
        [YamlMember(Alias = "stop_sequences", ApplyNamingConventions = false)]
        public List<string>? StopSequences { get; set; }
        
        // Add other potential settings as needed (e.g., ChatSystemPrompt)

        /// <summary>
        /// Converts this configuration object to Semantic Kernel's PromptExecutionSettings.
        /// </summary>
        /// <returns>A PromptExecutionSettings object.</returns>
        public PromptExecutionSettings ToPromptExecutionSettings()
        {
            var settings = new PromptExecutionSettings();

            settings.ExtensionData ??= new Dictionary<string, object>();
            
            settings.ExtensionData["max_tokens"] = MaxTokens!;
            settings.ExtensionData["temperature"] = Temperature!;
            settings.ExtensionData["top_p"] = TopP!;
            settings.ExtensionData["presence_penalty"] = PresencePenalty!;
            settings.ExtensionData["frequency_penalty"] = FrequencyPenalty!;
            settings.ExtensionData["stop_sequences"] = StopSequences!;

            return settings;    
        }
    }

    /// <summary>
    /// Deserializes a PromptConfig from a YAML file.
    /// </summary>
    /// <param name="filePath">The absolute path to the .prompt.yaml file.</param>
    /// <returns>The deserialized PromptConfig.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown on YAML parsing errors.</exception>
    /// <exception cref="InvalidDataException">Thrown if essential data (like Prompt) is missing.</exception>
    public static PromptConfig FromFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("Prompt configuration file not found.", filePath);
        }
        var config = Deserializer.Deserialize<PromptConfig>(System.IO.File.ReadAllText(filePath));

        // Basic validation
        if (string.IsNullOrWhiteSpace(config.Prompt))
        {
            throw new InvalidDataException($"The 'prompt' field is missing or empty in configuration file: {filePath}");
        }
        // Add other validation as needed (e.g., schema version)

        return config;
    }

    /// <summary>
    /// Deserializes a PromptConfig from a YAML string.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The deserialized PromptConfig.</returns>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown on YAML parsing errors.</exception>
    /// <exception cref="InvalidDataException">Thrown if essential data (like Prompt) is missing.</exception>
    public static PromptConfig FromYaml(string yaml)
    {
        var config = Deserializer.Deserialize<PromptConfig>(yaml);
        if (string.IsNullOrWhiteSpace(config.Prompt))
        {
             throw new InvalidDataException("The 'prompt' field is missing or empty in the provided YAML content.");
        }
        return config;
    }
} 