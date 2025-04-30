using YamlDotNet.Serialization;
using System.Collections.Generic;

namespace ConfigurableAIProvider.Configuration;

/// <summary>
/// Represents a single reusable model definition from models.yaml.
/// </summary>
public class ModelDefinition
{
    [YamlMember(Alias = "connection")]
    public string? Connection { get; set; } // Required: Refers to a key in connections.yaml

    [YamlMember(Alias = "modelId")]
    public string? ModelId { get; set; } // Required: The actual model ID/deployment name

    [YamlMember(Alias = "endpointType")]
    public EndpointType EndpointType { get; set; } // Required: Type of the endpoint

    /// <summary>
    /// Optional dictionary for default execution parameters.
    /// Note: Currently informational; not automatically applied by the factory during kernel build.
    /// Application code can retrieve these via IModelProvider if needed.
    /// </summary>
    [YamlMember(Alias = "parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
} 