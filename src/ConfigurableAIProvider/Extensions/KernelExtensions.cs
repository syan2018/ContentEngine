using ConfigurableAIProvider.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks; // Added for Task
using YamlDotNet.Core; // Added for YamlException
using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Models; // Make sure this is present

namespace ConfigurableAIProvider.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IKernel"/> and <see cref="Kernel"/>
/// related to loading plugins and functions from configuration files.
/// </summary>
public static class KernelExtensions
{
    /// <summary>
    /// Imports a plugin into the kernel based on a plugin.yaml configuration file.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="pluginConfigFile">Absolute path to the plugin.yaml configuration file.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>The imported plugin (as a KernelPlugin object).</returns>
    /// <exception cref="FileNotFoundException">Thrown if the plugin config file is not found.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the plugin directory derived from the config file path is invalid.</exception>
    /// <exception cref="ArgumentException">Thrown if the plugin configuration is invalid (e.g., missing name and not derivable from directory).</exception>
    public static KernelPlugin ImportPluginFromConfig(
        this Kernel kernel, // Changed from IKernel to Kernel to access ImportPlugin methods easily
        string pluginConfigFile,
        ILogger? logger = null)
    {
        logger?.LogDebug("Importing plugin from config file: {FilePath}", pluginConfigFile);

        var pluginConfig = PluginConfig.FromFile(pluginConfigFile); // Can throw FileNotFoundException or YamlException
        string pluginDirectory = Path.GetDirectoryName(pluginConfigFile)
                                 ?? throw new DirectoryNotFoundException($"Could not determine directory for plugin config file: {pluginConfigFile}");

        string pluginName = pluginConfig.Name ?? Path.GetFileName(pluginDirectory);
        if (string.IsNullOrWhiteSpace(pluginName))
        {
            throw new ArgumentException($"Plugin name is missing in '{pluginConfigFile}' and could not be derived from the directory path.", nameof(pluginConfigFile));
        }

        var functions = new List<KernelFunction>();

        // --- Load Semantic Functions ---
        if (pluginConfig.Functions?.SemanticFunctions != null)
        {
            foreach (var semanticFunctionConfig in pluginConfig.Functions.SemanticFunctions)
            {
                if (string.IsNullOrWhiteSpace(semanticFunctionConfig.File))
                {
                    logger?.LogWarning("Skipping semantic function in plugin '{PluginName}' due to missing 'file' property.", pluginName);
                    continue;
                }

                try
                {
                    // Construct absolute path to the prompt file
                    string promptFilePath = Path.GetFullPath(Path.Combine(pluginDirectory, semanticFunctionConfig.File));
                    
                    var function = ImportSemanticFunctionFromConfig(kernel, promptFilePath, pluginName, logger);
                    if (function != null) // Function can be null if file not found or invalid
                    {
                         functions.Add(function);
                    }
                }
                catch (Exception ex)
                {
                     // Log errors during individual function loading but continue with others
                    logger?.LogError(ex, "Failed to load semantic function from '{PromptFile}' for plugin '{PluginName}'. Skipping this function.", semanticFunctionConfig.File, pluginName);
                }
            }
        }
        else
        {
            logger?.LogDebug("No semantic functions defined in plugin '{PluginName}'.", pluginName);
        }

        // --- Load C# Functions (Placeholder) ---
        if (pluginConfig.Functions?.CSharpFunctions != null && pluginConfig.Functions.CSharpFunctions.Count > 0)
        {
            // TODO: Implement dynamic loading of C# functions from DLLs.
            // This requires AssemblyLoadContext and reflection, which is complex.
            logger?.LogWarning("Loading C# functions defined in plugin '{PluginName}' is NOT IMPLEMENTED in this version of ConfigurableAIProvider.", pluginName);
            // foreach (var cSharpFunctionConfig in pluginConfig.Functions.CSharpFunctions) { ... }
        }


        if (functions.Count == 0)
        {
             logger?.LogWarning("Plugin '{PluginName}' was loaded, but it contains no successfully loaded functions.", pluginName);
        } else {
             logger?.LogInformation("Successfully loaded {FunctionCount} function(s) for plugin '{PluginName}'.", functions.Count, pluginName);
        }

        // Create and return the plugin
        // Note: KernelPlugin.Create requires functions. Consider Kernel.Plugins.Add if issues arise.
         return KernelPluginFactory.CreateFromFunctions(pluginName, pluginConfig.Description, functions);
        // Alternatively, if the above causes issues or for different SK versions:
        // var plugin = new KernelPlugin(pluginName, pluginConfig.Description, functions);
        // kernel.Plugins.Add(plugin); // May need adjustment based on SK version
        // return plugin;

    }

