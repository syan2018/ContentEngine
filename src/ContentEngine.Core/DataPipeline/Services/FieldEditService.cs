using ContentEngine.Core.DataPipeline.Models;
using ContentEngine.Core.Storage;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 字段编辑服务实现
/// </summary>
public class FieldEditService : IFieldEditService
{
    private readonly LiteDbContext _dbContext;
    private readonly ILogger<FieldEditService> _logger;

    public FieldEditService(LiteDbContext dbContext, ILogger<FieldEditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<FieldChangeAnalysis> AnalyzeFieldChangeAsync(
        FieldDefinition originalField, 
        FieldDefinition editedField, 
        string schemaName, 
        int recordCount)
    {
        var analysis = new FieldChangeAnalysis();
        var changes = new List<FieldChangeInfo>();

        // 分析字段名称变更
        if (originalField.Name != editedField.Name)
        {
            changes.Add(new FieldChangeInfo
            {
                Title = "字段名称变更",
                Icon = "edit",
                Description = $"字段名称将从 \"{originalField.Name}\" 更改为 \"{editedField.Name}\"",
                Risks = recordCount > 0 ? new[] { 
                    "需要更新所有既有数据记录的字段名", 
                    "可能影响引用此字段的其他Schema",
                    "字段重命名操作不可逆"
                } : Array.Empty<string>(),
                Actions = recordCount > 0 ? new[] { 
                    $"重命名 {recordCount} 条记录中的字段名",
                    "验证字段名称的唯一性",
                    "更新相关的引用关系"
                } : Array.Empty<string>()
            });
        }

        // 分析数据类型变更
        if (originalField.Type != editedField.Type)
        {
            var isCompatible = IsCompatibleTypeConversion(originalField.Type, editedField.Type);
            changes.Add(new FieldChangeInfo
            {
                Title = "数据类型变更",
                Icon = "transform",
                Description = $"数据类型将从 \"{GetFieldTypeDisplayName(originalField.Type)}\" 更改为 \"{GetFieldTypeDisplayName(editedField.Type)}\"",
                Risks = recordCount > 0 ? new[] { 
                    isCompatible ? "部分数据可能需要格式转换" : "不兼容的类型转换可能导致数据丢失", 
                    "转换失败的数据将被设置为默认值或null",
                    "可能影响数据查询和统计功能",
                    "类型转换可能影响应用程序的数据处理逻辑"
                } : Array.Empty<string>(),
                Actions = recordCount > 0 ? new[] { 
                    $"尝试转换 {recordCount} 条记录的数据类型",
                    "无法转换的数据将被标记并记录日志",
                    "生成转换报告以供审查",
                    "备份原始数据以防转换失败"
                } : Array.Empty<string>()
            });
        }

        // 分析必填属性变更
        if (originalField.IsRequired != editedField.IsRequired)
        {
            if (editedField.IsRequired && !originalField.IsRequired)
            {
                changes.Add(new FieldChangeInfo
                {
                    Title = "设置为必填字段",
                    Icon = "star",
                    Description = "字段将变更为必填项",
                    Risks = recordCount > 0 ? new[] { 
                        "既有记录中的空值将导致数据验证失败",
                        "可能需要为空值设置默认值",
                        "影响数据录入的验证规则"
                    } : Array.Empty<string>(),
                    Actions = recordCount > 0 ? new[] { 
                        "检查并处理既有记录中的空值", 
                        "为空值设置默认值或标记为无效",
                        "更新数据验证规则"
                    } : Array.Empty<string>()
                });
            }
            else
            {
                changes.Add(new FieldChangeInfo
                {
                    Title = "设置为可选字段",
                    Icon = "star_border",
                    Description = "字段将变更为可选项",
                    Risks = Array.Empty<string>(),
                    Actions = new[] { "更新数据验证规则以允许空值" }
                });
            }
        }

        // 分析引用Schema变更
        if (originalField.ReferenceSchemaName != editedField.ReferenceSchemaName)
        {
            changes.Add(new FieldChangeInfo
            {
                Title = "引用Schema变更",
                Icon = "link",
                Description = $"引用Schema将从 \"{originalField.ReferenceSchemaName ?? "无"}\" 更改为 \"{editedField.ReferenceSchemaName ?? "无"}\"",
                Risks = recordCount > 0 ? new[] { 
                    "既有的引用关系可能失效", 
                    "数据完整性约束可能被破坏",
                    "可能产生悬空引用"
                } : Array.Empty<string>(),
                Actions = recordCount > 0 ? new[] { 
                    "验证既有引用数据的有效性", 
                    "清理无效的引用关系",
                    "更新引用完整性约束"
                } : Array.Empty<string>()
            });
        }

        // 分析备注变更
        if (originalField.Comment != editedField.Comment)
        {
            changes.Add(new FieldChangeInfo
            {
                Title = "字段备注更新",
                Icon = "comment",
                Description = "字段备注信息已更新",
                Risks = Array.Empty<string>(),
                Actions = new[] { "更新字段文档和说明" }
            });
        }

        analysis.Changes = changes;
        
        // 评估风险等级
        analysis.IsHighRisk = recordCount > 0 && (
            originalField.Name != editedField.Name ||
            originalField.Type != editedField.Type ||
            (!originalField.IsRequired && editedField.IsRequired)
        );

        if (analysis.IsHighRisk)
        {
            analysis.RiskAssessment = $"此操作将影响 {recordCount} 条既有数据记录，可能导致数据丢失或不一致。建议在执行前备份数据。";
        }

        return await Task.FromResult(analysis);
    }

    public async Task<List<DataChangePreview>> GetDataChangePreviewAsync(
        FieldDefinition originalField,
        FieldDefinition editedField,
        string schemaName,
        int previewCount = 5)
    {
        var previews = new List<DataChangePreview>();

        try
        {
            var collection = _dbContext.GetDataCollection(schemaName);
            var documents = collection.FindAll().Take(previewCount).ToList();

            foreach (var doc in documents)
            {
                var recordId = doc["_id"]?.ToString() ?? "unknown";
                var currentValue = GetFieldValue(doc, originalField.Name);
                
                var preview = new DataChangePreview
                {
                    RecordId = recordId,
                    CurrentValue = FormatValue(currentValue, originalField.Type)
                };

                // 模拟数据转换
                var conversionResult = SimulateDataConversion(currentValue, originalField.Type, editedField.Type);
                preview.NewValue = conversionResult.Value;
                preview.Status = conversionResult.Status;
                preview.WillLoseData = conversionResult.Status == ConversionStatus.DataLoss || 
                                     conversionResult.Status == ConversionStatus.Failed;
                preview.ErrorMessage = conversionResult.ErrorMessage;

                previews.Add(preview);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据变更预览失败: SchemaName={SchemaName}, FieldName={FieldName}", 
                schemaName, originalField.Name);
        }

        return await Task.FromResult(previews);
    }

    public async Task<FieldChangeResult> ApplyFieldChangeAsync(
        FieldDefinition originalField,
        FieldDefinition editedField,
        string schemaName)
    {
        var result = new FieldChangeResult();
        var logs = new List<string>();

        try
        {
            var collection = _dbContext.GetDataCollection(schemaName);
            var documents = collection.FindAll().ToList();
            
            logs.Add($"开始处理 {documents.Count} 条记录");

            foreach (var doc in documents)
            {
                var recordId = doc["_id"]?.ToString() ?? "unknown";
                
                try
                {
                    // 处理字段名称变更
                    if (originalField.Name != editedField.Name)
                    {
                        if (doc.ContainsKey(originalField.Name))
                        {
                            var value = doc[originalField.Name];
                            doc.Remove(originalField.Name);
                            doc[editedField.Name] = value;
                            logs.Add($"记录 {recordId}: 字段重命名 {originalField.Name} -> {editedField.Name}");
                        }
                    }

                    // 处理数据类型转换
                    if (originalField.Type != editedField.Type)
                    {
                        var fieldName = editedField.Name;
                        if (doc.ContainsKey(fieldName))
                        {
                            var currentValue = doc[fieldName];
                            var conversionResult = ConvertValue(currentValue, originalField.Type, editedField.Type);
                            
                            if (conversionResult.Success)
                            {
                                doc[fieldName] = conversionResult.Value;
                                logs.Add($"记录 {recordId}: 类型转换成功 {originalField.Type} -> {editedField.Type}");
                            }
                            else
                            {
                                result.FailedConversions++;
                                logs.Add($"记录 {recordId}: 类型转换失败 - {conversionResult.ErrorMessage}");
                            }
                        }
                    }

                    // 更新文档
                    collection.Update(doc);
                    result.AffectedRecords++;
                }
                catch (Exception ex)
                {
                    result.FailedConversions++;
                    logs.Add($"记录 {recordId}: 处理失败 - {ex.Message}");
                    _logger.LogError(ex, "处理记录失败: RecordId={RecordId}", recordId);
                }
            }

            result.Success = true;
            logs.Add($"字段变更完成: 成功处理 {result.AffectedRecords} 条记录，失败 {result.FailedConversions} 条");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            logs.Add($"字段变更失败: {ex.Message}");
            _logger.LogError(ex, "执行字段变更失败: SchemaName={SchemaName}, FieldName={FieldName}", 
                schemaName, originalField.Name);
        }

        result.Logs = logs;
        return await Task.FromResult(result);
    }

    public bool IsCompatibleTypeConversion(FieldType fromType, FieldType toType)
    {
        // 定义兼容的类型转换矩阵
        return (fromType, toType) switch
        {
            // 相同类型总是兼容的
            (var from, var to) when from == to => true,
            
            // 转换为文本通常是安全的（除了相同类型已经处理过的情况）
            (_, FieldType.Text) => true,
            
            // 布尔值转换
            (FieldType.Boolean, FieldType.Number) => true, // true=1, false=0
            (FieldType.Number, FieldType.Boolean) => true, // 0=false, 其他=true
            
            // 日期转换（排除已处理的转换为文本的情况）
            (FieldType.Text, FieldType.Date) => false, // 文本转日期可能失败
            
            // 引用类型转换（排除已处理的转换为文本的情况）
            (FieldType.Text, FieldType.Reference) => false, // 文本转引用需要验证
            
            // 其他转换默认不兼容
            _ => false
        };
    }

    private BsonValue? GetFieldValue(BsonDocument document, string fieldName)
    {
        return document.ContainsKey(fieldName) ? document[fieldName] : null;
    }

    private string FormatValue(BsonValue? value, FieldType fieldType)
    {
        if (value == null || value.IsNull)
            return "null";

        return fieldType switch
        {
            FieldType.Text => $"\"{value.AsString}\"",
            FieldType.Number => value.IsDouble ? value.AsDouble.ToString("0.##") : value.ToString(),
            FieldType.Boolean => value.AsBoolean.ToString().ToLower(),
            FieldType.Date => $"\"{value.AsDateTime:yyyy-MM-dd}\"",
            FieldType.Reference => $"\"{value.AsString}\"",
            _ => value.ToString()
        };
    }

    private (string Value, ConversionStatus Status, string? ErrorMessage) SimulateDataConversion(
        BsonValue? currentValue, FieldType fromType, FieldType toType)
    {
        if (currentValue == null || currentValue.IsNull)
        {
            return ("null", ConversionStatus.Success, null);
        }

        try
        {
            var converted = ConvertValue(currentValue, fromType, toType);
            if (converted.Success)
            {
                return (FormatValue(converted.Value, toType), ConversionStatus.Success, null);
            }
            else
            {
                return ("转换失败", ConversionStatus.Failed, converted.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            return ("转换失败", ConversionStatus.Failed, ex.Message);
        }
    }

    private (bool Success, BsonValue? Value, string? ErrorMessage) ConvertValue(
        BsonValue currentValue, FieldType fromType, FieldType toType)
    {
        try
        {
            if (currentValue.IsNull)
                return (true, BsonValue.Null, null);

            return (fromType, toType) switch
            {
                // 相同类型
                (var from, var to) when from == to => (true, currentValue, null),
                
                // 转换为文本（排除相同类型的情况）
                (FieldType.Number, FieldType.Text) => (true, new BsonValue(currentValue.IsDouble ? currentValue.AsDouble.ToString("0.##") : currentValue.ToString()), null),
                (_, FieldType.Text) => (true, new BsonValue(currentValue.ToString()), null),
                
                // 数字转换
                (FieldType.Text, FieldType.Number) => TryParseNumber(currentValue.AsString),
                (FieldType.Boolean, FieldType.Number) => (true, new BsonValue(currentValue.AsBoolean ? 1 : 0), null),
                
                // 布尔转换
                (FieldType.Number, FieldType.Boolean) => (true, new BsonValue(currentValue.AsDouble != 0), null),
                (FieldType.Text, FieldType.Boolean) => TryParseBoolean(currentValue.AsString),
                
                // 日期转换
                (FieldType.Text, FieldType.Date) => TryParseDate(currentValue.AsString),
                
                // 引用转换
                (FieldType.Text, FieldType.Reference) => (true, currentValue, null),
                
                // 不支持的转换
                _ => (false, null, $"不支持从 {fromType} 转换为 {toType}")
            };
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private (bool Success, BsonValue? Value, string? ErrorMessage) TryParseNumber(string text)
    {
        if (double.TryParse(text, out var number))
            return (true, new BsonValue(number), null);
        return (false, null, $"无法将 '{text}' 转换为数字");
    }

    private (bool Success, BsonValue? Value, string? ErrorMessage) TryParseBoolean(string text)
    {
        if (bool.TryParse(text, out var boolean))
            return (true, new BsonValue(boolean), null);
        if (text.ToLower() is "1" or "true" or "是" or "yes")
            return (true, new BsonValue(true), null);
        if (text.ToLower() is "0" or "false" or "否" or "no")
            return (true, new BsonValue(false), null);
        return (false, null, $"无法将 '{text}' 转换为布尔值");
    }

    private (bool Success, BsonValue? Value, string? ErrorMessage) TryParseDate(string text)
    {
        if (DateTime.TryParse(text, out var date))
            return (true, new BsonValue(date), null);
        return (false, null, $"无法将 '{text}' 转换为日期");
    }

    private string GetFieldTypeDisplayName(FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Text => "文本",
            FieldType.Number => "数值",
            FieldType.Boolean => "布尔",
            FieldType.Date => "日期",
            FieldType.Reference => "引用",
            _ => "未知"
        };
    }
} 