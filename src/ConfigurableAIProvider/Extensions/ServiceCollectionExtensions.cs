using ConfigurableAIProvider.Configuration;
using ConfigurableAIProvider.Services.Loaders;
using ConfigurableAIProvider.Services.Providers;
using ConfigurableAIProvider.Services.Configurators;
using ConfigurableAIProvider.Services.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        services.AddOptions<ConfigurableAIOptions>()
            .Bind(configuration.GetSection(ConfigurableAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // --- Register Core Services ---
        services.TryAddSingleton<IConnectionProvider, ConnectionProvider>();
        services.TryAddSingleton<IAgentConfigLoader, AgentConfigLoader>();
        services.TryAddSingleton<IModelProvider, ModelProvider>();
        services.TryAddScoped<IAIKernelFactory, DefaultAIKernelFactory>();

        // --- Register AI Service Configurators (Use TryAddEnumerable) ---
        // This ensures all configurators are added to the collection injected via IEnumerable<IAIServiceConfigurator>
        services.TryAddEnumerable([
            // Register each configurator implementation
            ServiceDescriptor.Singleton<IAIServiceConfigurator, AzureOpenAIServiceConfigurator>(),
            ServiceDescriptor.Singleton<IAIServiceConfigurator, OpenAIServiceConfigurator>(),
            ServiceDescriptor.Singleton<IAIServiceConfigurator, OllamaServiceConfigurator>()
            // Add other configurators here if needed
        ]);

        // --- Ensure Necessary Logging is Available ---
        services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        return services;
    }
} 