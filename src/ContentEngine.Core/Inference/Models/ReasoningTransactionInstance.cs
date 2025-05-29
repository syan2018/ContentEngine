using LiteDB;
using System;
using System.Collections.Generic;

namespace ContentEngine.Core.Inference.Models
{
    /// <summary>
    /// 推理事务的执行实例
    /// </summary>
    public class ReasoningTransactionInstance
    {
        /// <summary>
        /// 实例的唯一标识符
        /// </summary>
        [BsonId]
        public string InstanceId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 关联的推理事务定义ID
        /// </summary>
        public string DefinitionId { get; set; } = string.Empty;

        /// <summary>
        /// 实例开始执行时间
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 实例完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 执行状态
        /// </summary>
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        /// <summary>
        /// 解析后的数据视图，Key为视图名称
        /// </summary>
        public Dictionary<string, ViewData> ResolvedViews { get; set; } = new();

        /// <summary>
        /// 生成的输入组合列表
        /// </summary>
        public List<ReasoningInputCombination> InputCombinations { get; set; } = new();

        /// <summary>
        /// AI生成的输出结果列表
        /// </summary>
        public List<ReasoningOutputItem> Outputs { get; set; } = new();

        /// <summary>
        /// 错误记录列表
        /// </summary>
        public List<ErrorRecord> Errors { get; set; } = new();

        /// <summary>
        /// 最后处理的组合ID（用于断点续传）
        /// </summary>
        public string? LastProcessedCombinationId { get; set; }

        /// <summary>
        /// 执行指标
        /// </summary>
        public ExecutionMetrics Metrics { get; set; } = new();

        /// <summary>
        /// 是否可以恢复执行
        /// </summary>
        public bool CanResume => Status == TransactionStatus.Failed && !string.IsNullOrEmpty(LastProcessedCombinationId);
    }

    /// <summary>
    /// 推理事务执行状态
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending,

        /// <summary>
        /// 获取数据中
        /// </summary>
        FetchingData,

        /// <summary>
        /// 组合数据中
        /// </summary>
        CombiningData,

        /// <summary>
        /// 生成输出中
        /// </summary>
        GeneratingOutputs,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused
    }

    /// <summary>
    /// 错误记录
    /// </summary>
    public class ErrorRecord
    {
        /// <summary>
        /// 错误发生时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 错误类型
        /// </summary>
        public string ErrorType { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 关联的组合ID
        /// </summary>
        public string? CombinationId { get; set; }

        /// <summary>
        /// 是否可重试
        /// </summary>
        public bool IsRetriable { get; set; } = false;
    }

    /// <summary>
    /// 执行指标
    /// </summary>
    public class ExecutionMetrics
    {
        /// <summary>
        /// 总组合数
        /// </summary>
        public int TotalCombinations { get; set; }

        /// <summary>
        /// 已处理的组合数
        /// </summary>
        public int ProcessedCombinations { get; set; }

        /// <summary>
        /// 成功的输出数
        /// </summary>
        public int SuccessfulOutputs { get; set; }

        /// <summary>
        /// 失败的输出数
        /// </summary>
        public int FailedOutputs { get; set; }

        /// <summary>
        /// 预估成本（美元）
        /// </summary>
        public decimal EstimatedCostUSD { get; set; }

        /// <summary>
        /// 实际成本（美元）
        /// </summary>
        public decimal ActualCostUSD { get; set; }

        /// <summary>
        /// 已消耗的执行时间
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// 执行进度百分比
        /// </summary>
        public double ProgressPercentage => TotalCombinations > 0 ? (double)ProcessedCombinations / TotalCombinations * 100 : 0;
    }

    /// <summary>
    /// 数据视图
    /// </summary>
    public class ViewData
    {
        /// <summary>
        /// 视图名称
        /// </summary>
        public string ViewName { get; set; } = string.Empty;

        /// <summary>
        /// 视图包含的数据项列表
        /// </summary>
        public List<BsonDocument> Items { get; set; } = new();

        /// <summary>
        /// 视图数据项数量
        /// </summary>
        public int Count => Items.Count;
    }

    /// <summary>
    /// 推理输入组合
    /// </summary>
    public class ReasoningInputCombination
    {
        /// <summary>
        /// 组合的唯一标识符
        /// </summary>
        [BsonId]
        public string CombinationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 数据映射，Key为视图名称，Value为该视图在此组合中的BsonDocument
        /// </summary>
        public Dictionary<string, BsonDocument> DataMap { get; set; } = new();

        /// <summary>
        /// 组合创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 推理输出项
    /// </summary>
    public class ReasoningOutputItem
    {
        /// <summary>
        /// 输出的唯一标识符
        /// </summary>
        [BsonId]
        public string OutputId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 对应的输入组合ID
        /// </summary>
        public string InputCombinationId { get; set; } = string.Empty;

        /// <summary>
        /// AI生成的文本内容
        /// </summary>
        public string GeneratedText { get; set; } = string.Empty;

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否生成成功
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// 失败原因（如果失败）
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// 消耗的成本（美元）
        /// </summary>
        public decimal CostUSD { get; set; }

        /// <summary>
        /// 生成耗时
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }
} 