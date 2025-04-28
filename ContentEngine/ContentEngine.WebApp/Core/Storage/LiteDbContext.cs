using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using ContentEngine.WebApp.Core.DataPipeline.Models;

namespace ContentEngine.WebApp.Core.Storage;

/// <summary>
/// 管理 LiteDB 数据库连接和访问
/// </summary>
public class LiteDbContext : IDisposable
{
    public LiteDatabase Database { get; }

    // Schema 定义存储在一个固定的集合中
    private const string SchemaCollectionName = "_schemas";

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
        var mapper = BsonMapper.Global;
        // 如果需要，可以在这里添加自定义的类型映射
        // 例如: mapper.Entity<SchemaDefinition>().Id(x => x.Id);
        // 对于简单场景，LiteDB 的默认映射通常足够
    }

    private void EnsureIndexes()
    {
        // 为 SchemaDefinition 的 Name 字段创建唯一索引，确保 Schema 名称不重复
        SchemaDefinitions.EnsureIndex(x => x.Name, unique: true);
    }

    public void Dispose()
    {
        Database?.Dispose();
        GC.SuppressFinalize(this);
    }
} 