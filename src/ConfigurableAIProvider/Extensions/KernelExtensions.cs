using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ConfigurableAIProvider.Models; // Use models from this project
using System.Reflection;
using System.Runtime.Loader; // Potentially needed for advanced assembly loading, though LoadFrom is simpler
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel; // For DescriptionAttribute if used by KernelFunctionFactory

namespace ConfigurableAIProvider.Extensions
{
    /// <summary>
    /// Provides extension methods for Microsoft.SemanticKernel.Kernel
    /// related to loading plugins and functions from configuration files,
    /// based on the patterns observed in the semantic-kernel-from-config reference.
    /// </summary>
    public static class KernelExtensions
    {
        /// <summary>
        /// Imports a plugin into the kernel from a configuration file (PluginName.yaml).
        /// This method orchestrates the loading of both semantic and C# functions defined within the config.
        /// </summary>
        /// <param name="kernel">The kernel instance.</param>
        /// <param name="pluginConfigFile">Absolute path to the plugin's configuration file (e.g., PluginName.yaml).</param>
        /// <param name="pluginDirectory">Absolute path to the directory containing the plugin configuration file.</param>
        /// <param name="logger">Optional logger instance.</param>
        /// <returns>The imported KernelPlugin containing the loaded functions, or null if the plugin fails to load or contains no functions.</returns>
        public static KernelPlugin? ImportPluginFromConfig(
            this Kernel kernel,
            string pluginConfigFile,
            string pluginDirectory,
            ILogger? logger = null)
        {
            string logPrefix = $"Plugin Config '{pluginConfigFile}'";
            logger?.LogDebug("{LogPrefix}: Attempting to import plugin.", logPrefix);

            PluginConfig pluginConfig;
            try
            {
                // Load the plugin configuration structure from the YAML file
                pluginConfig = PluginConfig.FromFile(pluginConfigFile);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException || ex is YamlDotNet.Core.YamlException)
            {
                logger?.LogError(ex, "{LogPrefix}: Failed to load or parse configuration file. Skipping plugin.", logPrefix);
                return null; // Cannot proceed without valid config
            }
            catch (Exception ex) // Catch unexpected errors during loading
            {
                 logger?.LogError(ex, "{LogPrefix}: An unexpected error occurred while loading configuration file. Skipping plugin.", logPrefix);
                 return null;
            }

            // Determine plugin name: Use config 'name' first, then directory name as fallback
            string pluginName = pluginConfig.Name ?? Path.GetFileName(pluginDirectory);
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                logger?.LogError("{LogPrefix}: Plugin name is missing in config and could not be derived from the directory path '{DirectoryPath}'. Skipping plugin.", logPrefix, pluginDirectory);
                return null; // Invalid configuration: Plugin must have a name
            }

            logger?.LogInformation("{LogPrefix}: Loading plugin '{PluginName}' from directory: {DirectoryPath}", logPrefix, pluginName, pluginDirectory);

            var functions = new List<KernelFunction>();

            // --- Load Semantic Functions ---
            if (pluginConfig.Functions?.SemanticFunctions != null && pluginConfig.Functions.SemanticFunctions.Any())
            {
                logger?.LogDebug("{LogPrefix}: Found {Count} semantic function definition(s) for plugin '{PluginName}'.", logPrefix, pluginConfig.Functions.SemanticFunctions.Count, pluginName);
                foreach (var semanticFuncConfig in pluginConfig.Functions.SemanticFunctions)
                {
                    // Load semantic functions one by one
                    var function = LoadSemanticFunction(kernel, semanticFuncConfig, pluginDirectory, pluginName, logPrefix, logger);
                    if (function != null)
                    {
                        functions.Add(function);
                    }
                    // Errors logged within LoadSemanticFunction
                }
            }
            else
            {
                logger?.LogDebug("{LogPrefix}: No semantic functions defined for plugin '{PluginName}'.", logPrefix, pluginName);
            }

