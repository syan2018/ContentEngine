using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Utils
{
    /// <summary>
    /// JSON 解析和清理工具类
    /// 用于处理 AI 输出的各种格式，尝试提取并解析有效的 JSON
    /// </summary>
    public static class JsonParsingUtils
    {
        private static ILogger? _logger;
        
        /// <summary>
        /// 解析结果
        /// </summary>
        public class ParseResult
        {
            /// <summary>
            /// 是否解析成功
            /// </summary>
            public bool IsSuccess { get; set; }

            /// <summary>
            /// 解析后的结构化数据
            /// </summary>
            public BsonDocument? StructuredData { get; set; }

            /// <summary>
            /// 错误信息（如果解析失败）
            /// </summary>
            public string? ErrorMessage { get; set; }

            /// <summary>
            /// 清理后的 JSON 字符串（用于调试）
            /// </summary>
            public string? CleanedJson { get; set; }
        }

        /// <summary>
        /// 尝试从 AI 输出中解析 JSON
        /// </summary>
        /// <param name="aiOutput">AI 原始输出</param>
        /// <param name="forceJsonMode">是否为强制 JSON 模式（如果强制模式下解析失败，将返回失败）</param>
        /// <returns>解析结果</returns>
        public static ParseResult TryParseJson(string? aiOutput, bool forceJsonMode = false)
        {
            var result = new ParseResult();

            if (string.IsNullOrWhiteSpace(aiOutput))
            {
                result.ErrorMessage = "AI 输出为空";
                return result;
            }

            try
            {
                // 步骤 1: 清理输出内容
                var cleanedJson = CleanAiOutput(aiOutput);
                result.CleanedJson = cleanedJson;

                if (string.IsNullOrWhiteSpace(cleanedJson))
                {
                    result.ErrorMessage = "清理后的内容为空";
                    return result;
                }

                // 步骤 2: 尝试解析为 JSON
                using var jsonDoc = JsonDocument.Parse(cleanedJson);
                var jsonElement = jsonDoc.RootElement;

                // 步骤 3: 转换为 BsonDocument
                result.StructuredData = ConvertJsonElementToBsonDocument(jsonElement);
                result.IsSuccess = true;

                return result;
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"JSON 解析失败: {ex.Message}";
                
                if (forceJsonMode)
                {
                    // 强制 JSON 模式下，解析失败即为错误
                    _logger?.LogWarning("强制 JSON 模式下解析失败: {Error}, 原文: {Original}", ex.Message, aiOutput);
                    return result;
                }

                // 非强制模式下，尝试更激进的清理
                try
                {
                    var aggressiveClean = AggressiveCleanJson(aiOutput);
                    using var jsonDoc = JsonDocument.Parse(aggressiveClean);
                    var jsonElement = jsonDoc.RootElement;
                    
                    result.StructuredData = ConvertJsonElementToBsonDocument(jsonElement);
                    result.CleanedJson = aggressiveClean;
                    result.IsSuccess = true;
                    result.ErrorMessage = null; // 清除错误信息
                    
                    return result;
                }
                catch (Exception aggressiveEx)
                {
                    result.ErrorMessage = $"激进清理也失败: {aggressiveEx.Message}";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"解析过程中发生未知错误: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 清理 AI 输出，去除常见的格式标记
        /// </summary>
        private static string CleanAiOutput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = input.Trim();

            // 1. 去除 Markdown 代码块标记
            text = RemoveMarkdownCodeBlocks(text);

            // 2. 去除前后的方括号包裹（如果是单个对象）
            text = RemoveArrayWrapping(text);

            // 3. 去除其他常见的前后缀
            text = RemoveCommonPrefixSuffix(text);

            return text.Trim();
        }

        /// <summary>
        /// 激进清理 JSON - 尝试提取任何可能的 JSON 片段
        /// </summary>
        private static string AggressiveCleanJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = input;

            // 1. 首先应用标准清理
            text = CleanAiOutput(text);

            // 2. 尝试提取第一个完整的 JSON 对象
            var objectMatch = Regex.Match(text, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", RegexOptions.Singleline);
            if (objectMatch.Success)
            {
                return objectMatch.Value;
            }

            // 3. 如果没有找到对象，尝试提取数组
            var arrayMatch = Regex.Match(text, @"\[[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*\]", RegexOptions.Singleline);
            if (arrayMatch.Success)
            {
                return arrayMatch.Value;
            }

            // 4. 如果以上都失败，返回原始清理结果
            return text;
        }

        /// <summary>
        /// 去除 Markdown 代码块标记
        /// </summary>
        private static string RemoveMarkdownCodeBlocks(string text)
        {
            // 去除 ```json 和 ``` 标记
            text = Regex.Replace(text, @"```json\s*", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"```\s*$", "", RegexOptions.Multiline);
            text = Regex.Replace(text, @"^```\s*", "", RegexOptions.Multiline);
            
            // 去除单行代码标记 `
            if (text.StartsWith("`") && text.EndsWith("`") && text.Count(c => c == '`') == 2)
            {
                text = text.Substring(1, text.Length - 2);
            }

            return text;
        }

        /// <summary>
        /// 去除数组包裹（如果内容是单个对象）
        /// </summary>
        private static string RemoveArrayWrapping(string text)
        {
            text = text.Trim();
            
            if (text.StartsWith("[") && text.EndsWith("]"))
            {
                var inner = text.Substring(1, text.Length - 2).Trim();
                
                // 检查内部是否为单个JSON对象
                if (inner.StartsWith("{") && inner.EndsWith("}"))
                {
                    // 简单检查是否为单个对象（计算大括号匹配）
                    var braceCount = 0;
                    var hasCommaOutsideObject = false;
                    
                    foreach (char c in inner)
                    {
                        if (c == '{') braceCount++;
                        else if (c == '}') braceCount--;
                        else if (c == ',' && braceCount == 0)
                        {
                            hasCommaOutsideObject = true;
                            break;
                        }
                    }
                    
                    // 如果没有在对象外部的逗号，说明是单个对象
                    if (!hasCommaOutsideObject)
                    {
                        return inner;
                    }
                }
            }
            
            return text;
        }

        /// <summary>
        /// 去除常见的前后缀
        /// </summary>
        private static string RemoveCommonPrefixSuffix(string text)
        {
            text = text.Trim();

            // 去除常见的解释性文字
            var cleanPatterns = new[]
            {
                @"^[Jj]son\s*[:：]\s*",
                @"^[Rr]esult\s*[:：]\s*",
                @"^[Oo]utput\s*[:：]\s*",
                @"^[Aa]nswer\s*[:：]\s*",
                @"^[Hh]ere\s+is\s+the\s+[Jj]son\s*[:：]?\s*",
                @"^[Tt]he\s+[Jj]son\s+is\s*[:：]?\s*",
            };

            foreach (var pattern in cleanPatterns)
            {
                text = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
            }

            return text.Trim();
        }

        /// <summary>
        /// 将 JsonElement 转换为 BsonDocument
        /// </summary>
        private static BsonDocument ConvertJsonElementToBsonDocument(JsonElement element)
        {
            var bsonDoc = new BsonDocument();

            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("只支持 JSON 对象转换为 BsonDocument");
            }

            foreach (var property in element.EnumerateObject())
            {
                var bsonValue = ConvertJsonElementToBsonValue(property.Value);
                bsonDoc[property.Name] = bsonValue;
            }

            return bsonDoc;
        }

        /// <summary>
        /// 将 JsonElement 转换为 BsonValue
        /// </summary>
        private static BsonValue ConvertJsonElementToBsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => new BsonValue(element.GetString() ?? string.Empty),
                JsonValueKind.Number => element.TryGetInt32(out var intVal) ? new BsonValue((int)intVal) :
                                       element.TryGetInt64(out var longVal) ? new BsonValue((long)longVal) :
                                       new BsonValue(element.GetDouble()),
                JsonValueKind.True => new BsonValue(true),
                JsonValueKind.False => new BsonValue(false),
                JsonValueKind.Null => BsonValue.Null,
                JsonValueKind.Object => ConvertJsonObjectToBsonDocument(element),
                JsonValueKind.Array => ConvertJsonArrayToBsonArray(element),
                _ => new BsonValue(element.GetRawText())
            };
        }

        /// <summary>
        /// 将 JSON 对象转换为 BsonDocument
        /// </summary>
        private static BsonValue ConvertJsonObjectToBsonDocument(JsonElement element)
        {
            var bsonDoc = new BsonDocument();
            foreach (var property in element.EnumerateObject())
            {
                var bsonValue = ConvertJsonElementToBsonValue(property.Value);
                bsonDoc[property.Name] = bsonValue;
            }
            return (BsonValue)bsonDoc; // 使用显式转换
        }

        /// <summary>
        /// 将 JSON 数组转换为 BsonArray
        /// </summary>
        private static BsonValue ConvertJsonArrayToBsonArray(JsonElement element)
        {
            var bsonArray = new BsonArray();
            foreach (var item in element.EnumerateArray())
            {
                var bsonValue = ConvertJsonElementToBsonValue(item);
                bsonArray.Add(bsonValue);
            }
            return (BsonValue)bsonArray; // 使用显式转换
        }

        /// <summary>
        /// 设置Logger实例
        /// </summary>
        /// <param name="logger">Logger实例</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }
    }
}
