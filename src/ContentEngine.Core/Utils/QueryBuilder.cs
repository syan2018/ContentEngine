using ContentEngine.Core.DataPipeline.Models;

namespace ContentEngine.Core.Utils;

/// <summary>
/// 查询构建器工具类
/// 用于将用户筛选条件转换为 LiteDB 筛选表达式
/// </summary>
public static class QueryBuilder
{
    /// <summary>
    /// 数据筛选条件
    /// </summary>
    public class DataFilter
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 构建 LiteDB 筛选表达式
    /// </summary>
    /// <param name="searchText">搜索文本</param>
    /// <param name="filters">筛选条件列表</param>
    /// <param name="schema">Schema 定义</param>
    /// <returns>LiteDB 筛选表达式字符串</returns>
    public static string BuildFilterExpression(string? searchText, List<DataFilter> filters, SchemaDefinition? schema)
    {
        var conditions = new List<string>();

        // 添加搜索条件
        if (!string.IsNullOrEmpty(searchText) && schema?.Fields != null)
        {
            var searchConditions = new List<string>();
            foreach (var field in schema.Fields)
            {
                if (field.Type == FieldType.Text)
                {
                    searchConditions.Add($"{field.Name} LIKE '%{EscapeValue(searchText)}%'");
                }
            }
            
            if (searchConditions.Count > 0)
            {
                conditions.Add($"({string.Join(" OR ", searchConditions)})");
            }
        }

        // 添加筛选条件
        foreach (var filter in filters)
        {
            var condition = BuildFilterCondition(filter);
            if (!string.IsNullOrEmpty(condition))
            {
                conditions.Add(condition);
            }
        }

        // 组合所有条件
        if (conditions.Count == 0)
            return string.Empty;

        return string.Join(" AND ", conditions);
    }

    /// <summary>
    /// 构建单个筛选条件
    /// </summary>
    private static string BuildFilterCondition(DataFilter filter)
    {
        if (string.IsNullOrEmpty(filter.Field) || string.IsNullOrEmpty(filter.Value))
            return string.Empty;

        var escapedValue = EscapeValue(filter.Value);

        return filter.Operator switch
        {
            "equals" => $"{filter.Field} = '{escapedValue}'",
            "contains" => $"{filter.Field} LIKE '%{escapedValue}%'",
            "greater_than" => $"{filter.Field} > {FormatNumericValue(filter.Value)}",
            "less_than" => $"{filter.Field} < {FormatNumericValue(filter.Value)}",
            "greater_than_or_equal" => $"{filter.Field} >= {FormatNumericValue(filter.Value)}",
            "less_than_or_equal" => $"{filter.Field} <= {FormatNumericValue(filter.Value)}",
            "not_equals" => $"{filter.Field} <> '{escapedValue}'",
            "starts_with" => $"{filter.Field} LIKE '{escapedValue}%'",
            "ends_with" => $"{filter.Field} LIKE '%{escapedValue}'",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 转义字符串值
    /// </summary>
    private static string EscapeValue(string value)
    {
        return value.Replace("'", "''");
    }

    /// <summary>
    /// 格式化数值
    /// </summary>
    private static string FormatNumericValue(string value)
    {
        // 尝试解析为数字
        if (double.TryParse(value, out var numericValue))
        {
            return numericValue.ToString();
        }
        
        // 如果不是数字，作为字符串处理
        return $"'{EscapeValue(value)}'";
    }

    /// <summary>
    /// 获取操作符的显示文本
    /// </summary>
    public static string GetOperatorDisplayText(string operatorValue)
    {
        return operatorValue switch
        {
            "equals" => "等于",
            "contains" => "包含",
            "greater_than" => "大于",
            "less_than" => "小于",
            "greater_than_or_equal" => "大于等于",
            "less_than_or_equal" => "小于等于",
            "not_equals" => "不等于",
            "starts_with" => "开始于",
            "ends_with" => "结束于",
            _ => operatorValue
        };
    }

    /// <summary>
    /// 获取可用的操作符列表
    /// </summary>
    public static List<(string Value, string Text)> GetAvailableOperators()
    {
        return new List<(string, string)>
        {
            ("equals", "等于"),
            ("contains", "包含"),
            ("greater_than", "大于"),
            ("less_than", "小于"),
            ("greater_than_or_equal", "大于等于"),
            ("less_than_or_equal", "小于等于"),
            ("not_equals", "不等于"),
            ("starts_with", "开始于"),
            ("ends_with", "结束于")
        };
    }
} 