            // --- Load C# Functions ---
            if (pluginConfig.Functions?.CSharpFunctions != null && pluginConfig.Functions.CSharpFunctions.Any())
            {
                logger?.LogDebug("{LogPrefix}: Found {Count} C# function definition(s) for plugin '{PluginName}'.", logPrefix, pluginConfig.Functions.CSharpFunctions.Count, pluginName);
                foreach (var csharpFuncConfig in pluginConfig.Functions.CSharpFunctions)
                {
                    // Load all functions from the specified C# class
                    var csharpFunctions = LoadCSharpFunctions(csharpFuncConfig, pluginDirectory, pluginName, logPrefix, logger);
                    functions.AddRange(csharpFunctions); // AddRange handles empty list correctly
                     // Specific counts/errors logged within LoadCSharpFunctions
                }
            }
            else
            {
                logger?.LogDebug("{LogPrefix}: No C# functions defined for plugin '{PluginName}'.", logPrefix, pluginName);
            }

            // Check if any functions were successfully loaded
            if (!functions.Any())
            {
                logger?.LogWarning("{LogPrefix}: Plugin '{PluginName}' was processed, but contains no successfully loaded functions. Skipping plugin creation.", logPrefix, pluginName);
                return null; // Return null if no functions loaded
            }

            logger?.LogInformation("{LogPrefix}: Successfully loaded {FunctionCount} function(s) into memory for plugin '{PluginName}'. Creating plugin object.", logPrefix, functions.Count, pluginName);

