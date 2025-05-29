using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理事务实例管理服务接口
    /// 专门负责推理事务实例的创建、查询、更新、删除操作
    /// </summary>
    public interface IReasoningInstanceService
    {
        /// <summary>
        /// 创建新的推理事务实例
        /// </summary>
        /// <param name="definitionId">推理定义ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的推理事务实例</returns>
        Task<ReasoningTransactionInstance> CreateInstanceAsync(
            string definitionId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有推理事务实例
        /// </summary>
        /// <param name="definitionId">定义ID（可选，用于筛选）</param>
        /// <param name="status">状态（可选，用于筛选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例列表</returns>
        Task<List<ReasoningTransactionInstance>> GetInstancesAsync(
            string? definitionId = null, 
            TransactionStatus? status = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取推理事务实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例，如果不存在返回null</returns>
        Task<ReasoningTransactionInstance?> GetInstanceByIdAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新推理事务实例
        /// </summary>
        /// <param name="instance">推理事务实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的推理事务实例</returns>
        Task<ReasoningTransactionInstance> UpdateInstanceAsync(
            ReasoningTransactionInstance instance, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除推理事务实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteInstanceAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取实例的执行进度
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行进度信息</returns>
        Task<InstanceProgressInfo> GetInstanceProgressAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取实例的统计信息
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>统计信息</returns>
        Task<InstanceStatistics> GetInstanceStatisticsAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 实例进度信息
    /// </summary>
    public class InstanceProgressInfo
    {
        public string InstanceId { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public int TotalCombinations { get; set; }
        public int ProcessedCombinations { get; set; }
        public int SuccessfulOutputs { get; set; }
        public int FailedOutputs { get; set; }
        public decimal ProgressPercentage => TotalCombinations > 0 ? (decimal)ProcessedCombinations / TotalCombinations * 100 : 0;
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedRemainingTime { get; set; }
    }

    /// <summary>
    /// 实例统计信息
    /// </summary>
    public class InstanceStatistics
    {
        public string InstanceId { get; set; } = string.Empty;
        public int TotalCombinations { get; set; }
        public int ProcessedCombinations { get; set; }
        public int SuccessfulOutputs { get; set; }
        public int FailedOutputs { get; set; }
        public decimal EstimatedCostUSD { get; set; }
        public decimal ActualCostUSD { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<ErrorRecord> Errors { get; set; } = new();
    }
} 