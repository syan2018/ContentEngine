using ConfigurableAIProvider.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic; // For KeyNotFoundException docs

namespace ConfigurableAIProvider.Services.Providers;

/// <summary>
/// Provides access to centrally defined AI model configurations (ModelDefinition).
/// </summary>
public interface IModelProvider
{
    /// <summary>
    /// Gets the configuration definition for a specific model by its unique ID.
    /// </summary>
    /// <param name="modelDefinitionId">The unique identifier of the model definition (key in models.yaml).</param>
    /// <returns>The corresponding ModelDefinition.</returns>
    /// <exception cref="KeyNotFoundException">If the model definition ID is not found.</exception>
    Task<ModelDefinition> GetModelDefinitionAsync(string modelDefinitionId);
} 