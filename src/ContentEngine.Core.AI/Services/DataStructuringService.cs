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
                _logger.LogInformation("开始处理数据源: {SourceName} ({SourceType})", dataSource.Name, dataSource.Type);

                // 构建提取提示词
                var prompt = BuildExtractionPrompt(schema, dataSource, extractionMode, fieldMappings);
                
                // 调用AI进行数据提取
                var extractedData = await CallAIForExtractionAsync(prompt, cancellationToken);
                
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

    private string BuildExtractionPrompt(
        SchemaDefinition schema,
        DataSource dataSource,
        ExtractionMode extractionMode,
        Dictionary<string, string> fieldMappings)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("你是一个专业的数据提取专家。请从以下内容中提取结构化数据。");
        prompt.AppendLine();

        // Schema信息
        prompt.AppendLine($"目标数据结构: {schema.Name}");
        prompt.AppendLine($"描述: {schema.Description}");
        prompt.AppendLine();

        // 字段定义
        prompt.AppendLine("字段定义:");
        foreach (var field in schema.Fields)
        {
            prompt.AppendLine($"- {field.Name} ({field.Type}): {field.Comment ?? "无描述"}");
            if (field.IsRequired)
            {
                prompt.AppendLine("  [必填]");
            }
        }
        prompt.AppendLine();

        // 提取模式
        if (extractionMode == ExtractionMode.OneToOne)
        {
            prompt.AppendLine("提取模式: 一对一 - 从整个数据源提取一条记录");
        }
        else
        {
            prompt.AppendLine("提取模式: 批量 - 从数据源中提取所有可能的记录");
        }
        prompt.AppendLine();

        // 字段映射指令
        if (fieldMappings.ContainsKey("_autoMapping"))
        {
            prompt.AppendLine("特殊指令:");
            prompt.AppendLine(fieldMappings["_autoMapping"]);
            prompt.AppendLine();
        }
        else if (fieldMappings.Any())
        {
            prompt.AppendLine("字段映射指令:");
            foreach (var mapping in fieldMappings)
            {
                prompt.AppendLine($"- {mapping.Key}: {mapping.Value}");
            }
            prompt.AppendLine();
        }

        // 输出格式要求
        prompt.AppendLine("请以JSON格式返回提取结果，格式如下:");
        if (extractionMode == ExtractionMode.OneToOne)
        {
            prompt.AppendLine("{");
            foreach (var field in schema.Fields)
            {
                prompt.AppendLine($"  \"{field.Name}\": \"提取的值\",");
            }
            prompt.AppendLine("}");
        }
        else
        {
            prompt.AppendLine("[");
            prompt.AppendLine("  {");
            foreach (var field in schema.Fields)
            {
                prompt.AppendLine($"    \"{field.Name}\": \"提取的值\",");
            }
            prompt.AppendLine("  },");
            prompt.AppendLine("  // 更多记录...");
            prompt.AppendLine("]");
        }
        prompt.AppendLine();

        // 数据源内容
        prompt.AppendLine("要处理的数据内容:");
        prompt.AppendLine("---");
        prompt.AppendLine(dataSource.Content);
        prompt.AppendLine("---");

        return prompt.ToString();
    }

    private async Task<string> CallAIForExtractionAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            // 使用默认的Agent配置，或者可以配置专门的数据提取Agent
            var kernel = await _kernelFactory.BuildKernelAsync("ContentEngineHelper");
            if (kernel == null)
            {
                throw new InvalidOperationException("无法创建AI Kernel");
            }

            var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            return result.GetValue<string>() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI提取调用失败");
            throw new InvalidOperationException("AI数据提取失败", ex);
        }
    }

    private List<BsonDocument> ParseExtractionResult(string aiResponse, SchemaDefinition schema)
    {
        var records = new List<BsonDocument>();

        try
        {
            // 清理AI响应，移除可能的markdown代码块标记
            var cleanResponse = aiResponse.Trim();
            if (cleanResponse.StartsWith("```json"))
            {
                cleanResponse = cleanResponse.Substring(7);
            }
            if (cleanResponse.EndsWith("```"))
            {
                cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
            }
            cleanResponse = cleanResponse.Trim();

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
            _logger.LogError(ex, "解析AI响应JSON失败: {Response}", aiResponse);
            throw new InvalidOperationException("AI返回的数据格式无效", ex);
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