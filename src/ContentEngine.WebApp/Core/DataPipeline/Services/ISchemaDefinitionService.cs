using ContentEngine.WebApp.Core.DataPipeline.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContentEngine.WebApp.Core.DataPipeline.Services;

/// <summary>
/// 定义管理 SchemaDefinition 的服务接口
/// </summary>
public interface ISchemaDefinitionService
{
    /// <summary>
    /// 创建一个新的 Schema 定义
    /// </summary>
    /// <param name="schemaDefinition">要创建的 Schema 对象</param>
    /// <returns>创建后的 Schema 的 ID</returns>
    Task<int> CreateSchemaAsync(SchemaDefinition schemaDefinition);

    /// <summary>
    /// 获取所有 Schema 定义
    /// </summary>
    /// <returns>Schema 定义列表</returns>
    Task<List<SchemaDefinition>> GetAllSchemasAsync();

    /// <summary>
    /// 根据 ID 获取单个 Schema 定义
    /// </summary>
    /// <param name="id">Schema ID</param>
    /// <returns>找到的 Schema 定义，如果不存在则返回 null</returns>
    Task<SchemaDefinition?> GetSchemaByIdAsync(int id);

    /// <summary>
    /// 根据名称获取单个 Schema 定义
    /// </summary>
    /// <param name="name">Schema 名称</param>
    /// <returns>找到的 Schema 定义，如果不存在则返回 null</returns>
    Task<SchemaDefinition?> GetSchemaByNameAsync(string name);

    /// <summary>
    /// 更新一个已存在的 Schema 定义
    /// </summary>
    /// <param name="schemaDefinition">包含更新信息的 Schema 对象</param>
    /// <returns>更新是否成功</returns>
    Task<bool> UpdateSchemaAsync(SchemaDefinition schemaDefinition);

    /// <summary>
    /// 根据 ID 删除一个 Schema 定义
    /// </summary>
    /// <remarks>
    /// 注意：删除 Schema 可能需要考虑其关联的数据实例如何处理。
    /// 这里简单实现删除定义本身。
    /// </remarks>
    /// <param name="id">要删除的 Schema ID</param>
    /// <returns>删除是否成功</returns>
    Task<bool> DeleteSchemaAsync(int id);
} 