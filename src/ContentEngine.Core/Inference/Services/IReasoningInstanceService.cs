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
        /// 获取实例的基本统计信息（用于报表和历史数据）
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>基本统计信息</returns>
        Task<InstanceBasicStats> GetInstanceBasicStatsAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 实例基本统计信息（用于报表，不包含实时状态）
    /// </summary>
    public class InstanceBasicStats
    {
        public string InstanceId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TransactionStatus FinalStatus { get; set; }
        public int TotalCombinations { get; set; }
        public int SuccessfulOutputs { get; set; }
        public int FailedOutputs { get; set; }
        public decimal ActualCostUSD { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public List<ErrorRecord> CriticalErrors { get; set; } = new();
    }
} 