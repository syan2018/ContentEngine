using ConfigurableAIProvider.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConfigurableAIProvider.Models;

/// <summary>
/// Represents the entire collection of model definitions loaded from models.yaml.
/// </summary>
public class ModelsConfig
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
           .WithNamingConvention(CamelCaseNamingConvention.Instance)
           .IgnoreUnmatchedProperties()
           .Build();

    /// <summary>
    /// Dictionary mapping unique model definition IDs (e.g., "azure-gpt4o-mini-std")
    /// to their corresponding ModelConfig objects.
    /// </summary>
    [YamlMember(Alias = "models")]
    public Dictionary<string, ModelConfig>? Models { get; set; }

    /// <summary>
    /// 从文件加载并合并环境特定的模型配置。
    /// 会自动合并 models.yaml 和 models.{environment}.yaml 文件。
    /// </summary>
    /// <param name="baseFilePath">基础配置文件路径 (models.yaml)</param>
    /// <param name="environment">环境名称 (如 "dev", "prod")</param>
    /// <returns>合并后的模型配置</returns>
    public static ModelsConfig FromFileWithEnvironment(string baseFilePath, string environment = "dev")
    {
        if (!File.Exists(baseFilePath))
        {
            throw new FileNotFoundException($"基础模型配置文件未找到: {baseFilePath}");
        }

        var baseConfig = FromFile(baseFilePath);

        // 构建环境特定的文件路径
        var directory = Path.GetDirectoryName(baseFilePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(baseFilePath);
        var extension = Path.GetExtension(baseFilePath);
        var envFilePath = Path.Combine(directory, $"{fileName}.{environment}{extension}");

        // 如果环境特定文件存在，则合并配置
        if (File.Exists(envFilePath))
        {
            var envConfig = FromFile(envFilePath);
            return MergeConfigs(baseConfig, envConfig);
        }

        return baseConfig;
    }

    /// <summary>
    /// 合并两个模型配置，环境配置会覆盖基础配置中的同名模型。
    /// </summary>
    /// <param name="baseConfig">基础配置</param>
    /// <param name="envConfig">环境特定配置</param>
    /// <returns>合并后的配置</returns>
    private static ModelsConfig MergeConfigs(ModelsConfig baseConfig, ModelsConfig envConfig)
    {
        var mergedConfig = new ModelsConfig
        {
            Models = new Dictionary<string, ModelConfig>()
        };

        // 先复制基础配置中的所有模型
        if (baseConfig.Models != null)
        {
            foreach (var kvp in baseConfig.Models)
            {
                mergedConfig.Models[kvp.Key] = kvp.Value;
            }
        }

        // 然后用环境配置覆盖或添加模型
        if (envConfig.Models != null)
        {
            foreach (var kvp in envConfig.Models)
            {
                mergedConfig.Models[kvp.Key] = kvp.Value;
            }
        }

        return mergedConfig;
    }

    public static ModelsConfig FromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Models configuration file not found: {filePath}");
        }
        return FromYaml(File.ReadAllText(filePath));
    }

    public static ModelsConfig FromYaml(string yaml)
    {
        try
        {
            return Deserializer.Deserialize<ModelsConfig>(yaml);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidDataException($"Error parsing models YAML: {ex.Message}", ex);
        }
    }
} 