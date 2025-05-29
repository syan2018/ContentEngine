using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理执行服务接口
    /// 专门负责推理事务的执行、控制和监控
    /// </summary>
    public interface IReasoningExecutionService
    {
        /// <summary>
        /// 执行推理事务
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例</returns>
        Task<ReasoningTransactionInstance> ExecuteTransactionAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 恢复执行失败的推理事务
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例</returns>
        Task<ReasoningTransactionInstance> ResumeTransactionAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 暂停正在执行的推理事务
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否暂停成功</returns>
        Task<bool> PauseTransactionAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消正在执行的推理事务
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否取消成功</returns>
        Task<bool> CancelTransactionAsync(
            string instanceId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查推理事务是否可以执行
        /// </summary>
        /// <param name="definitionId">推理定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行前检查结果</returns>
        Task<ExecutionPreCheckResult> PreCheckExecutionAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取正在执行的推理事务列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>正在执行的实例列表</returns>
        Task<List<ReasoningTransactionInstance>> GetRunningTransactionsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量取消推理事务
        /// </summary>
        /// <param name="instanceIds">实例ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>取消结果</returns>
        Task<BatchCancelResult> BatchCancelTransactionsAsync(
            List<string> instanceIds, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 执行前检查结果
    /// </summary>
    public class ExecutionPreCheckResult
    {
        public bool CanExecute { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public decimal EstimatedCostUSD { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public int EstimatedCombinations { get; set; }
    }

    /// <summary>
    /// 批量取消结果
    /// </summary>
    public class BatchCancelResult
    {
        public int TotalRequested { get; set; }
        public int SuccessfullyCancelled { get; set; }
        public int Failed { get; set; }
        public List<string> FailedInstanceIds { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();
    }
} 