using System.ComponentModel.DataAnnotations;
using System.IO; // Required for Path.Combine

namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Options for the Configurable AI Provider.
/// </summary>
public class ConfigurableAIOptions
{
    public const string SectionName = "ConfigurableAI";

    /// <summary>
    /// The root directory path where Agent configuration subdirectories are located.
    /// Can be relative (to application base directory) or absolute.
    /// Defaults to "Agents".
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string AgentsDirectory { get; set; } = "Agents";

    /// <summary>
    /// The path to the central connections configuration file.
    /// Can be relative (to application base directory) or absolute.
    /// Defaults to "Agents/connections.yaml".
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionsFilePath { get; set; } = Path.Combine("Agents", "connections.yaml");

    /// <summary>
    /// The path to the central model definitions configuration file.
    /// Can be relative (to application base directory) or absolute.
    /// Defaults to "Agents/models.yaml".
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ModelsFilePath { get; set; } = Path.Combine("Agents", "models.yaml");

    /// <summary>
    /// The optional root directory path for globally shared plugins.
    /// If set, plugins referenced in agent.yaml might be resolved against this path.
    /// </summary>
    public string? GlobalPluginsDirectory { get; set; } = "Plugins"; // Keep the old default for now

    /// <summary>
    /// The current operating environment (e.g., "dev", "prod").
    /// Used to load environment-specific configuration files like agent.dev.yaml.
    /// It's recommended to get this from ASPNETCORE_ENVIRONMENT or similar environment variables.
    /// Defaults to "dev".
    /// </summary>
    public string Environment { get; set; } = "dev";
} 