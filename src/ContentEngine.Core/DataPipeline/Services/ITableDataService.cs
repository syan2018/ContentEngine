using Microsoft.AspNetCore.Components.Forms;
using ContentEngine.Core.DataPipeline.Models;
using LiteDB;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 表格数据读取服务接口
/// </summary>
public interface ITableDataService
{
    /// <summary>
    /// 读取Excel文件数据
    /// </summary>
    /// <param name="file">Excel文件</param>
    /// <param name="sheetIndex">工作表索引（默认第一个）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表格数据</returns>
    Task<TableImportData> ReadExcelAsync(IBrowserFile file, int sheetIndex = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取CSV文件数据
    /// </summary>
    /// <param name="file">CSV文件</param>
    /// <param name="encoding">文件编码（默认UTF-8）</param>
    /// <param name="delimiter">分隔符（默认逗号）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表格数据</returns>
    Task<TableImportData> ReadCsvAsync(IBrowserFile file, string encoding = "UTF-8", char delimiter = ',', CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取Excel文件的所有工作表名称
    /// </summary>
    /// <param name="file">Excel文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工作表名称列表</returns>
    Task<List<string>> GetExcelSheetNamesAsync(IBrowserFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查文件是否为支持的表格文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="mimeType">MIME类型</param>
    /// <returns>是否支持</returns>
    bool IsTableFileSupported(string fileName, string mimeType);

    /// <summary>
    /// 根据映射配置将表格数据转换为BsonDocument列表
    /// </summary>
    /// <param name="tableData">表格数据</param>
    /// <param name="schema">数据结构定义</param>
    /// <param name="config">导入配置</param>
    /// <returns>转换后的BsonDocument列表</returns>
    Task<List<BsonDocument>> ConvertTableDataToBsonAsync(TableImportData tableData, SchemaDefinition schema, TableImportConfig config);

    /// <summary>
    /// 自动推荐字段映射
    /// </summary>
    /// <param name="tableHeaders">表格表头</param>
    /// <param name="schema">数据结构定义</param>
    /// <returns>推荐的字段映射</returns>
    List<FieldMapping> SuggestFieldMappings(List<string> tableHeaders, SchemaDefinition schema);
} 