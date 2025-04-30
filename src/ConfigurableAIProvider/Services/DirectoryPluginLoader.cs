using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ConfigurableAIProvider.Services;

/// <summary>
/// Loads Semantic Kernel plugins from a specified directory structure.
/// Assumes each subdirectory in the configured path is a plugin, 
/// and each subdirectory within that is a semantic function.
/// </summary>
public class DirectoryPluginLoader : IPluginLoader
{
    private readonly PluginOptions _options;
    private readonly ILogger<DirectoryPluginLoader> _logger;

    public DirectoryPluginLoader(IOptions<PluginOptions> options, ILogger<DirectoryPluginLoader>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? NullLogger<DirectoryPluginLoader>.Instance;

        if (string.IsNullOrWhiteSpace(_options.DirectoryPath))
        {
            _logger.LogWarning("Plugin directory path is not configured in PluginOptions. No directory-based plugins will be loaded.");
        }
    }

    public Task<Kernel> LoadPluginsAsync(Kernel kernel)
    {
        if (string.IsNullOrWhiteSpace(_options.DirectoryPath))
        {
            return Task.FromResult(kernel); // No path configured, do nothing
        }

        // Ensure the path is absolute or relative to the application base directory
        string rootDirectory = Path.GetFullPath(_options.DirectoryPath, AppContext.BaseDirectory);

        if (!Directory.Exists(rootDirectory))
        {
            _logger.LogWarning("Configured plugin directory does not exist: {DirectoryPath}. No directory-based plugins will be loaded.", rootDirectory);
            return Task.FromResult(kernel);
        }

        _logger.LogInformation("Scanning for plugins in directory: {DirectoryPath}", rootDirectory);

        try
        {
            foreach (var pluginDirectory in Directory.EnumerateDirectories(rootDirectory))
            {
                var pluginName = Path.GetFileName(pluginDirectory);
                if (string.IsNullOrWhiteSpace(pluginName))
                {
                    continue;
                }

                try
                {
                    // Semantic Kernel's built-in function to load from the standard directory structure
                    // It expects PluginName/FunctionName/{skprompt.txt, config.json}
                    // Passing the parent directory (pluginDirectory) and pluginName works.
                    kernel.ImportPluginFromPromptDirectory(pluginDirectory, pluginName);
                    _logger.LogInformation("Successfully loaded plugin '{PluginName}' from {Directory}", pluginName, pluginDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin '{PluginName}' from directory {Directory}", pluginName, pluginDirectory);
                    // Optionally continue loading other plugins
                }
            }
            // TODO: Add logic here to load Native Functions if needed, potentially based on a manifest file within the plugin directory.
        }
        catch (Exception ex)
        { 
             _logger.LogError(ex, "An error occurred while scanning plugin directory: {DirectoryPath}", rootDirectory);
             // Decide if this should prevent further execution or just log
        }
        

        return Task.FromResult(kernel);
    }
} 