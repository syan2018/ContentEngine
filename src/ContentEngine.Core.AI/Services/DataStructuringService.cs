using ContentEngine.Core.DataPipeline.Models;
using ConfigurableAIProvider.Services.Factories;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text;
using System.Text.Json;

namespace ContentEngine.Core.AI.Services;

/// <summary>
/// 数据结构化服务实现
/// </summary>
public class DataStructuringService : IDataStructuringService
{
    private readonly IAIKernelFactory _kernelFactory;
    private readonly ILogger<DataStructuringService> _logger;

    public DataStructuringService(IAIKernelFactory kernelFactory, ILogger<DataStructuringService> logger)
    {
        _kernelFactory = kernelFactory;
        _logger = logger;
    }

    public async Task<List<ExtractionResult>> ExtractDataAsync(
        SchemaDefinition schema,
        List<DataSource> dataSources,
        ExtractionMode extractionMode,
        Dictionary<string, string> fieldMappings,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ExtractionResult>();

        foreach (var dataSource in dataSources)
        {
            var result = new ExtractionResult
            {
                SourceId = dataSource.Id,
                Status = ExtractionStatus.Processing,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("开始处理数据源: {SourceName} ({SourceType}), 提取模式: {ExtractionMode}", dataSource.Name, dataSource.Type, extractionMode);

                // 使用语义插件进行数据提取
                var extractedData = await CallAIForExtractionAsync(schema, dataSource, extractionMode, fieldMappings, cancellationToken);
                
                // 保存原始输出用于调试
                result.RawAIOutput = extractedData;
                
                // 解析AI返回的结果
                var records = ParseExtractionResult(extractedData, schema);
                
                result.Records = records;
                result.Status = ExtractionStatus.Success;
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("成功提取 {RecordCount} 条记录从数据源: {SourceName}", records.Count, dataSource.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理数据源失败: {SourceName}", dataSource.Name);
                result.Status = ExtractionStatus.Error;
                result.Error = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            results.Add(result);
        }

        return results;
    }

    public async Task<List<ValidationResult>> ValidateRecordsAsync(SchemaDefinition schema, List<BsonDocument> records)
    {
        var results = new List<ValidationResult>();

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var validation = new ValidationResult
            {
                RecordIndex = i,
                IsValid = true
            };

            // 验证必填字段
            foreach (var field in schema.Fields.Where(f => f.IsRequired))
            {
                if (!record.ContainsKey(field.Name) || record[field.Name] == null || record[field.Name].IsNull)
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"缺少必填字段: {field.Name}");
                }
            }

            // 验证字段类型
            foreach (var field in schema.Fields)
            {
                if (record.ContainsKey(field.Name) && !record[field.Name].IsNull)
                {
                    var value = record[field.Name];
                    var isValidType = field.Type switch
                    {
                        FieldType.Text => value.IsString,
                        FieldType.Number => value.IsInt32 || value.IsInt64 || value.IsDouble,
                        FieldType.Boolean => value.IsBoolean,
                        FieldType.Date => value.IsDateTime || (value.IsString && DateTime.TryParse(value.AsString, out _)),
                        _ => true
                    };

                    if (!isValidType)
                    {
                        validation.Warnings.Add($"字段 {field.Name} 的类型可能不正确");
                    }
                }
            }

            results.Add(validation);
        }

