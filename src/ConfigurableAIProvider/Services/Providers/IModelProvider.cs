
using ConfigurableAIProvider.Models; 

namespace ConfigurableAIProvider.Services.Providers;

/// <summary>
/// Provides access to centrally defined AI model configurations (ModelConfig).
/// </summary>
public interface IModelProvider
{
    /// <summary>
    /// Gets the configuration definition for a specific model by its unique ID.
    /// </summary>
    /// <param name="modelDefinitionId">The unique identifier of the model definition (key in models.yaml).</param>
    /// <returns>The corresponding ModelConfig.</returns>
    /// <exception cref="KeyNotFoundException">If the model definition ID is not found.</exception>
    Task<ModelConfig> GetModelDefinitionAsync(string modelDefinitionId);
} 