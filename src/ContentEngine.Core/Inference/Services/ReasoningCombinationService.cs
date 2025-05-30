using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.Inference.Utils;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理组合服务实现
    /// 专注于组合级别的操作：生成、执行、查询、重试
    /// </summary>
    public class ReasoningCombinationService : IReasoningCombinationService
    {
        private readonly IReasoningDefinitionService _definitionService;
        private readonly IReasoningInstanceService _instanceService;
        private readonly IQueryProcessingService _queryProcessingService;
        private readonly IPromptExecutionService _promptExecutionService;
        private readonly ILogger<ReasoningCombinationService> _logger;

        public ReasoningCombinationService(
            IReasoningDefinitionService definitionService,
            IReasoningInstanceService instanceService,
            IQueryProcessingService queryProcessingService,
            IPromptExecutionService promptExecutionService,
            ILogger<ReasoningCombinationService> logger)
        {
            _definitionService = definitionService;
            _instanceService = instanceService;
            _queryProcessingService = queryProcessingService;
            _promptExecutionService = promptExecutionService;
            _logger = logger;
        }

        public async Task<List<ReasoningInputCombination>> GenerateInputCombinationsAsync(
            string definitionId, 
            CancellationToken cancellationToken = default)
        {
            var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"推理事务定义不存在: {definitionId}");
            }

            return await _queryProcessingService.GenerateInputCombinationsAsync(definition, cancellationToken);
        }

        public async Task<ReasoningOutputItem> ExecuteCombinationAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");
            }

            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"与实例 {instanceId} 关联的推理事务定义 {instance.DefinitionId} 不存在。");
            }

            var targetCombination = instance.InputCombinations.FirstOrDefault(c => c.CombinationId == combinationId);
            if (targetCombination == null)
            {
                throw new InvalidOperationException($"组合ID {combinationId} 在实例 {instanceId} 中未找到。");
            }

            // 执行单个组合
            var output = await ExecuteSingleCombinationAsync(definition, targetCombination, cancellationToken);

            // 持久化结果到实例中
            await PersistCombinationResultAsync(instance, output, cancellationToken);

            _logger.LogInformation("实例 {InstanceId} 中的组合 {CombinationId} 执行完成，成功: {IsSuccess}", 
                instanceId, combinationId, output.IsSuccess);
            
                return output;
        }

        public async Task<ReasoningOutputItem?> GetOutputForCombinationAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                _logger.LogWarning("尝试获取组合输出时未找到实例: {InstanceId}", instanceId);
                return null;
            }
            
            var output = instance.Outputs.FirstOrDefault(o => o.InputCombinationId == combinationId);
            if (output == null)
            {
                _logger.LogDebug("在实例 {InstanceId} 中未找到组合 {CombinationId} 的输出", instanceId, combinationId);
            }
            return output;
        }

        public async Task<BatchCombinationExecutionResult> BatchExecuteCombinationsAsync(
            string instanceId, 
            List<string> combinationIds, 
            int maxConcurrency = 5, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");
            }

            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"与实例 {instanceId} 关联的推理事务定义 {instance.DefinitionId} 不存在。");
            }

            var result = new BatchCombinationExecutionResult
            {
                TotalRequested = combinationIds.Count
            };

            var startTime = DateTime.UtcNow;
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task>();

            foreach (var combinationId in combinationIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var task = ProcessSingleCombinationInBatchAsync(
                    instance, definition, combinationId, result, semaphore, cancellationToken);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            result.TotalExecutionTime = DateTime.UtcNow - startTime;

            // 最终持久化实例状态，确保所有结果都被保存
            try
            {
                await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
                _logger.LogInformation("批量执行完成并持久化: {InstanceId}, 请求: {Total}, 成功: {Success}, 失败: {Failed}", 
                    instanceId, result.TotalRequested, result.SuccessfullyExecuted, result.Failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量执行最终持久化失败: {InstanceId}, 尝试重试", instanceId);
                
                // 重试一次持久化
                try
                {
                    await Task.Delay(1000, cancellationToken); // 等待1秒后重试
                    await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
                    _logger.LogWarning("批量执行持久化重试成功: {InstanceId}", instanceId);
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "批量执行持久化重试仍然失败: {InstanceId}, 结果可能丢失", instanceId);
                    // 不抛出异常，因为执行本身是成功的，只是持久化失败
                }
            }

            return result;
        }

        public async Task<CombinationDetails?> GetCombinationDetailsAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                return null;
            }

            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
            if (definition == null)
            {
                return null;
            }

            var combination = instance.InputCombinations.FirstOrDefault(c => c.CombinationId == combinationId);
            if (combination == null)
            {
                return null;
            }

            var output = instance.Outputs.FirstOrDefault(o => o.InputCombinationId == combinationId);
            var filledPrompt = PromptTemplatingEngine.Fill(definition.PromptTemplate.TemplateContent, combination.DataMap);

            return new CombinationDetails
            {
                CombinationId = combinationId,
                InstanceId = instanceId,
                DataMap = combination.DataMap.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value),
                FilledPrompt = filledPrompt,
                Output = output,
                Status = DetermineCombinationStatus(output),
                CreatedAt = instance.StartedAt,
                ExecutedAt = output?.ExecutionTime != null ? instance.StartedAt.Add(output.ExecutionTime) : null
            };
        }

        public async Task<string> PreviewCombinationPromptAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");
            }

            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"与实例 {instanceId} 关联的推理事务定义 {instance.DefinitionId} 不存在。");
            }

            var combination = instance.InputCombinations.FirstOrDefault(c => c.CombinationId == combinationId);
            if (combination == null)
            {
                throw new InvalidOperationException($"组合ID {combinationId} 在实例 {instanceId} 中未找到。");
            }

            return PromptTemplatingEngine.Fill(definition.PromptTemplate.TemplateContent, combination.DataMap);
        }

        public async Task<ReasoningOutputItem> RetryFailedCombinationAsync(
            string instanceId, 
            string combinationId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");
            }

            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"与实例 {instanceId} 关联的推理事务定义 {instance.DefinitionId} 不存在。");
            }

            var combination = instance.InputCombinations.FirstOrDefault(c => c.CombinationId == combinationId);
            if (combination == null)
            {
                throw new InvalidOperationException($"组合ID {combinationId} 在实例 {instanceId} 中未找到。");
            }

            // 检查是否已有失败的输出
            var existingOutput = instance.Outputs.FirstOrDefault(o => o.InputCombinationId == combinationId);
            if (existingOutput != null && existingOutput.IsSuccess)
            {
                _logger.LogWarning("尝试重试成功的组合: {InstanceId}/{CombinationId}", instanceId, combinationId);
                return existingOutput;
            }

            // 重新执行
            var newOutput = await ExecuteSingleCombinationAsync(definition, combination, cancellationToken);

            // 更新或添加结果
            if (existingOutput != null)
            {
                instance.Outputs.Remove(existingOutput);
            }
            
            await PersistCombinationResultAsync(instance, newOutput, cancellationToken);

            _logger.LogInformation("重试组合完成: {InstanceId}/{CombinationId}, 成功: {IsSuccess}", 
                instanceId, combinationId, newOutput.IsSuccess);

            return newOutput;
        }

        public async Task<List<ReasoningInputCombination>> GetFailedCombinationsAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                return new List<ReasoningInputCombination>();
            }

            var failedOutputIds = instance.Outputs
                .Where(o => !o.IsSuccess)
                .Select(o => o.InputCombinationId)
                .ToHashSet();

            return instance.InputCombinations
                .Where(c => failedOutputIds.Contains(c.CombinationId))
                .ToList();
        }

        public async Task<List<ReasoningInputCombination>> RegenerateAndResetInstanceAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await _instanceService.GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
            {
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");
            }

            var definition = await _definitionService.GetDefinitionByIdAsync(instance.DefinitionId, cancellationToken);
            if (definition == null)
            {
                throw new InvalidOperationException($"与实例 {instanceId} 关联的推理事务定义 {instance.DefinitionId} 不存在。");
            }

            _logger.LogInformation("开始重新生成组合并重置实例状态: {InstanceId}", instanceId);

            // 1. 重新生成组合
            var newCombinations = await _queryProcessingService.GenerateInputCombinationsAsync(definition, cancellationToken);

            // 2. 重置实例状态
            ResetInstanceState(instance, newCombinations);

            // 3. 持久化更新
            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);

            _logger.LogInformation("组合重新生成完成: {InstanceId}, 新组合数: {Count}, 状态已重置", 
                instanceId, newCombinations.Count);

            return newCombinations;
        }

        #region 私有辅助方法

        /// <summary>
        /// 执行单个组合（核心逻辑）
        /// </summary>
        private async Task<ReasoningOutputItem> ExecuteSingleCombinationAsync(
            ReasoningTransactionDefinition definition,
            ReasoningInputCombination combination,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // 填充Prompt模板
                var filledPrompt = PromptTemplatingEngine.Fill(definition.PromptTemplate.TemplateContent, combination.DataMap);

                // 执行AI调用
                var result = await _promptExecutionService.ExecutePromptAsync(filledPrompt, "ContentEngineHelper", cancellationToken);
                var executionTime = DateTime.UtcNow - startTime;

                return new ReasoningOutputItem
                {
                    InputCombinationId = combination.CombinationId,
                    IsSuccess = result.IsSuccess,
                    GeneratedText = result.GeneratedText,
                    FailureReason = result.FailureReason,
                    CostUSD = result.CostUSD,
                    ExecutionTime = executionTime
                };
            }
            catch (Exception ex)
            {
                var executionTime = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "执行组合 {CombinationId} 时发生错误", combination.CombinationId);
                
                return new ReasoningOutputItem
                {
                    InputCombinationId = combination.CombinationId,
                    IsSuccess = false,
                    FailureReason = ex.Message,
                    ExecutionTime = executionTime
                };
            }
        }

        /// <summary>
        /// 持久化组合结果到实例中
        /// </summary>
        private async Task PersistCombinationResultAsync(
            ReasoningTransactionInstance instance,
            ReasoningOutputItem output,
            CancellationToken cancellationToken)
        {
            // 移除已存在的输出（如果有）
            var existingOutput = instance.Outputs.FirstOrDefault(o => o.InputCombinationId == output.InputCombinationId);
            if (existingOutput != null)
            {
                instance.Outputs.Remove(existingOutput);
                
                // 更新指标
                instance.Metrics.ActualCostUSD -= existingOutput.CostUSD;
                if (existingOutput.IsSuccess)
                {
                    instance.Metrics.SuccessfulOutputs--;
                }
                else
                {
                    instance.Metrics.FailedOutputs--;
                }
            }

            // 添加新输出
            instance.Outputs.Add(output);
            instance.Metrics.ActualCostUSD += output.CostUSD;
            instance.LastProcessedCombinationId = output.InputCombinationId;

            if (output.IsSuccess)
            {
                instance.Metrics.SuccessfulOutputs++;
            }
            else
            {
                instance.Metrics.FailedOutputs++;
                instance.Errors.Add(new ErrorRecord
                {
                    ErrorType = "CombinationExecutionError",
                    Message = output.FailureReason ?? "组合执行失败",
                    CombinationId = output.InputCombinationId,
                    IsRetriable = true
                });
            }

            // 更新处理计数（如果是新的组合）
            if (existingOutput == null)
            {
                instance.Metrics.ProcessedCombinations++;
            }

            await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
        }

        /// <summary>
        /// 批量处理中的单个组合处理
        /// </summary>
        private async Task ProcessSingleCombinationInBatchAsync(
            ReasoningTransactionInstance instance,
            ReasoningTransactionDefinition definition,
            string combinationId,
            BatchCombinationExecutionResult batchResult,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var combination = instance.InputCombinations.FirstOrDefault(c => c.CombinationId == combinationId);
                if (combination == null)
                {
                    lock (batchResult)
                    {
                        batchResult.Failed++;
                        batchResult.FailedCombinationIds.Add(combinationId);
                        batchResult.ErrorMessages.Add($"组合 {combinationId} 未找到");
                    }
                    return;
                }

                var output = await ExecuteSingleCombinationAsync(definition, combination, cancellationToken);

                lock (batchResult)
                {
                    batchResult.Results.Add(output);
                    batchResult.TotalCost += output.CostUSD;

                    if (output.IsSuccess)
                    {
                        batchResult.SuccessfullyExecuted++;
                    }
                    else
                    {
                        batchResult.Failed++;
                        batchResult.FailedCombinationIds.Add(combinationId);
                        batchResult.ErrorMessages.Add(output.FailureReason ?? "执行失败");
                    }
                }

                lock (instance)
                {
                    // 移除已存在的输出
                    var existingOutput = instance.Outputs.FirstOrDefault(o => o.InputCombinationId == combinationId);
                    if (existingOutput != null)
                    {
                        instance.Outputs.Remove(existingOutput);
                        instance.Metrics.ActualCostUSD -= existingOutput.CostUSD;
                        if (existingOutput.IsSuccess)
                            instance.Metrics.SuccessfulOutputs--;
                        else
                            instance.Metrics.FailedOutputs--;
                    }

                    // 添加新输出
                    instance.Outputs.Add(output);
                    instance.Metrics.ActualCostUSD += output.CostUSD;
                    instance.LastProcessedCombinationId = combinationId;

                    if (output.IsSuccess)
                    {
                        instance.Metrics.SuccessfulOutputs++;
                    }
                    else
                    {
                        instance.Metrics.FailedOutputs++;
                        instance.Errors.Add(new ErrorRecord
                        {
                            ErrorType = "BatchCombinationExecutionError",
                            Message = output.FailureReason ?? "批量组合执行失败",
                            CombinationId = combinationId,
                            IsRetriable = true
                        });
                    }

                    if (existingOutput == null)
                    {
                        instance.Metrics.ProcessedCombinations++;
                    }
                }

                try
                {
                    await _instanceService.UpdateInstanceAsync(instance, cancellationToken);
                    _logger.LogDebug("批量执行定期持久化: {InstanceId}, 已处理: {ProcessedCount}", 
                        instance.InstanceId, instance.Metrics.ProcessedCombinations);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "批量执行定期持久化失败: {InstanceId}", instance.InstanceId);
                    // 持久化失败不影响执行继续
                }
                
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// 确定组合状态
        /// </summary>
        private static CombinationStatus DetermineCombinationStatus(ReasoningOutputItem? output)
        {
            if (output == null)
                return CombinationStatus.Pending;
            
            return output.IsSuccess ? CombinationStatus.Completed : CombinationStatus.Failed;
        }

        /// <summary>
        /// 重置实例状态
        /// </summary>
        private void ResetInstanceState(ReasoningTransactionInstance instance, List<ReasoningInputCombination> newCombinations)
        {
            // 重置组合和输出
            instance.InputCombinations = newCombinations;
            instance.Outputs.Clear();
            
            // 重置指标
            instance.Metrics.TotalCombinations = newCombinations.Count;
            instance.Metrics.ActualCostUSD = 0;
            instance.Metrics.ProcessedCombinations = 0;
            instance.Metrics.SuccessfulOutputs = 0;
            instance.Metrics.FailedOutputs = 0;
            instance.Metrics.ElapsedTime = TimeSpan.Zero;
            
            // 重置错误和状态
            instance.Errors.Clear();
            instance.LastProcessedCombinationId = null;
            
            // 重置执行状态和时间戳
            if (instance.Status == TransactionStatus.Completed || instance.Status == TransactionStatus.Failed)
            {
                instance.Status = TransactionStatus.Pending;
                instance.CompletedAt = null;
                // 保持 StartedAt 不变，因为这是实例的创建时间
            }
            
            _logger.LogDebug("实例状态已重置: {InstanceId}, 状态: {Status}, 新组合数: {Count}", 
                instance.InstanceId, instance.Status, newCombinations.Count);
        }

        #endregion
    }
} 