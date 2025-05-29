using System.Collections.Generic;

namespace ContentEngine.Core.Inference.Models
{
    /// <summary>
    /// 处理后的输入数据，包含解析的视图和生成的组合
    /// </summary>
    public class ProcessedInputData
    {
        /// <summary>
        /// 解析后的数据视图，Key为视图名称
        /// </summary>
        public Dictionary<string, ViewData> ResolvedViews { get; set; } = new();

        /// <summary>
        /// 生成的输入组合列表
        /// </summary>
        public List<ReasoningInputCombination> Combinations { get; set; } = new();

        /// <summary>
        /// 处理统计信息
        /// </summary>
        public ProcessingStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// 处理统计信息
    /// </summary>
    public class ProcessingStatistics
    {
        /// <summary>
        /// 总视图数量
        /// </summary>
        public int TotalViews { get; set; }

        /// <summary>
        /// 总记录数量
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 生成的组合数量
        /// </summary>
        public int GeneratedCombinations { get; set; }

        /// <summary>
        /// 处理耗时
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
    }
} 