    /// <summary>
    /// Imports a single semantic function into the kernel based on a .prompt.yaml configuration file.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="promptConfigFile">Absolute path to the .prompt.yaml configuration file.</param>
    /// <param name="pluginName">The name of the plugin this function belongs to.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>The imported KernelFunction, or null if loading fails.</returns>
    public static KernelFunction? ImportSemanticFunctionFromConfig(
        this Kernel kernel, // Use Kernel for consistency
        string promptConfigFile,
        string? pluginName = null,
        ILogger? logger = null)
    {
        logger?.LogDebug("Importing semantic function from config file: {FilePath}", promptConfigFile);

        try
        {
            var promptConfig = PromptConfig.FromFile(promptConfigFile); // Can throw FileNotFound, YamlException, InvalidDataException
            string functionName = Path.GetFileNameWithoutExtension(promptConfigFile); // Use filename as function name

            var templateConfig = new PromptTemplateConfig
            {
                Template = promptConfig.Prompt,
                TemplateFormat = promptConfig.TemplateFormat,
                Name = functionName, // Set function name here
                Description = promptConfig.Description,
                InputVariables = new List<InputVariable>(), // Map InputVariable from PromptConfig
                // OutputVariable mapping might be needed depending on SK version/usage
                 ExecutionSettings = new Dictionary<string, PromptExecutionSettings>() // Map ExecutionSettings from PromptConfig
            };

            // Map Input Variables
            if (promptConfig.InputVariables != null)
            {
                 templateConfig.InputVariables = promptConfig.InputVariables.ConvertAll(iv => new InputVariable
                 {
                     Name = iv.Name,
                     Description = iv.Description,
                     Default = iv.DefaultValue, // Correct property name
                     IsRequired = iv.IsRequired
                 });
            }
            
            // Map Execution Settings
            if (promptConfig.ExecutionSettings != null)
            {
                foreach(var kvp in promptConfig.ExecutionSettings)
                {
                    templateConfig.ExecutionSettings.Add(kvp.Key, kvp.Value.ToPromptExecutionSettings());
                }
            }


            // Create the function using KernelFunctionFactory
            var function = KernelFunctionFactory.CreateFromPrompt(
                templateConfig,
                promptTemplateFactory: null, // Use default factory
                loggerFactory: kernel.LoggerFactory // Use kernel's logger factory
            );

             logger?.LogInformation("Successfully imported semantic function '{FunctionName}' from '{FilePath}'.", functionName, promptConfigFile);
            return function;
        }
        catch (FileNotFoundException ex)
        {
            logger?.LogError(ex, "Prompt configuration file not found: {FilePath}. Skipping function.", promptConfigFile);
            return null;
        }
        catch (InvalidDataException ex)
        {
             logger?.LogError(ex, "Invalid data in prompt configuration file: {FilePath}. Skipping function.", promptConfigFile);
             return null;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
             logger?.LogError(ex, "Failed to parse YAML in prompt configuration file: {FilePath}. Skipping function.", promptConfigFile);
             return null;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An unexpected error occurred while importing semantic function from '{FilePath}'. Skipping function.", promptConfigFile);
            return null;
        }
    }

     // Helper method from the reference (slightly adapted) - Currently UNUSED as C# functions are not implemented
    // public static Dictionary<string, ISKFunction> ImportNativeFunctionsFromDll(
    //     this IKernel kernel,
    //     PluginConfig.CSharpFunctionConfig functionConfig,
    //     string? pluginName = null,
    //     ITrustService? trustService = null)
    // {
    //      logger?.LogWarning("ImportNativeFunctionsFromDll is not implemented.");
    //      return new Dictionary<string, ISKFunction>();
         // TODO: Implement C# function loading logic here if needed in the future.
         // Requires loading assembly (AssemblyLoadContext), finding the type (functionConfig.ClassName),
         // creating an instance, and using kernel.ImportFunctions to import methods.
         // Handle security considerations (trustService).
    // }
} 