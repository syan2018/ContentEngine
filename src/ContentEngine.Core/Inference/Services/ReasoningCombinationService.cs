using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.Inference.Utils;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理组合服务实现
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
            var startTime = DateTime.UtcNow;
            try
            {
                var prompt = PromptTemplatingEngine.Fill(definition.PromptTemplate.TemplateContent, targetCombination.DataMap);
                
                var result = await _promptExecutionService.ExecutePromptAsync(prompt, "ContentEngineHelper", cancellationToken);
                var executionTime = DateTime.UtcNow - startTime;

                var output = new ReasoningOutputItem
                {
                    InputCombinationId = combinationId,
                    IsSuccess = result.IsSuccess,
                    GeneratedText = result.GeneratedText,
                    ExecutionTime = executionTime,
                    CostUSD = result.CostUSD
                };

                if (!result.IsSuccess)
                {
                    output.FailureReason = result.FailureReason;
                }

                _logger.LogInformation("实例 {InstanceId} 中的组合 {CombinationId} 执行完成，成功: {IsSuccess}", instanceId, combinationId, result.IsSuccess);
                return output;
            }
            catch (Exception ex)
            {
                var executionTime = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "执行实例 {InstanceId} 中的组合 {CombinationId} 时发生错误", instanceId, combinationId);
                
                return new ReasoningOutputItem
                {
                    InputCombinationId = combinationId,
                    IsSuccess = false,
                    FailureReason = ex.Message,
                    ExecutionTime = executionTime
                };
            }
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

        // 其他方法的简化实现
        public Task<BatchCombinationExecutionResult> BatchExecuteCombinationsAsync(string instanceId, List<string> combinationIds, int maxConcurrency = 5, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("批量执行功能将在后续版本实现");
        }

        public Task<CombinationDetails?> GetCombinationDetailsAsync(string instanceId, string combinationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("组合详情功能将在后续版本实现");
        }

        public Task<string> PreviewCombinationPromptAsync(string instanceId, string combinationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Prompt预览功能将在后续版本实现");
        }

        public Task<ReasoningOutputItem> RetryFailedCombinationAsync(string instanceId, string combinationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("重试功能将在后续版本实现");
        }

        public Task<List<ReasoningInputCombination>> GetFailedCombinationsAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("获取失败组合功能将在后续版本实现");
        }
    }
} 