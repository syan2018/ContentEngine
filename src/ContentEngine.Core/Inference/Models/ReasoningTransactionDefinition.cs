using LiteDB;
using System;
using System.Collections.Generic;

namespace ContentEngine.Core.Inference.Models
{
    /// <summary>
    /// 定义一个推理事务的蓝图。
    /// </summary>
    public class ReasoningTransactionDefinition
    {
        /// <summary>
        /// 推理事务定义的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 推理事务的名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 推理事务的描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 主体Schema名称（可选）
        /// </summary>
        public string? SubjectSchemaName { get; set; }

        /// <summary>
        /// 查询定义列表，定义如何获取数据视图
        /// </summary>
        public List<QueryDefinition> QueryDefinitions { get; set; } = new();

        /// <summary>
        /// Prompt模板定义
        /// </summary>
        public PromptTemplateDefinition PromptTemplate { get; set; } = new();

        /// <summary>
        /// 数据组合规则列表
        /// </summary>
        public List<DataCombinationRule> DataCombinationRules { get; set; } = new();

        /// <summary>
        /// 执行约束
        /// </summary>
        public ExecutionConstraints ExecutionConstraints { get; set; } = new();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 执行约束，用于控制事务执行的资源消耗和风险
    /// </summary>
    public class ExecutionConstraints
    {
        /// <summary>
        /// 最大预估成本（美元）
        /// </summary>
        public decimal MaxEstimatedCostUSD { get; set; } = 10.0m;

        /// <summary>
        /// 最大执行时间（分钟）
        /// </summary>
        public int MaxExecutionTimeMinutes { get; set; } = 30;

        /// <summary>
        /// 最大并发AI调用数
        /// </summary>
        public int MaxConcurrentAICalls { get; set; } = 5;

        /// <summary>
        /// 是否启用批处理
        /// </summary>
        public bool EnableBatching { get; set; } = true;

        /// <summary>
        /// 批处理大小
        /// </summary>
        public int BatchSize { get; set; } = 10;
    }

    /// <summary>
    /// 定义如何获取数据以形成一个"数据视图"。
    /// </summary>
    public class QueryDefinition
    {
        /// <summary>
        /// 查询的唯一标识符
        /// </summary>
        public string QueryId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 输出视图名称，例如 "TargetNPCs", "ActiveScenarios"
        /// </summary>
        public string OutputViewName { get; set; } = string.Empty;

        /// <summary>
        /// 源Schema名称（LiteDB集合名）
        /// </summary>
        public string SourceSchemaName { get; set; } = string.Empty;

        /// <summary>
        /// 筛选表达式，LiteDB BSON表达式，例如 "$.Status == 'Active'"
        /// </summary>
        public string FilterExpression { get; set; } = string.Empty;

        /// <summary>
        /// 选择的字段列表
        /// </summary>
        public List<string> SelectFields { get; set; } = new();
    }

    /// <summary>
    /// 定义Prompt模板。
    /// </summary>
    public class PromptTemplateDefinition
    {
        /// <summary>
        /// 模板内容，例如 "Describe {{NpcView.Name}} in {{ScenarioView.Location}}."
        /// </summary>
        public string TemplateContent { get; set; } = string.Empty;

        /// <summary>
        /// 预期的输入视图名称列表，例如 ["NpcView", "ScenarioView"]
        /// </summary>
        public List<string> ExpectedInputViewNames { get; set; } = new();
    }

    /// <summary>
    /// 定义数据如何组合，主要是指视图间的叉积规则。
    /// </summary>
    public class DataCombinationRule
    {
        /// <summary>
        /// 需要进行叉积的视图名称列表
        /// </summary>
        public List<string> ViewNamesToCrossProduct { get; set; } = new();

        /// <summary>
        /// 单例上下文视图名称列表
        /// </summary>
        public List<string> SingletonViewNamesForContext { get; set; } = new();

        /// <summary>
        /// 最大组合数量，防止组合爆炸
        /// </summary>
        public int MaxCombinations { get; set; } = 1000;

        /// <summary>
        /// 组合策略
        /// </summary>
        public CombinationStrategy Strategy { get; set; } = CombinationStrategy.CrossProduct;

        /// <summary>
        /// 采样规则（当组合数超限时使用）
        /// </summary>
        public SamplingRule? SamplingRule { get; set; }
    }

    /// <summary>
    /// 数据组合策略枚举
    /// </summary>
    public enum CombinationStrategy
    {
        /// <summary>
        /// 完全叉积
        /// </summary>
        CrossProduct,

        /// <summary>
        /// 随机采样
        /// </summary>
        RandomSampling,

        /// <summary>
        /// 基于优先级采样
        /// </summary>
        PrioritySampling
    }

    /// <summary>
    /// 采样规则定义
    /// </summary>
    public class SamplingRule
    {
        /// <summary>
        /// 用于排序的字段名
        /// </summary>
        public string PriorityField { get; set; } = "Priority";

        /// <summary>
        /// 是否优先选择高值（true=优先高值，false=优先低值）
        /// </summary>
        public bool PreferHigherValues { get; set; } = true;

        /// <summary>
        /// 随机采样的种子值
        /// </summary>
        public double RandomSeed { get; set; } = 0.5;
    }
} 