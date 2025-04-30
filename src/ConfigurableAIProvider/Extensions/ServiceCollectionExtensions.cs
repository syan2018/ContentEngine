using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions; // Needed for TryAddSingleton/TryAddScoped
using Microsoft.Extensions.Logging; // Needed for ILogger injection
using Microsoft.Extensions.Logging.Abstractions; // Added for NullLogger
using Microsoft.Extensions.Options; // Ensures AddOptions is available
using System;
using Microsoft.SemanticKernel; // Required for Kernel

namespace ConfigurableAIProvider.Extensions;

/// <summary>
/// Extension methods for setting up Configurable AI Provider services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Configurable AI Provider services, enabling AI Kernel creation based on external Agent configurations.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configuration">The application configuration. Expects a "ConfigurableAI" section.</param>
    /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddConfigurableAIProvider(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Configure Options ---
        // Binds the "ConfigurableAI" section from appsettings.json (or other config sources) to ConfigurableAIOptions
        services.AddOptions<ConfigurableAIOptions>()
            .Bind(configuration.GetSection(ConfigurableAIOptions.SectionName))
            .ValidateDataAnnotations() // Optional: Enables validation based on DataAnnotations in ConfigurableAIOptions
            .ValidateOnStart(); // Optional: Validates options during application startup

        // --- Register Core Services ---

        // TryAddSingleton ensures these are only registered once, even if this method is called multiple times.
        // ConnectionProvider loads connections.yaml and caches resolved values. Singleton is appropriate.
        services.TryAddSingleton<IConnectionProvider, ConnectionProvider>();

        // AgentConfigLoader loads agent.yaml files and caches them. Singleton is appropriate.
        services.TryAddSingleton<IAgentConfigLoader, AgentConfigLoader>();

        // DefaultAIKernelFactory uses the loaders to build Kernels. Scoped is usually best for Kernel factories
        // as Kernels themselves often have scoped dependencies or manage state that shouldn't be singleton.
        services.TryAddScoped<IAIKernelFactory, DefaultAIKernelFactory>();

        // --- Ensure Necessary Logging is Available ---
        // The services above depend on ILogger<T>. AddLogging() normally registers ILoggerFactory.
        // TryAdd ensures it's safe if logging is already configured.
        services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>(); // Provide NullLoggerFactory as a fallback
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>))); // Use framework's default Logger<T>


        // --- Remove Old/Obsolete Registrations (if they existed previously) ---
        // services.RemoveAll<IOptions<AIServiceOptions>>(); // Example if AIServiceOptions was used
        // services.RemoveAll<IPluginLoader>();           // Example if IPluginLoader was used


        // --- Optional: Default Kernel Registration (Example, Use with Caution) ---
        // You might want a "default" Kernel based on a specific agent name.
        // This depends heavily on application needs. Injecting IAIKernelFactory and calling
        // BuildKernelAsync("MyDefaultAgent") where needed is generally more explicit and flexible.
        /*
        services.AddScoped<Kernel>(sp =>
        {
            var factory = sp.GetRequiredService<IAIKernelFactory>();
            const string defaultAgentName = "DefaultAgent"; // Or get from configuration
            try
            {
                // Need to block here for service registration, which is discouraged.
                // Consider creating kernels asynchronously after service provider is built if possible.
                return factory.BuildKernelAsync(defaultAgentName).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var logger = sp.GetService<ILogger<Kernel>>() ?? NullLogger<Kernel>.Instance;
                logger.LogCritical(ex, "Failed to build the default Kernel for agent '{AgentName}'. Application might not function correctly.", defaultAgentName);
                // Depending on the app, either throw or return a non-functional Kernel/null.
                // Throwing is often safer during startup.
                throw new InvalidOperationException($"Failed to build the default Kernel '{defaultAgentName}'.", ex);
            }
        });
        */

        return services;
    }
} 