            // Create the KernelPlugin instance from the collected functions
            try
            {
                return KernelPluginFactory.CreateFromFunctions(pluginName, pluginConfig.Description, functions);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "{LogPrefix}: Failed to create KernelPlugin object for '{PluginName}' after loading functions.", logPrefix, pluginName);
                return null;
            }
        }

        /// <summary>
        /// Loads a single semantic function based on its configuration. (Private Helper)
        /// </summary>
        private static KernelFunction? LoadSemanticFunction(
            Kernel kernel,
            PluginConfig.FunctionsNode.SemanticFunctionConfig config,
            string pluginDirectory,
            string pluginName,
            string parentLogPrefix, // For context
            ILogger? logger)
        {
            if (string.IsNullOrWhiteSpace(config.Path))
            {
                logger?.LogWarning("{ParentLogPrefix}: Skipping semantic function for plugin '{PluginName}' due to missing 'path' property in config.", parentLogPrefix, pluginName);
                return null;
            }

            string functionConfigFile = "";
            string logPrefix = ""; // Define before try block
            try
            {
                // Construct absolute path to the function's YAML config file
                functionConfigFile = Path.GetFullPath(Path.Combine(pluginDirectory, config.Path!));
                logPrefix = $"{parentLogPrefix}, Semantic Func Path '{config.Path}' -> '{functionConfigFile}'"; // Updated log prefix
                
                logger?.LogDebug("{LogPrefix}: Attempting to import semantic function.", logPrefix);

                // Load the specific function configuration
                var promptConfig = PromptConfig.FromFile(functionConfigFile);

                // Determine function name (use filename without extension)
                string functionName = Path.GetFileNameWithoutExtension(functionConfigFile);
                 logger?.LogTrace("{LogPrefix}: Function Name determined as '{FunctionName}'.", logPrefix, functionName);


                // Build PromptTemplateConfig based on loaded PromptConfig
                var templateConfig = new PromptTemplateConfig
                {
                    Template = promptConfig.Prompt, // Assume prompt is mandatory (validated in FromFile)
                    TemplateFormat = promptConfig.TemplateFormat ?? "semantic-kernel", // Default if null
                    Name = functionName, // Use filename as function name
                    Description = promptConfig.Description,
                    InputVariables = new List<InputVariable>(),
                    ExecutionSettings = new Dictionary<string, PromptExecutionSettings>()
                };

                // Map Input Variables from PromptConfig.InputConfig structure
                if (promptConfig.Input?.Parameters != null)
                {
                     logger?.LogTrace("{LogPrefix}: Mapping {Count} input variables.", logPrefix, promptConfig.Input.Parameters.Count);
                     templateConfig.InputVariables = promptConfig.Input.Parameters.ConvertAll(p => new InputVariable
                     {
                         Name = p.Name, // Assume Name is mandatory
                         Description = p.Description,
                         Default = p.DefaultValue,
                         IsRequired = p.IsRequired
                     });
                } else {
                      logger?.LogTrace("{LogPrefix}: No input variables defined in config.", logPrefix);
                }

                // Map Execution Settings using the RENAMED inner class
                if (promptConfig.ExecutionSettings != null)
                {
                     logger?.LogTrace("{LogPrefix}: Mapping {Count} execution setting groups.", logPrefix, promptConfig.ExecutionSettings.Count);
                     foreach (var kvp in promptConfig.ExecutionSettings)
                     {
                         string serviceId = kvp.Key; 
                         // Use the renamed type here:
                         PromptConfig.ExecutionSettingDetails settingDetails = kvp.Value;
                         templateConfig.ExecutionSettings.Add(serviceId, settingDetails.ToPromptExecutionSettings());
                         logger?.LogTrace("{LogPrefix}: Mapped execution settings for service '{ServiceId}'.", logPrefix, serviceId);
                     }
                } else {
                    logger?.LogTrace("{LogPrefix}: No execution settings defined in config.", logPrefix);
                }

                // Create the KernelFunction using the factory
                var function = KernelFunctionFactory.CreateFromPrompt(
                    templateConfig,
                    promptTemplateFactory: null, // Use default SK prompt template factory
                    loggerFactory: kernel.LoggerFactory // Reuse kernel's logger factory
                );

                logger?.LogInformation("{LogPrefix}: Successfully imported semantic function '{FunctionName}'.", logPrefix, functionName);
                return function;
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException || ex is YamlDotNet.Core.YamlException)
            {
                logger?.LogError(ex, "{LogPrefix}: Failed to load, parse, or validate function configuration file. Skipping function.", logPrefix);
                return null;
            }
            catch (Exception ex) // Catch unexpected errors during function creation
            {
                // Use functionName if available, otherwise Path.GetFileNameWithoutExtension
                string fnName = string.IsNullOrWhiteSpace(functionConfigFile) ? "[unknown function]" : Path.GetFileNameWithoutExtension(functionConfigFile);
                logger?.LogError(ex, "{LogPrefix}: An unexpected error occurred while importing semantic function '{FunctionName}'. Skipping function.", logPrefix, fnName);
                return null;
            }
        }

        /// <summary>
        /// Loads C# native functions from a specified DLL and class. (Private Helper)
        /// </summary>
        private static IEnumerable<KernelFunction> LoadCSharpFunctions(
            PluginConfig.FunctionsNode.CSharpFunctionConfig config,
            string pluginDirectory,
            string pluginName,
            string parentLogPrefix, // For context
            ILogger? logger)
        {
            // Validate configuration: ClassName is always required
            if (string.IsNullOrWhiteSpace(config.ClassName))
            {
                logger?.LogWarning("{ParentLogPrefix}: Skipping C# function definition for plugin '{PluginName}' due to missing 'className' property.", parentLogPrefix, pluginName);
                return [];
            }
            // Dll is optional. If null/empty, load from executing assembly.

            string logPrefix = $"{parentLogPrefix}, C# Config (Class: {config.ClassName}, DLL: {config.Dll ?? "(Executing Assembly)"})"; // Adjust log prefix
            logger?.LogDebug("{LogPrefix}: Attempting to load C# functions.", logPrefix);

            string assemblyLoadSourceDescription = ""; // For logging

            try
            {
                // Determine which assembly to load
                Assembly assembly;
                if (string.IsNullOrWhiteSpace(config.Dll))
                {
                     logger?.LogTrace("{LogPrefix}: DLL path not specified, attempting to load from the executing assembly.", logPrefix);
                     assembly = Assembly.GetExecutingAssembly();
                     assemblyLoadSourceDescription = "Executing Assembly";
                }
                else
                {
                    // 1. Resolve DLL Path (relative to plugin directory)
                    string dllPath = Path.GetFullPath(Path.Combine(pluginDirectory, config.Dll));
                    logger?.LogTrace("{LogPrefix}: Resolved DLL path to '{DllPath}'.", logPrefix, dllPath);
                    assemblyLoadSourceDescription = $"DLL: {dllPath}";

                    if (!File.Exists(dllPath))
                    {
                        logger?.LogError("{LogPrefix}: Assembly file not found at resolved path: {DllPath}. Skipping.", logPrefix, dllPath);
                        return Enumerable.Empty<KernelFunction>();
                    }

                    // 2. Load Assembly from specified path
                    assembly = Assembly.LoadFrom(dllPath);
                }
                logger?.LogTrace("{LogPrefix}: Successfully loaded assembly from {AssemblySource}.", logPrefix, assemblyLoadSourceDescription);


                // 3. Find the Class Type
                var classType = assembly.GetType(config.ClassName);
                if (classType == null)
                {
                    logger?.LogError("{LogPrefix}: Class '{ClassName}' not found within assembly loaded from {AssemblySource}. Skipping.", logPrefix, config.ClassName, assemblyLoadSourceDescription);
                    return [];
                }
                logger?.LogTrace("{LogPrefix}: Found class type '{ClassName}'.", logPrefix, classType.FullName);

                // 4. Create an Instance of the Class (Requires parameterless constructor)
                object classInstance;
                try
                {
                    classInstance = Activator.CreateInstance(classType)
                                    ?? throw new InvalidOperationException("Activator.CreateInstance returned null.");
                    logger?.LogTrace("{LogPrefix}: Successfully created instance of class '{ClassName}'.", logPrefix, config.ClassName);
                }
                catch (MissingMethodException mmEx)
                {
                    logger?.LogError(mmEx, "{LogPrefix}: Class '{ClassName}' does not have a public parameterless constructor. Cannot create instance. Skipping.", logPrefix, config.ClassName);
                    return [];
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "{LogPrefix}: Failed to create instance of class '{ClassName}'. Skipping.", logPrefix, config.ClassName);
                    return [];
                }

                // 5. Create KernelFunctions from methods marked with [KernelFunction]
                var functions = new List<KernelFunction>();
                var methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                    .Where(m => m.GetCustomAttribute<KernelFunctionAttribute>() != null);

                logger?.LogTrace("{LogPrefix}: Found {MethodCount} methods with [KernelFunction] attribute in class '{ClassName}'.", logPrefix, methods.Count(), config.ClassName);
                
                foreach (var methodInfo in methods)
                {
                    try
                    {
                        object? target = methodInfo.IsStatic ? null : classInstance;
                        var function = KernelFunctionFactory.CreateFromMethod(methodInfo, target, loggerFactory: null);
                        functions.Add(function);
                        logger?.LogTrace("{LogPrefix}: Successfully created KernelFunction from method '{MethodName}'.", logPrefix, methodInfo.Name);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "{LogPrefix}: Failed to create KernelFunction from method '{MethodName}'. Skipping this method.", logPrefix, methodInfo.Name);
                    }
                }

                logger?.LogInformation("{LogPrefix}: Successfully created {Count} KernelFunction object(s) from class '{ClassName}'.", logPrefix, functions.Count, config.ClassName);
                return functions;
            }
            catch (FileNotFoundException ex) // Can happen if assembly dependencies are missing
            {
                logger?.LogError(ex, "{LogPrefix}: Assembly or one of its dependencies not found (loading from {AssemblySource}). Ensure all required DLLs are present. Skipping.", logPrefix, assemblyLoadSourceDescription);
                return [];
            }
            catch (BadImageFormatException ex)
            {
                logger?.LogError(ex, "{LogPrefix}: Assembly file is invalid or incompatible (loading from {AssemblySource}). Skipping.", logPrefix, assemblyLoadSourceDescription);
                return [];
            }
            catch (Exception ex) // Catch-all for other loading/reflection errors
            {
                logger?.LogError(ex, "{LogPrefix}: An unexpected error occurred while loading or importing C# functions (Source: {AssemblySource}). Skipping.", logPrefix, assemblyLoadSourceDescription);
                return [];
            }
        }
    }
} 