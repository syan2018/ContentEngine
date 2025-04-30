using Microsoft.SemanticKernel;
using System.Threading.Tasks;

namespace ConfigurableAIProvider.Services;

/// <summary>
/// Interface for creating Kernel instances based on named configurations (Agents).
/// </summary>
public interface IAIKernelFactory
{
    /// <summary>
    /// Builds a Kernel instance configured according to the specified agent name.
    /// </summary>
    /// <param name="agentName">The name of the agent configuration to load.</param>
    /// <returns>A configured Kernel instance.</returns>
    /// <exception cref="System.Exception">Throws exceptions if configuration loading or Kernel building fails.</exception>
    Task<Kernel> BuildKernelAsync(string agentName);
} 