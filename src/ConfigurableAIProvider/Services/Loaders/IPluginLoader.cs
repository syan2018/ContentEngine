using Microsoft.SemanticKernel;
using System.Threading.Tasks;

namespace ConfigurableAIProvider.Services.Loaders;

/// <summary>
/// Interface for loading plugins into a Kernel instance.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Loads plugins from the configured source (e.g., directory) into the provided Kernel.
    /// </summary>
    /// <param name="kernel">The Kernel instance to load plugins into.</param>
    /// <returns>The Kernel instance with loaded plugins (for chaining, though modification happens in place).</returns>
    /// <remarks>
    /// Implementations should handle potential errors during loading gracefully (e.g., logging warnings).
    /// </remarks>
    Task<Kernel> LoadPluginsAsync(Kernel kernel);
} 