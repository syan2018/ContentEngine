using ConfigurableAIProvider.Configuration;
using System.Threading.Tasks;
using System.IO; // Required for exception documentation

namespace ConfigurableAIProvider.Services;

/// <summary>
/// Loads agent configurations.
/// </summary>
public interface IAgentConfigLoader
{
    /// <summary>
    /// Loads the configuration for a specific agent, considering the environment.
    /// </summary>
    /// <param name="agentName">The name of the agent (corresponds to subdirectory name).</param>
    /// <returns>The loaded and merged AgentConfig.</returns>
    /// <exception cref="DirectoryNotFoundException">If the agent directory is not found.</exception>
    /// <exception cref="FileNotFoundException">If the agent.yaml file is missing.</exception>
    Task<AgentConfig> LoadConfigAsync(string agentName);
} 