        return results;
    }

    public async Task<List<BsonDocument>> ParseRawOutput(string rawOutput, SchemaDefinition schema)
    {
        return await Task.FromResult(ParseExtractionResult(rawOutput, schema));
    }

    private async Task<string> CallAIForExtractionAsync(
        SchemaDefinition schema,
        DataSource dataSource,
        ExtractionMode extractionMode,
        Dictionary<string, string> fieldMappings,
        CancellationToken cancellationToken)
    {
        try
        {
            // 使用 ContentEngineHelper Agent 和 DataStructuring 插件
            var kernel = await _kernelFactory.BuildKernelAsync("ContentEngineHelper");
            if (kernel == null)
            {
                throw new InvalidOperationException("无法创建AI Kernel");
            }

            // 构建字段定义文本
            var fieldDefinitions = BuildFieldDefinitionsText(schema.Fields);
            
            // 构建字段映射指令
            var fieldMappingsText = BuildFieldMappingsText(fieldMappings);
            if (string.IsNullOrEmpty(fieldMappingsText))
            {
                fieldMappingsText = "无特殊映射指令，按字段名称直接匹配。";
            }
            
            // 构建输出字段格式
            var outputFields = BuildOutputFieldsFormat(schema.Fields);

            // 准备参数
            var arguments = new KernelArguments
            {
                ["schemaName"] = schema.Name,
                ["schemaDescription"] = schema.Description ?? "无描述",
                ["fieldDefinitions"] = fieldDefinitions,
                ["fieldMappings"] = fieldMappingsText,
                ["outputFields"] = outputFields,
                ["dataContent"] = dataSource.Content
            };

            // 根据提取模式选择对应的语义函数
            var functionName = extractionMode == ExtractionMode.OneToOne 
                ? "ExtractSingleRecord" 
                : "ExtractMultipleRecords";
            
            _logger.LogInformation("使用语义函数: {FunctionName}, 提取模式: {ExtractionMode}", functionName, extractionMode);
            
            var function = kernel.Plugins["DataStructuring"][functionName];
            var result = await kernel.InvokeAsync(function, arguments, cancellationToken);
            
            return result.GetValue<string>() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI提取调用失败");
            throw new InvalidOperationException("AI数据提取失败", ex);
        }
    }

    /// <summary>
    /// 构建字段定义文本
    /// </summary>
    private string BuildFieldDefinitionsText(List<FieldDefinition> fields)
    {
        var sb = new StringBuilder();
        foreach (var field in fields)
        {
            sb.AppendLine($"- {field.Name} ({field.Type}): {field.Comment ?? "无描述"}");
            if (field.IsRequired)
            {
                sb.AppendLine("  [必填]");
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 构建字段映射指令文本
    /// </summary>
    private string BuildFieldMappingsText(Dictionary<string, string> fieldMappings)
    {
        if (!fieldMappings.Any())
            return string.Empty;

        var sb = new StringBuilder();
        if (fieldMappings.ContainsKey("_autoMapping"))
        {
            sb.AppendLine("特殊指令:");
            sb.AppendLine(fieldMappings["_autoMapping"]);
        }
        else
        {
            foreach (var mapping in fieldMappings)
            {
                sb.AppendLine($"- {mapping.Key}: {mapping.Value}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 构建输出字段格式
    /// </summary>
    private string BuildOutputFieldsFormat(List<FieldDefinition> fields)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var comma = i < fields.Count - 1 ? "," : "";
            sb.AppendLine($"    \"{field.Name}\": \"提取的值\"{comma}");
        }
        return sb.ToString().TrimEnd();
    }

    private List<BsonDocument> ParseExtractionResult(string aiResponse, SchemaDefinition schema)
    {
        var records = new List<BsonDocument>();
        var cleanResponse = string.Empty;

        try
        {
            // 清理AI响应，移除可能的markdown代码块标记
            cleanResponse = aiResponse.Trim();
            
            // 移除各种可能的代码块标记
            if (cleanResponse.StartsWith("```json"))
            {
                cleanResponse = cleanResponse.Substring(7);
            }
            else if (cleanResponse.StartsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(3);
            }
            
            if (cleanResponse.EndsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
            }
            
            cleanResponse = cleanResponse.Trim();
            
            // 记录清理后的响应用于调试
            _logger.LogDebug("清理后的AI响应: {CleanResponse}", cleanResponse);

            // 尝试解析JSON
            using var document = JsonDocument.Parse(cleanResponse);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                // 批量模式 - 数组格式
                foreach (var item in root.EnumerateArray())
                {
                    var record = ConvertJsonElementToBsonDocument(item, schema);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // 单条记录模式
                var record = ConvertJsonElementToBsonDocument(root, schema);
                if (record != null)
                {
                    records.Add(record);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析AI响应JSON失败。原始响应: {Response}, 清理后响应: {CleanResponse}", aiResponse, cleanResponse);
            throw new InvalidOperationException($"AI返回的数据格式无效: {ex.Message}。原始响应: {aiResponse}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析AI响应时发生未知错误。原始响应: {Response}", aiResponse);
            throw new InvalidOperationException($"解析AI响应时发生错误: {ex.Message}", ex);
        }

        return records;
    }

    private BsonDocument? ConvertJsonElementToBsonDocument(JsonElement element, SchemaDefinition schema)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var document = new BsonDocument();

        foreach (var field in schema.Fields)
        {
            if (element.TryGetProperty(field.Name, out var property))
            {
                var bsonValue = ConvertJsonElementToBsonValue(property, field.Type);
                if (bsonValue != null)
                {
                    document[field.Name] = bsonValue;
                }
            }
        }

        return document;
    }

    private BsonValue? ConvertJsonElementToBsonValue(JsonElement element, FieldType fieldType)
    {
        try
        {
            return fieldType switch
            {
                FieldType.Text => element.ValueKind == JsonValueKind.String ? new BsonValue(element.GetString()) : null,
                FieldType.Number => element.ValueKind == JsonValueKind.Number ? 
                    (element.TryGetInt32(out var intVal) ? new BsonValue(intVal) : new BsonValue(element.GetDouble())) : null,
                FieldType.Boolean => element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False ? 
                    new BsonValue(element.GetBoolean()) : null,
                FieldType.Date => element.ValueKind == JsonValueKind.String && DateTime.TryParse(element.GetString(), out var date) ? 
                    new BsonValue(date) : null,
                _ => element.ValueKind == JsonValueKind.String ? new BsonValue(element.GetString()) : null
            };
        }
        catch
        {
            return null;
        }
    }
} 