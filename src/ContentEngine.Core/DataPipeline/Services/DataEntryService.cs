using ContentEngine.Core.DataPipeline.Models;
using ContentEngine.Core.Storage;
using LiteDB;

// Needed for SchemaDefinition if used in validation

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 管理具体数据实例 (BsonDocument) 的服务实现
/// </summary>
public class DataEntryService : IDataEntryService
{
    private readonly LiteDbContext _liteDbContext;
    private readonly ISchemaDefinitionService _schemaDefinitionService; // Inject to validate schema existence

    public DataEntryService(LiteDbContext liteDbContext, ISchemaDefinitionService schemaDefinitionService)
    {
        _liteDbContext = liteDbContext;
        _schemaDefinitionService = schemaDefinitionService;
    }

    public async Task<BsonValue> CreateDataAsync(string schemaName, BsonDocument data)
    {
        // 1. 验证 Schema 是否存在
        var schema = await ValidateSchemaExistsAsync(schemaName);

        // 2. (可选) 根据 Schema 定义验证输入数据 data 的结构和类型
        //    这部分逻辑可以比较复杂，暂时省略。可以在此添加一个 ValidateDataAgainstSchema 方法。

        // 3. 获取数据集合并插入
        var collection = _liteDbContext.GetDataCollection(schemaName);
        var newId = collection.Insert(data);
        return newId;
    }

    public async Task<BsonDocument?> GetDataByIdAsync(string schemaName, BsonValue id)
    {
        await ValidateSchemaExistsAsync(schemaName); // 确保操作的 Schema 有效
        var collection = _liteDbContext.GetDataCollection(schemaName);
        var data = collection.FindById(id);
        return data;
    }

    public async Task<List<BsonDocument>> GetDataAsync(string schemaName, int skip = 0, int limit = 50)
    {
        await ValidateSchemaExistsAsync(schemaName);
        var collection = _liteDbContext.GetDataCollection(schemaName);
        // LiteDB 的 Find 支持 Skip 和 Limit
        var dataList = collection.Find(Query.All(), skip, limit).ToList();
        return dataList;
    }

    public async Task<long> CountDataAsync(string schemaName)
    {
        await ValidateSchemaExistsAsync(schemaName);
        var collection = _liteDbContext.GetDataCollection(schemaName);
        var count = collection.LongCount(); // 使用 LongCount 获取总数
        return count;
    }

    public async Task<bool> UpdateDataAsync(string schemaName, BsonValue id, BsonDocument data)
    {
        await ValidateSchemaExistsAsync(schemaName);
        var collection = _liteDbContext.GetDataCollection(schemaName);

        // 1. (可选) 验证数据结构

        // 2. LiteDB 的 Update 会替换整个文档，需要确保传入的 BsonDocument 包含 _id
        data["_id"] = id; // 确保 ID 存在于要更新的文档中
        var updated = collection.Update(data);
        return updated;
    }

    public async Task<bool> DeleteDataAsync(string schemaName, BsonValue id)
    {
        await ValidateSchemaExistsAsync(schemaName);
        var collection = _liteDbContext.GetDataCollection(schemaName);
        var deleted = collection.Delete(id);
        return deleted;
    }

    /// <summary>
    /// 辅助方法：验证指定的 Schema 名称是否存在
    /// </summary>
    /// <exception cref="InvalidOperationException">如果 Schema 不存在则抛出</exception>
    private async Task<SchemaDefinition> ValidateSchemaExistsAsync(string schemaName)
    {
        var schema = await _schemaDefinitionService.GetSchemaByNameAsync(schemaName);
        if (schema == null)
        {
            throw new InvalidOperationException($"Schema with name '{schemaName}' does not exist.");
        }
        return schema; // 返回 Schema 定义，以便将来可能的验证
    }
} 