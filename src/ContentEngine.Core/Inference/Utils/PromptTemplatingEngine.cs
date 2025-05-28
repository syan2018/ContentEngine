using LiteDB;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ContentEngine.Core.Inference.Utils
{
    /// <summary>
    /// Prompt模板引擎，用于将数据填充到模板中
    /// </summary>
    public static class PromptTemplatingEngine
    {
        /// <summary>
        /// 模板占位符的正则表达式模式: {{ViewName.FieldName}}
        /// </summary>
        private static readonly Regex PlaceholderRegex = new(@"\{\{([^.}]+)\.([^}]+)\}\}", RegexOptions.Compiled);

        /// <summary>
        /// 将数据映射填充到模板中
        /// </summary>
        /// <param name="templateContent">模板内容</param>
        /// <param name="dataMap">数据映射，Key为视图名称，Value为该视图的BsonDocument</param>
        /// <returns>填充后的字符串</returns>
        public static string Fill(string templateContent, Dictionary<string, BsonDocument> dataMap)
        {
            if (string.IsNullOrEmpty(templateContent))
                return string.Empty;

            if (dataMap == null || dataMap.Count == 0)
                return templateContent;

            return PlaceholderRegex.Replace(templateContent, match =>
            {
                var viewName = match.Groups[1].Value.Trim();
                var fieldName = match.Groups[2].Value.Trim();

                // 检查视图是否存在
                if (!dataMap.TryGetValue(viewName, out var viewData))
                {
                    return $"[视图未找到: {viewName}]";
                }

                // 获取字段值
                var fieldValue = GetFieldValue(viewData, fieldName);
                return fieldValue ?? $"[字段未找到: {viewName}.{fieldName}]";
            });
        }

        /// <summary>
        /// 从BsonDocument中获取字段值
        /// </summary>
        /// <param name="document">BSON文档</param>
        /// <param name="fieldPath">字段路径，支持嵌套访问，如 "attributes.strength"</param>
        /// <returns>字段值的字符串表示</returns>
        private static string? GetFieldValue(BsonDocument document, string fieldPath)
        {
            try
            {
                // 支持嵌套字段访问
                var pathParts = fieldPath.Split('.');
                BsonValue currentValue = document;

                foreach (var part in pathParts)
                {
                    if (currentValue.IsDocument)
                    {
                        var doc = currentValue.AsDocument;
                        if (doc.TryGetValue(part, out var value))
                        {
                            currentValue = value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                return FormatBsonValue(currentValue);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 格式化BsonValue为用户友好的字符串
        /// </summary>
        /// <param name="value">BSON值</param>
        /// <returns>格式化后的字符串</returns>
        private static string FormatBsonValue(BsonValue value)
        {
            return value.Type switch
            {
                BsonType.Null => "null",
                BsonType.String => value.AsString,
                BsonType.Int32 => value.AsInt32.ToString(),
                BsonType.Int64 => value.AsInt64.ToString(),
                BsonType.Double => value.AsDouble.ToString("F2"),
                BsonType.Decimal => value.AsDecimal.ToString("F2"),
                BsonType.Boolean => value.AsBoolean ? "true" : "false",
                BsonType.DateTime => value.AsDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                BsonType.Array => FormatArray(value.AsArray),
                BsonType.Document => FormatDocument(value.AsDocument),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// 格式化BsonArray为字符串
        /// </summary>
        /// <param name="array">BSON数组</param>
        /// <returns>格式化后的字符串</returns>
        private static string FormatArray(BsonArray array)
        {
            var items = new List<string>();
            foreach (var item in array)
            {
                items.Add(FormatBsonValue(item));
            }
            return $"[{string.Join(", ", items)}]";
        }

        /// <summary>
        /// 格式化BsonDocument为字符串
        /// </summary>
        /// <param name="document">BSON文档</param>
        /// <returns>格式化后的字符串</returns>
        private static string FormatDocument(BsonDocument document)
        {
            var pairs = new List<string>();
            foreach (var kvp in document)
            {
                pairs.Add($"{kvp.Key}: {FormatBsonValue(kvp.Value)}");
            }
            return $"{{{string.Join(", ", pairs)}}}";
        }

        /// <summary>
        /// 提取模板中的所有视图名称
        /// </summary>
        /// <param name="templateContent">模板内容</param>
        /// <returns>视图名称列表</returns>
        public static List<string> ExtractViewNames(string templateContent)
        {
            if (string.IsNullOrEmpty(templateContent))
                return new List<string>();

            var viewNames = new HashSet<string>();
            var matches = PlaceholderRegex.Matches(templateContent);

            foreach (Match match in matches)
            {
                var viewName = match.Groups[1].Value.Trim();
                viewNames.Add(viewName);
            }

            return new List<string>(viewNames);
        }

        /// <summary>
        /// 验证模板语法是否正确
        /// </summary>
        /// <param name="templateContent">模板内容</param>
        /// <returns>验证结果，包含错误信息</returns>
        public static TemplateValidationResult ValidateTemplate(string templateContent)
        {
            var result = new TemplateValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(templateContent))
            {
                result.IsValid = false;
                result.Errors.Add("模板内容不能为空");
                return result;
            }

            // 检查是否有未闭合的占位符
            var openBraces = templateContent.Split("{{").Length - 1;
            var closeBraces = templateContent.Split("}}").Length - 1;

            if (openBraces != closeBraces)
            {
                result.IsValid = false;
                result.Errors.Add("占位符括号不匹配，请检查所有 {{}} 是否正确闭合");
            }

            // 检查占位符格式
            var matches = PlaceholderRegex.Matches(templateContent);
            foreach (Match match in matches)
            {
                var viewName = match.Groups[1].Value.Trim();
                var fieldName = match.Groups[2].Value.Trim();

                if (string.IsNullOrEmpty(viewName))
                {
                    result.IsValid = false;
                    result.Errors.Add($"占位符 '{match.Value}' 中的视图名称不能为空");
                }

                if (string.IsNullOrEmpty(fieldName))
                {
                    result.IsValid = false;
                    result.Errors.Add($"占位符 '{match.Value}' 中的字段名称不能为空");
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 模板验证结果
    /// </summary>
    public class TemplateValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
} 