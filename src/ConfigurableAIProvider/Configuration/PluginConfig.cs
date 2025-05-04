using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Represents the configuration for a Semantic Kernel plugin, typically loaded from a plugin.yaml file.
/// </summary>
public class PluginConfig
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        // Allow properties not present in YAML to be ignored during deserialization
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Optional name of the plugin. If not provided, it might be inferred from the directory name.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Optional description of the plugin.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Configuration for the functions contained within the plugin.
    /// </summary>
    [YamlMember(Alias = "functions")] // Match case used in reference
    public FunctionsNode? Functions { get; set; }

    /// <summary>
    /// Represents the 'functions' node in plugin.yaml.
    /// </summary>
    public class FunctionsNode // Renamed from FunctionsConfig to avoid conflict
    {
        /// <summary>
        /// List of semantic function configurations.
        /// </summary>
        [YamlMember(Alias = "semanticFunctions")]
        public List<SemanticFunctionConfig>? SemanticFunctions { get; set; }

        /// <summary>
        /// List of C# (native) function configurations.
        /// </summary>
        [YamlMember(Alias = "cSharpFunctions")]
        public List<CSharpFunctionConfig>? CSharpFunctions { get; set; } // Corrected type to List
    }

    /// <summary>
    /// Configuration for a single semantic function reference.
    /// </summary>
    public class SemanticFunctionConfig
    {
        /// <summary>
        /// Relative path to the .prompt.yaml file for the semantic function.
        /// </summary>
        [YamlMember(Alias = "file")] // Changed from 'path' to 'file' based on KernelExtensions logic
        public string? File { get; set; }

        // ExposeToPlanner might be handled differently in newer SK or planner implementations
        // [YamlMember(Alias = "exposeToPlanner")]
        // public bool ExposeToPlanner { get; set; } = true;
    }

    /// <summary>
    /// Configuration for a single C# function reference.
    /// </summary>
    public class CSharpFunctionConfig
    {
        /// <summary>
        /// Relative path to the DLL containing the native function.
        /// </summary>
        [YamlMember(Alias = "dll")]
        public string? Dll { get; set; }

        /// <summary>
        /// The fully qualified name of the class containing the native functions.
        /// </summary>
        [YamlMember(Alias = "className")]
        public string? ClassName { get; set; }

        // ExposeToPlanner might be handled differently
        // [YamlMember(Alias = "exposeToPlanner")]
        // public bool? ExposeToPlanner { get; set; }
    }

    /// <summary>
    /// Deserializes a PluginConfig from a YAML file.
    /// </summary>
    /// <param name="filePath">The absolute path to the plugin.yaml file.</param>
    /// <returns>The deserialized PluginConfig.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown on YAML parsing errors.</exception>
    public static PluginConfig FromFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("Plugin configuration file not found.", filePath);
        }
        return Deserializer.Deserialize<PluginConfig>(System.IO.File.ReadAllText(filePath));
    }

    /// <summary>
    /// Deserializes a PluginConfig from a YAML string.
    /// </summary>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>The deserialized PluginConfig.</returns>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown on YAML parsing errors.</exception>
    public static PluginConfig FromYaml(string yaml)
    {
        return Deserializer.Deserialize<PluginConfig>(yaml);
    }
} 