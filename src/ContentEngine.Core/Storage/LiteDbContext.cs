using ContentEngine.Core.DataPipeline.Models;
using ContentEngine.Core.Inference.Models;
using LiteDB;

namespace ContentEngine.Core.Storage;

/// <summary>
/// 管理 LiteDB 数据库连接和访问
/// </summary>
public class LiteDbContext : IDisposable
{
    public LiteDatabase Database { get; }

    // Schema 定义存储在一个固定的集合中
    private const string SchemaCollectionName = "_schemas";
    
    // 推理事务定义和实例的集合名称
    private const string ReasoningDefinitionsCollectionName = "_reasoningDefinitions";
    private const string ReasoningInstancesCollectionName = "_reasoningInstances";

    // 静态构造函数确保 LiteDB 映射配置只执行一次
    static LiteDbContext()
    {
        ConfigureGlobalMappings();
    }

    public LiteDbContext(IConfiguration configuration)
    {
        // 从配置中读取数据库路径，如果未配置则使用默认值
        // 建议将数据库文件放在项目外部或可配置的位置，避免随源码提交
        // 例如，可以在 appsettings.json 中配置 "LiteDbOptions:DatabasePath"
        var dbPath = configuration.GetValue<string>("LiteDbOptions:DatabasePath") ?? "ContentEngine.db";

        // 确保数据库所在的目录存在
        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        Database = new LiteDatabase(dbPath);

        // (可选) 在这里为 SchemaDefinition 模型配置 BSON 映射
        ConfigureMappings();

        // (可选) 在这里为 Schema 集合创建索引
        EnsureIndexes();
    }

    /// <summary>
    /// 获取用于存储 Schema 定义的集合
    /// </summary>
    public ILiteCollection<SchemaDefinition> SchemaDefinitions => Database.GetCollection<SchemaDefinition>(SchemaCollectionName);

    /// <summary>
    /// 获取用于存储推理事务定义的集合
    /// </summary>
    public ILiteCollection<ReasoningTransactionDefinition> ReasoningDefinitions => Database.GetCollection<ReasoningTransactionDefinition>(ReasoningDefinitionsCollectionName);

    /// <summary>
    /// 获取用于存储推理事务实例的集合
    /// </summary>
    public ILiteCollection<ReasoningTransactionInstance> ReasoningInstances => Database.GetCollection<ReasoningTransactionInstance>(ReasoningInstancesCollectionName);

    /// <summary>
    /// 根据 Schema 名称获取用于存储其实例数据的集合
    /// </summary>
    /// <param name="schemaName">Schema 的名称</param>
    /// <returns>用于存储该 Schema 实例的 BsonDocument 集合</returns>
    public ILiteCollection<BsonDocument> GetDataCollection(string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentException("Schema name cannot be empty.", nameof(schemaName));
        }
        // 避免使用以下划线开头的内部集合名称
        if (schemaName.StartsWith("_"))
        {
             throw new ArgumentException("Schema name cannot start with an underscore.", nameof(schemaName));
        }
        return Database.GetCollection<BsonDocument>(schemaName);
    }

    private void ConfigureMappings()
    {
        // 全局映射已在静态构造函数中配置
        // 这里可以保留其他特定于数据库实例的配置（如果需要的话）
    }

    /// <summary>
    /// 配置 LiteDB 全局映射 - 只在应用程序启动时执行一次
    /// </summary>
    private static void ConfigureGlobalMappings()
    {
        // ID 映射现在通过模型类上的 [BsonId] 属性来处理
        // 这里可以保留其他全局映射配置（如果需要的话）
    }

    private void EnsureIndexes()
    {
        // 为 SchemaDefinition 的 Name 字段创建唯一索引，确保 Schema 名称不重复
        SchemaDefinitions.EnsureIndex(x => x.Name, unique: true);
        
        // 为推理事务定义创建索引
        ReasoningDefinitions.EnsureIndex(x => x.Name, unique: false);
        ReasoningDefinitions.EnsureIndex(x => x.CreatedAt, unique: false);
        
        // 为推理事务实例创建索引
        ReasoningInstances.EnsureIndex(x => x.DefinitionId, unique: false);
        ReasoningInstances.EnsureIndex(x => x.Status, unique: false);
        ReasoningInstances.EnsureIndex(x => x.StartedAt, unique: false);
    }

    /// <summary>
    /// 清理推理引擎相关的数据库集合（用于解决主键类型变更后的兼容性问题）
    /// </summary>
    public void ClearReasoningData()
    {
        try
        {
            // 删除推理事务定义集合
            if (Database.CollectionExists(ReasoningDefinitionsCollectionName))
            {
                Database.DropCollection(ReasoningDefinitionsCollectionName);
                Console.WriteLine($"已清理集合: {ReasoningDefinitionsCollectionName}");
            }

            // 删除推理事务实例集合
            if (Database.CollectionExists(ReasoningInstancesCollectionName))
            {
                Database.DropCollection(ReasoningInstancesCollectionName);
                Console.WriteLine($"已清理集合: {ReasoningInstancesCollectionName}");
            }

            Console.WriteLine("推理引擎数据清理完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理推理引擎数据时发生错误: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        Database?.Dispose();
        GC.SuppressFinalize(this);
    }
} 