using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理组合服务接口
    /// 专门负责组合生成和单个组合的执行
    /// </summary>
    public interface IReasoningCombinationService
    {
        /// <summary>
        /// 根据推理事务定义实时生成输入组合（不依赖执行记录）
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>输入组合列表</returns>
        Task<List<ReasoningInputCombination>> GenerateInputCombinationsAsync(
            string definitionId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行指定推理实例中的单个组合
        /// </summary>
        /// <param name="instanceId">推理事务实例ID</param>
        /// <param name="combinationId">组合ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        Task<ReasoningOutputItem> ExecuteCombinationAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定推理实例中组合的输出结果
        /// </summary>
        /// <param name="instanceId">推理事务实例ID</param>
        /// <param name="combinationId">组合ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>输出结果，如果不存在返回null</returns>
        Task<ReasoningOutputItem?> GetOutputForCombinationAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量执行多个组合
        /// </summary>
        /// <param name="instanceId">推理事务实例ID</param>
        /// <param name="combinationIds">组合ID列表</param>
        /// <param name="maxConcurrency">最大并发数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量执行结果</returns>
        Task<BatchCombinationExecutionResult> BatchExecuteCombinationsAsync(
            string instanceId, 
            List<string> combinationIds, 
            int maxConcurrency = 5, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取组合的详细信息
        /// </summary>
        /// <param name="instanceId">推理事务实例ID</param>
        /// <param name="combinationId">组合ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>组合详细信息</returns>
        Task<CombinationDetails?> GetCombinationDetailsAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 预览组合的Prompt内容
        /// </summary>
        /// <param name="instanceId">推理事务实例ID</param>
        /// <param name="combinationId">组合ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>填充后的Prompt内容</returns>
        Task<string> PreviewCombinationPromptAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 重新执行失败的组合
        /// </summary>
        /// <param name="instanceId">推理事务实例ID</param>
        /// <param name="combinationId">组合ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重新执行结果</returns>
        Task<ReasoningOutputItem> RetryFailedCombinationAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取实例中所有失败的组合
        /// </summary>
        /// <param name="instanceId">推理事务实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>失败的组合列表</returns>
        Task<List<ReasoningInputCombination>> GetFailedCombinationsAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 批量组合执行结果
    /// </summary>
    public class BatchCombinationExecutionResult
    {
        public int TotalRequested { get; set; }
        public int SuccessfullyExecuted { get; set; }
        public int Failed { get; set; }
        public List<ReasoningOutputItem> Results { get; set; } = new();
        public List<string> FailedCombinationIds { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();
        public TimeSpan TotalExecutionTime { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// 组合详细信息
    /// </summary>
    public class CombinationDetails
    {
        public string CombinationId { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public Dictionary<string, object> DataMap { get; set; } = new();
        public string FilledPrompt { get; set; } = string.Empty;
        public ReasoningOutputItem? Output { get; set; }
        public CombinationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
    }

    /// <summary>
    /// 组合状态
    /// </summary>
    public enum CombinationStatus
    {
        Pending,
        Executing,
        Completed,
        Failed,
        Retrying
    }
} 