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

    public async Task<long> CountDataWithFilterAsync(string schemaName, string? filterExpression)
    {
        await ValidateSchemaExistsAsync(schemaName);
        var collection = _liteDbContext.GetDataCollection(schemaName);
        
        if (string.IsNullOrWhiteSpace(filterExpression))
        {
            var count = collection.LongCount();
            return count;
        }
        else
        {
            var count = collection.Query().Where(filterExpression).LongCount();
            return count;
        }
    }

    public async Task<List<BsonDocument>> GetDataWithFilterAsync(string schemaName, int skip, int limit, string? filterExpression)
    {
        await ValidateSchemaExistsAsync(schemaName);
        var collection = _liteDbContext.GetDataCollection(schemaName);
        
        if (string.IsNullOrWhiteSpace(filterExpression))
        {
            var dataList = collection.Query().Skip(skip).Limit(limit).ToList();
            return dataList;
        }
        else
        {
            var queryResult = collection.Query().Where(filterExpression).Skip(skip).Limit(limit).ToList();
            return queryResult;
        }
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

    /// <summary>
    /// 将 BsonDocument 查询条件转换为 LiteDB BsonExpression
    /// </summary>
    private BsonExpression ConvertBsonDocumentToQuery(BsonDocument queryDoc)
    {
        // 处理 $and 操作
        if (queryDoc.ContainsKey("$and"))
        {
            var andConditions = queryDoc["$and"].AsArray;
            var expressions = new List<string>();
            
            foreach (var condition in andConditions)
            {
                var expr = ConvertBsonDocumentToQuery(condition.AsDocument);
                expressions.Add($"({expr})");
            }
            
            return BsonExpression.Create(string.Join(" AND ", expressions));
        }
        
        // 处理 $or 操作
        if (queryDoc.ContainsKey("$or"))
        {
            var orConditions = queryDoc["$or"].AsArray;
            var expressions = new List<string>();
            
            foreach (var condition in orConditions)
            {
                var expr = ConvertBsonDocumentToQuery(condition.AsDocument);
                expressions.Add($"({expr})");
            }
            
            return BsonExpression.Create(string.Join(" OR ", expressions));
        }
        
        // 处理单个字段条件
        var firstKey = queryDoc.Keys.First();
        var value = queryDoc[firstKey];
        
        if (value.IsDocument)
        {
            var valueDoc = value.AsDocument;
            
            // 处理正则表达式
            if (valueDoc.ContainsKey("$regex"))
            {
                var pattern = valueDoc["$regex"].AsString;
                return BsonExpression.Create($"{firstKey} LIKE '%{pattern}%'");
            }
            
            // 处理大于
            if (valueDoc.ContainsKey("$gt"))
            {
                var gtValue = valueDoc["$gt"];
                return BsonExpression.Create($"{firstKey} > {FormatValue(gtValue)}");
            }
            
            // 处理小于
            if (valueDoc.ContainsKey("$lt"))
            {
                var ltValue = valueDoc["$lt"];
                return BsonExpression.Create($"{firstKey} < {FormatValue(ltValue)}");
            }
            
            // 处理大于等于
            if (valueDoc.ContainsKey("$gte"))
            {
                var gteValue = valueDoc["$gte"];
                return BsonExpression.Create($"{firstKey} >= {FormatValue(gteValue)}");
            }
            
            // 处理小于等于
            if (valueDoc.ContainsKey("$lte"))
            {
                var lteValue = valueDoc["$lte"];
                return BsonExpression.Create($"{firstKey} <= {FormatValue(lteValue)}");
            }
        }
        
        // 处理等于条件
        return BsonExpression.Create($"{firstKey} = {FormatValue(value)}");
    }

    /// <summary>
    /// 格式化 BsonValue 为查询字符串
    /// </summary>
    private string FormatValue(BsonValue value)
    {
        if (value.IsString)
        {
            return $"'{value.AsString.Replace("'", "''")}'";
        }
        else if (value.IsDateTime)
        {
            return $"#{value.AsDateTime:yyyy-MM-dd HH:mm:ss}#";
        }
        else if (value.IsBoolean)
        {
            return value.AsBoolean ? "true" : "false";
        }
        else
        {
            return value.ToString();
        }
    }
} 