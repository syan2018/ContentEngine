using ContentEngine.Core.Inference.Models;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理事务实例管理服务实现
    /// </summary>
    public class ReasoningInstanceService : IReasoningInstanceService
    {
        private readonly IReasoningRepository _reasoningRepository;
        private readonly IReasoningDefinitionService _definitionService;
        private readonly ILogger<ReasoningInstanceService> _logger;

        public ReasoningInstanceService(
            IReasoningRepository reasoningRepository,
            IReasoningDefinitionService definitionService,
            ILogger<ReasoningInstanceService> logger)
        {
            _reasoningRepository = reasoningRepository;
            _definitionService = definitionService;
            _logger = logger;
        }

        public async Task<ReasoningTransactionInstance> CreateInstanceAsync(
            string definitionId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("定义ID不能为空", nameof(definitionId));

            var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
                throw new InvalidOperationException($"推理事务定义不存在: {definitionId}");

            var instance = new ReasoningTransactionInstance
            {
                DefinitionId = definitionId,
                Status = TransactionStatus.Pending,
                StartedAt = DateTime.UtcNow
            };

            await _reasoningRepository.CreateInstanceAsync(instance);
            _logger.LogInformation("创建推理事务实例: {InstanceId} (定义: {DefinitionId})", instance.InstanceId, definitionId);

            return instance;
        }

        public async Task<List<ReasoningTransactionInstance>> GetInstancesAsync(
            string? definitionId = null, 
            TransactionStatus? status = null, 
            CancellationToken cancellationToken = default)
        {
            return await _reasoningRepository.GetInstancesAsync(definitionId, status);
        }

        public async Task<ReasoningTransactionInstance?> GetInstanceByIdAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return null;

            return await _reasoningRepository.GetInstanceByIdAsync(instanceId);
        }

        public async Task<ReasoningTransactionInstance> UpdateInstanceAsync(
            ReasoningTransactionInstance instance, 
            CancellationToken cancellationToken = default)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return await _reasoningRepository.UpdateInstanceAsync(instance);
        }

        public async Task<bool> DeleteInstanceAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return false;

            return await _reasoningRepository.DeleteInstanceAsync(instanceId);
        }

        public async Task<InstanceProgressInfo> GetInstanceProgressAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");

            var progress = new InstanceProgressInfo
            {
                InstanceId = instanceId,
                Status = instance.Status,
                TotalCombinations = instance.Metrics.TotalCombinations,
                ProcessedCombinations = instance.Metrics.ProcessedCombinations,
                SuccessfulOutputs = instance.Metrics.SuccessfulOutputs,
                FailedOutputs = instance.Metrics.FailedOutputs,
                ElapsedTime = instance.Metrics.ElapsedTime
            };

            // 计算预估剩余时间
            if (progress.ProcessedCombinations > 0 && progress.TotalCombinations > progress.ProcessedCombinations)
            {
                var avgTimePerCombination = progress.ElapsedTime.TotalSeconds / progress.ProcessedCombinations;
                var remainingCombinations = progress.TotalCombinations - progress.ProcessedCombinations;
                progress.EstimatedRemainingTime = TimeSpan.FromSeconds(avgTimePerCombination * remainingCombinations);
            }

            return progress;
        }

        public async Task<InstanceStatistics> GetInstanceStatisticsAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");

            return new InstanceStatistics
            {
                InstanceId = instanceId,
                TotalCombinations = instance.Metrics.TotalCombinations,
                ProcessedCombinations = instance.Metrics.ProcessedCombinations,
                SuccessfulOutputs = instance.Metrics.SuccessfulOutputs,
                FailedOutputs = instance.Metrics.FailedOutputs,
                EstimatedCostUSD = instance.Metrics.EstimatedCostUSD,
                ActualCostUSD = instance.Metrics.ActualCostUSD,
                ElapsedTime = instance.Metrics.ElapsedTime,
                StartedAt = instance.StartedAt,
                CompletedAt = instance.CompletedAt,
                Errors = instance.Errors
            };
        }
    }
} 