using System.Text.RegularExpressions;

namespace ContentEngine.Core.AI.Utils
{
    /// <summary>
    /// AI输出清理工具，去除 <think>...</think> 标签内容。
    /// </summary>
    public static class AIResponseCleaner
    {
        /// <summary>
        /// 移除AI输出中的所有 <think>...</think> 标签及其内容。
        /// </summary>
        public static string? RemoveThinkTags(string? input)
        {
            return string.IsNullOrEmpty(input) ? input :
                // 非贪婪匹配，支持多行
                Regex.Replace(input, @"<think>[\s\S]*?</think>", string.Empty, RegexOptions.IgnoreCase);
        }
    }
}