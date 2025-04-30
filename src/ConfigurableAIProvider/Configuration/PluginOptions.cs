namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Options for configuring plugin loading.
/// </summary>
public class PluginOptions
{
    public const string SectionName = "Plugins";

    /// <summary>
    /// The root directory path where plugin subdirectories are located.
    /// Can be relative (to application base directory) or absolute.
    /// </summary>
    public string? DirectoryPath { get; set; } = "Plugins"; // Default to a "Plugins" subdirectory
} 