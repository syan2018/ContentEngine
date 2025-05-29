using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理预估服务接口
    /// 专门负责推理事务的成本、时间和组合数量预估
    /// </summary>
    public interface IReasoningEstimationService
    {
        /// <summary>
        /// 预估推理事务的执行成本
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预估成本（美元）</returns>
        Task<decimal> EstimateExecutionCostAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 预估推理事务的执行时间
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预估执行时间</returns>
        Task<TimeSpan> EstimateExecutionTimeAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 预估推理事务的组合数量
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预估组合数量</returns>
        Task<int> EstimateCombinationCountAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取详细的预估信息
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>详细预估信息</returns>
        Task<DetailedEstimation> GetDetailedEstimationAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量预估多个推理定义
        /// </summary>
        /// <param name="definitionIds">推理定义ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量预估结果</returns>
        Task<List<BatchEstimationResult>> BatchEstimateAsync(
            List<string> definitionIds, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取历史预估准确性统计
        /// </summary>
        /// <param name="definitionId">推理定义ID（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预估准确性统计</returns>
        Task<EstimationAccuracyStats> GetEstimationAccuracyAsync(
            string? definitionId = null, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 详细预估信息
    /// </summary>
    public class DetailedEstimation
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DefinitionName { get; set; } = string.Empty;
        public int EstimatedCombinations { get; set; }
        public decimal EstimatedCostUSD { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public EstimationBreakdown Breakdown { get; set; } = new();
        public List<string> Assumptions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime EstimatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 预估分解信息
    /// </summary>
    public class EstimationBreakdown
    {
        public Dictionary<string, int> QueryRecordCounts { get; set; } = new();
        public decimal CostPerCombination { get; set; }
        public int MaxConcurrency { get; set; }
        public double SecondsPerCombination { get; set; }
        public Dictionary<string, decimal> CostByComponent { get; set; } = new();
    }

    /// <summary>
    /// 批量预估结果
    /// </summary>
    public class BatchEstimationResult
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DefinitionName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int EstimatedCombinations { get; set; }
        public decimal EstimatedCostUSD { get; set; }
        public TimeSpan EstimatedTime { get; set; }
    }

    /// <summary>
    /// 预估准确性统计
    /// </summary>
    public class EstimationAccuracyStats
    {
        public int TotalExecutions { get; set; }
        public double CostAccuracyPercentage { get; set; }
        public double TimeAccuracyPercentage { get; set; }
        public double CombinationAccuracyPercentage { get; set; }
        public decimal AverageCostDeviation { get; set; }
        public TimeSpan AverageTimeDeviation { get; set; }
        public double AverageCombinationDeviation { get; set; }
        public DateTime LastUpdated { get; set; }
    }
} 