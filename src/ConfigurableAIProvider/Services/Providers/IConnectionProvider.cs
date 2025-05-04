using ConfigurableAIProvider.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using ConfigurableAIProvider.Models; // Required for KeyNotFoundException documentation

// Update namespace
namespace ConfigurableAIProvider.Services.Providers;

/// <summary>
/// Provides access to resolved AI service connection configurations.
/// </summary>
public interface IConnectionProvider
{
    /// <summary>
    /// Gets the resolved configuration for a specific connection by name.
    /// Placeholders (like {{ENV_VAR}}) in the configuration are expected to be replaced.
    /// </summary>
    /// <param name="connectionName">The logical name of the connection (key in connections.yaml).</param>
    /// <returns>The resolved ConnectionConfig.</returns>
    /// <exception cref="KeyNotFoundException">If the connection name is not found.</exception>
    /// <exception cref="System.InvalidOperationException">If placeholder resolution fails.</exception>
    Task<ConnectionConfig> GetResolvedConnectionAsync(string connectionName);
} 