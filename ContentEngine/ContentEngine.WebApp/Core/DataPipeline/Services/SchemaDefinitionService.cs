using ContentEngine.WebApp.Core.DataPipeline.Models;
using ContentEngine.WebApp.Core.Storage;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContentEngine.WebApp.Core.DataPipeline.Services;

/// <summary>
/// 管理 SchemaDefinition 的服务实现
/// </summary>
public class SchemaDefinitionService : ISchemaDefinitionService
{
    private readonly LiteDbContext _liteDbContext;
    private readonly ILiteCollection<SchemaDefinition> _schemaCollection;

    public SchemaDefinitionService(LiteDbContext liteDbContext)
    {
        _liteDbContext = liteDbContext;
        _schemaCollection = _liteDbContext.SchemaDefinitions;
    }

    public Task<int> CreateSchemaAsync(SchemaDefinition schemaDefinition)
    {
        // 验证 Schema 名称是否已存在 (因为有唯一索引，插入时也会失败，但提前检查更友好)
        if (_schemaCollection.Exists(x => x.Name == schemaDefinition.Name))
        {
            throw new InvalidOperationException($"Schema with name '{schemaDefinition.Name}' already exists.");
        }

        // 可以在此添加更多验证逻辑，例如字段名是否有效等

        schemaDefinition.CreatedAt = DateTime.UtcNow;
        schemaDefinition.UpdatedAt = DateTime.UtcNow;

        var newId = _schemaCollection.Insert(schemaDefinition);
        return Task.FromResult(newId.AsInt32);
    }

    public Task<List<SchemaDefinition>> GetAllSchemasAsync()
    {
        var schemas = _schemaCollection.FindAll().ToList();
        return Task.FromResult(schemas);
    }

    public Task<SchemaDefinition?> GetSchemaByIdAsync(int id)
    {
        var schema = _schemaCollection.FindById(id);
        return Task.FromResult(schema);
    }

    public Task<SchemaDefinition?> GetSchemaByNameAsync(string name)
    {
        // 使用索引进行查找
        var schema = _schemaCollection.FindOne(x => x.Name == name);
        return Task.FromResult(schema);
    }

    public Task<bool> UpdateSchemaAsync(SchemaDefinition schemaDefinition)
    {
        // 确保更新时也检查名称冲突 (如果允许修改名称)
        if (_schemaCollection.Exists(x => x.Name == schemaDefinition.Name && x.Id != schemaDefinition.Id))
        {
            throw new InvalidOperationException($"Another schema with name '{schemaDefinition.Name}' already exists.");
        }

        schemaDefinition.UpdatedAt = DateTime.UtcNow;
        var updated = _schemaCollection.Update(schemaDefinition);
        return Task.FromResult(updated);
    }

    public Task<bool> DeleteSchemaAsync(int id)
    {
        // 重要：实际应用中，删除 Schema 前需要考虑如何处理关联的数据。
        // 策略可能包括：
        // 1. 禁止删除包含数据的 Schema。
        // 2. 归档 Schema 而不是物理删除。
        // 3. 删除 Schema 的同时删除其所有数据实例 (危险！)。
        // 此处仅实现简单删除定义。
        var schema = _schemaCollection.FindById(id);
        if (schema != null)
        {
            // 可在此加入检查，如果 GetDataCollection(schema.Name).Count() > 0 则阻止删除或提示
        }

        var deleted = _schemaCollection.Delete(id);
        return Task.FromResult(deleted);
    }
} 