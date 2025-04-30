using ConfigurableAIProvider.Configuration;
using Microsoft.SemanticKernel;

namespace ConfigurableAIProvider.Services.Configurators;

/// <summary>
/// Interface for configuring AI services on a KernelBuilder based on specific provider types.
/// </summary>
public interface IAIServiceConfigurator
{
    /// <summary>
    /// Gets the ServiceType that this configurator handles.
    /// </summary>
    ServiceType HandledServiceType { get; }

    /// <summary>
    /// Configures the KernelBuilder by adding the appropriate AI service.
    /// </summary>
    /// <param name="builder">The IKernelBuilder to configure.</param>
    /// <param name="modelDefinition">The centrally defined model configuration.</param>
    /// <param name="connectionConfig">The resolved connection configuration.</param>
    void ConfigureService(IKernelBuilder builder, ModelDefinition modelDefinition, ConnectionConfig connectionConfig);
} 