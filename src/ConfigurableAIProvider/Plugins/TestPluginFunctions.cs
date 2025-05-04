using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ConfigurableAIProvider.Plugins
{
    /// <summary>
    /// A simple plugin for testing C# function loading.
    /// </summary>
    public class TestPluginFunctions
    {
        /// <summary>
        /// Returns a greeting string.
        /// </summary>
        /// <param name="name">The name to greet.</param>
        /// <returns>A greeting message.</returns>
        [KernelFunction]
        [Description("Says hello to the specified name.")]
        public string SayHello([Description("The name to say hello to")] string name)
        {   
            return $"Hello, {name} from the Test Plugin!";
        }

        // Add more simple functions here if needed for testing
    }
} 