using ContentEngine.Core.Inference.Models;
using Microsoft.Extensions.Logging;
using ContentEngine.Core.Inference.Utils;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理执行服务实现
    /// 专注于事务级别的执行控制和监控
    /// </summary>
    public class ReasoningExecutionService : IReasoningExecutionService
    {
        private readonly IReasoningDefinitionService _definitionService;
        private readonly IReasoningInstanceService _instanceService;
        private readonly IReasoningEstimationService _estimationService;
        private readonly IQueryProcessingService _queryProcessingService;
        private readonly IReasoningCombinationService _combinationService;
        private readonly ILogger<ReasoningExecutionService> _logger;

        public ReasoningExecutionService(
            IReasoningDefinitionService definitionService,
            IReasoningInstanceService instanceService,
            IReasoningEstimationService estimationService,
            IQueryProcessingService queryProcessingService,
            IReasoningCombinationService combinationService,
            ILogger<ReasoningExecutionService> logger)
        {
            _definitionService = definitionService;
            _instanceService = instanceService;
            _estimationService = estimationService;
            _queryProcessingService = queryProcessingService;
            _combinationService = combinationService;
            _logger = logger;
        }

        public async Task<ReasoningTransactionInstance> ExecuteTransactionAsync(
            string instanceId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            // 获取实例
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");
            }
            
            // 执行前检查
            var preCheckResult = await PreCheckExecutionAsync(instance.DefinitionId, executionParams, cancellationToken);
            if (!preCheckResult.CanExecute)
            {
                throw new InvalidOperationException($"执行前检查失败: {string.Join("; ", preCheckResult.Errors)}");
            }
            
            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"推理事务定义不存在: {instance.DefinitionId}");
            }

            _logger.LogInformation("开始执行推理事务: {DefinitionName} (实例ID: {InstanceId})", definition.Name, instance.InstanceId);
            
            try
            {
                instance.Metrics.EstimatedCostUSD = preCheckResult.EstimatedCostUSD;

                // 处理数据
                await ProcessDataAsync(instance, definition, cancellationToken);
                
                // 生成AI输出
                await GenerateOutputsAsync(instance, definition, cancellationToken);

                // 重新获取最新的实例状态，因为批量执行可能已经更新了实例
                var latestInstance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
                if (latestInstance == null)
                {
                    throw new InvalidOperationException($"无法获取最新的实例状态: {instanceId}");
                }

                // 标记完成
                latestInstance.Status = TransactionStatus.Completed;
                latestInstance.CompletedAt = DateTime.UtcNow;
                latestInstance.Metrics.ElapsedTime = DateTime.UtcNow - latestInstance.StartedAt;

                await _instanceService.UpdateInstanceAsync(latestInstance, cancellationToken);
                _logger.LogInformation("推理事务执行完成: {InstanceId}", latestInstance.InstanceId);

                return latestInstance;
            }
            catch (Exception ex)
            {
                // 在异常情况下也需要获取最新实例状态，避免覆盖已有的执行结果
                var latestInstanceForError = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
                if (latestInstanceForError != null)
                {
                    latestInstanceForError.Status = TransactionStatus.Failed;
                    latestInstanceForError.CompletedAt = DateTime.UtcNow;
                    latestInstanceForError.Metrics.ElapsedTime = DateTime.UtcNow - latestInstanceForError.StartedAt;
                    latestInstanceForError.Errors.Add(new ErrorRecord
                    {
                        ErrorType = ex.GetType().Name,
                        Message = ex.Message,
                        IsRetriable = true
                    });

                    await _instanceService.UpdateInstanceAsync(latestInstanceForError, cancellationToken);
                    _logger.LogError(ex, "推理事务执行失败: {InstanceId}", latestInstanceForError.InstanceId);
                }
                else
                {
                    // 如果无法获取最新实例，使用原始实例
                    instance.Status = TransactionStatus.Failed;
                    instance.CompletedAt = DateTime.UtcNow;
                    instance.Metrics.ElapsedTime = DateTime.UtcNow - instance.StartedAt;
                    instance.Errors.Add(new ErrorRecord
                    {
                        ErrorType = ex.GetType().Name,
                        Message = ex.Message,
                        IsRetriable = true
                    });

                    await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
                    _logger.LogError(ex, "推理事务执行失败: {InstanceId}", instance.InstanceId);
                }

                throw;
            }
        }

        public async Task<ExecutionPreCheckResult> PreCheckExecutionAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new ExecutionPreCheckResult { CanExecute = true };

            try
            {
                var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
                if (definition == null)
                {
                    result.Errors.Add($"推理事务定义不存在: {definitionId}");
                    result.CanExecute = false;
                    return result;
                }

                // 预估成本、时间和组合数量
                result.EstimatedCostUSD = await _estimationService.EstimateExecutionCostAsync(definitionId, executionParams, cancellationToken);
                result.EstimatedTime = await _estimationService.EstimateExecutionTimeAsync(definitionId, executionParams, cancellationToken);
                result.EstimatedCombinations = await _estimationService.EstimateCombinationCountAsync(definitionId, executionParams, cancellationToken);

                // 检查成本限制
                if (result.EstimatedCostUSD > definition.ExecutionConstraints.MaxEstimatedCostUSD)
                {
                    result.Errors.Add($"预估成本 ${result.EstimatedCostUSD:F2} 超过限制 ${definition.ExecutionConstraints.MaxEstimatedCostUSD:F2}");
                }

                // 检查组合数量
                if (result.EstimatedCombinations == 0)
                {
                    result.Warnings.Add("预估组合数量为0，可能没有匹配的数据");
                }

                result.CanExecute = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"执行前检查时发生错误: {ex.Message}");
                result.CanExecute = false;
            }

            return result;
        }

        public async Task<List<ReasoningTransactionInstance>> GetRunningTransactionsAsync(
            CancellationToken cancellationToken = default)
        {
            var runningStatuses = new[] { TransactionStatus.Pending, TransactionStatus.FetchingData, 
                                        TransactionStatus.CombiningData, TransactionStatus.GeneratingOutputs };
            
            var allInstances = await _instanceService.GetInstancesAsync(cancellationToken: cancellationToken);
            return allInstances.Where(i => runningStatuses.Contains(i.Status)).ToList();
        }

        // 暂未实现的方法
        public Task<ReasoningTransactionInstance> ResumeTransactionAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("恢复执行功能将在后续版本实现");
        }

        public Task<bool> PauseTransactionAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("暂停执行功能将在后续版本实现");
        }

        public Task<bool> CancelTransactionAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("取消执行功能将在后续版本实现");
        }

        public Task<BatchCancelResult> BatchCancelTransactionsAsync(List<string> instanceIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("批量取消功能将在后续版本实现");
        }

        #region 私有辅助方法

        private async Task ProcessDataAsync(ReasoningTransactionInstance instance, ReasoningTransactionDefinition definition, CancellationToken cancellationToken)
        {
            instance.Status = TransactionStatus.FetchingData;
            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);

            _logger.LogInformation("开始处理数据: {InstanceId}", instance.InstanceId);

            // 检查实例是否已经有组合（可能是用户手动生成的）
            if (instance.InputCombinations?.Any() == true)
            {
                _logger.LogInformation("使用实例现有组合: {InstanceId}, 组合数: {Count}", 
                    instance.InstanceId, instance.InputCombinations.Count);
                
                // 确保指标正确
                instance.Metrics.TotalCombinations = instance.InputCombinations.Count;
                
            }
            else
            {
                // 实例没有组合，需要重新生成
                _logger.LogInformation("实例无现有组合，重新生成: {InstanceId}", instance.InstanceId);
                
                var processedData = await _queryProcessingService.GenerateAndResolveInputDataAsync(definition, cancellationToken);
                
                instance.ResolvedViews = processedData.ResolvedViews;
                instance.InputCombinations = processedData.Combinations;
                instance.Metrics.TotalCombinations = processedData.Combinations.Count;
                
                _logger.LogInformation("数据处理完成: {InstanceId}, 视图数: {ViewCount}, 组合数: {CombinationCount}", 
                    instance.InstanceId, processedData.Statistics.TotalViews, processedData.Statistics.GeneratedCombinations);
            }

            instance.Status = TransactionStatus.CombiningData;
            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
        }

        private async Task GenerateOutputsAsync(ReasoningTransactionInstance instance, ReasoningTransactionDefinition definition, CancellationToken cancellationToken)
        {
            instance.Status = TransactionStatus.GeneratingOutputs;
            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);

            _logger.LogInformation("开始生成AI输出: {InstanceId}, 组合数: {CombinationCount}", 
                instance.InstanceId, instance.InputCombinations.Count);

            // 使用 ReasoningCombinationService 的批量执行功能
            var combinationIds = instance.InputCombinations.Select(c => c.CombinationId).ToList();
            var constraints = definition.ExecutionConstraints;

            var batchResult = await _combinationService.BatchExecuteCombinationsAsync(
                instance.InstanceId, 
                combinationIds, 
                constraints.MaxConcurrentAICalls, 
                cancellationToken);

            _logger.LogInformation("AI输出生成完成: {InstanceId}, 成功: {Success}, 失败: {Failed}, 总成本: ${Cost:F2}", 
                instance.InstanceId, batchResult.SuccessfullyExecuted, batchResult.Failed, batchResult.TotalCost);
        }

        #endregion
    }
} 