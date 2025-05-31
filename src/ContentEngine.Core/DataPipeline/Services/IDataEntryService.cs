using LiteDB;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 定义管理具体数据实例 (BsonDocument) 的服务接口
/// </summary>
public interface IDataEntryService
{
    /// <summary>
    /// 根据指定的 Schema 创建一个新的数据实例
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <param name="data">要存储的数据 (BsonDocument)</param>
    /// <returns>新创建数据实例的 LiteDB ID (BsonValue)</returns>
    /// <exception cref="ArgumentException">当 Schema 名称无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当 Schema 定义不存在时抛出</exception>
    Task<BsonValue> CreateDataAsync(string schemaName, BsonDocument data);

    /// <summary>
    /// 根据 Schema 名称和 ID 获取单个数据实例
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <param name="id">数据实例的 LiteDB ID</param>
    /// <returns>找到的数据实例 (BsonDocument)，如果不存在则返回 null</returns>
    Task<BsonDocument?> GetDataByIdAsync(string schemaName, BsonValue id);

    /// <summary>
    /// 根据 Schema 名称获取数据实例列表 (支持分页)
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <param name="skip">跳过的记录数 (用于分页)</param>
    /// <param name="limit">返回的最大记录数 (用于分页)</param>
    /// <returns>数据实例列表</returns>
    Task<List<BsonDocument>> GetDataAsync(string schemaName, int skip = 0, int limit = 50);


    /// <summary>
    /// 根据 Schema 名称获取数据实例列表 (支持分页和 LiteDB 筛选表达式)
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <param name="skip">跳过的记录数 (用于分页)</param>
    /// <param name="limit">返回的最大记录数 (用于分页)</param>
    /// <param name="filterExpression">LiteDB 筛选表达式字符串</param>
    /// <returns>数据实例列表</returns>
    Task<List<BsonDocument>> GetDataWithFilterAsync(string schemaName, int skip, int limit, string? filterExpression);

    /// <summary>
    /// 更新一个已存在的数据实例
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <param name="id">要更新的数据实例的 LiteDB ID</param>
    /// <param name="data">包含更新信息的数据 (BsonDocument)</param>
    /// <returns>更新是否成功</returns>
    Task<bool> UpdateDataAsync(string schemaName, BsonValue id, BsonDocument data);

    /// <summary>
    /// 根据 Schema 名称和 ID 删除一个数据实例
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <param name="id">要删除的数据实例的 LiteDB ID</param>
    /// <returns>删除是否成功</returns>
    Task<bool> DeleteDataAsync(string schemaName, BsonValue id);

    /// <summary>
    /// 获取指定 Schema 的数据实例总数
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <returns>数据实例数量</returns>
    Task<long> CountDataAsync(string schemaName);

    /// <summary>
    /// 获取指定 Schema 的数据实例总数 (支持 LiteDB 筛选表达式)
    /// </summary>
    /// <param name="schemaName">目标 Schema 的名称</param>
    /// <param name="filterExpression">LiteDB 筛选表达式字符串</param>
    /// <returns>数据实例数量</returns>
    Task<long> CountDataWithFilterAsync(string schemaName, string? filterExpression);
} 