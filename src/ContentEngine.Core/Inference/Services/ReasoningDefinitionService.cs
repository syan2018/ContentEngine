using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.Inference.Utils;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理事务定义管理服务实现
    /// </summary>
    public class ReasoningDefinitionService : IReasoningDefinitionService
    {
        private readonly IReasoningRepository _reasoningRepository;
        private readonly ILogger<ReasoningDefinitionService> _logger;

        public ReasoningDefinitionService(
            IReasoningRepository reasoningRepository,
            ILogger<ReasoningDefinitionService> logger)
        {
            _reasoningRepository = reasoningRepository;
            _logger = logger;
        }

        public async Task<ReasoningTransactionDefinition> CreateDefinitionAsync(
            ReasoningTransactionDefinition definition, 
            CancellationToken cancellationToken = default)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new ArgumentException("推理事务名称不能为空", nameof(definition));

            if (definition.QueryDefinitions == null || !definition.QueryDefinitions.Any())
                throw new ArgumentException("推理事务必须包含至少一个查询定义", nameof(definition));

            // 验证定义
            var validationResult = await ValidateDefinitionAsync(definition, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"推理事务定义无效: {string.Join("; ", validationResult.Errors)}");
            }

            definition.CreatedAt = DateTime.UtcNow;
            definition.UpdatedAt = DateTime.UtcNow;

            await _reasoningRepository.CreateDefinitionAsync(definition);
            _logger.LogInformation("创建推理事务定义: {DefinitionName} (ID: {DefinitionId})", definition.Name, definition.Id);

            return definition;
        }

        public async Task<List<ReasoningTransactionDefinition>> GetAllDefinitionsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _reasoningRepository.GetAllDefinitionsAsync();
        }

        public async Task<ReasoningTransactionDefinition?> GetDefinitionByIdAsync(
            string definitionId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                return null;

            return await _reasoningRepository.GetDefinitionByIdAsync(definitionId);
        }

        public async Task<ReasoningTransactionDefinition> UpdateDefinitionAsync(
            ReasoningTransactionDefinition definition, 
            CancellationToken cancellationToken = default)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var existing = await GetDefinitionByIdAsync(definition.Id, cancellationToken);
            if (existing == null)
                throw new InvalidOperationException($"推理事务定义不存在: {definition.Id}");

            // 验证定义
            var validationResult = await ValidateDefinitionAsync(definition, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"推理事务定义无效: {string.Join("; ", validationResult.Errors)}");
            }

            definition.UpdatedAt = DateTime.UtcNow;
            return await _reasoningRepository.UpdateDefinitionAsync(definition);
        }

        public async Task<bool> DeleteDefinitionAsync(
            string definitionId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            return await _reasoningRepository.DeleteDefinitionAsync(definitionId);
        }

        public async Task<DefinitionValidationResult> ValidateDefinitionAsync(
            ReasoningTransactionDefinition definition, 
            CancellationToken cancellationToken = default)
        {
            var result = new DefinitionValidationResult { IsValid = true };

            try
            {
                // 检查基本字段
                if (string.IsNullOrWhiteSpace(definition.Name))
                {
                    result.Errors.Add("推理事务名称不能为空");
                }

                if (definition.QueryDefinitions == null || !definition.QueryDefinitions.Any())
                {
                    result.Errors.Add("推理事务必须包含至少一个查询定义");
                }

                // 验证Prompt模板
                if (definition.PromptTemplate != null && !string.IsNullOrWhiteSpace(definition.PromptTemplate.TemplateContent))
                {
                    var templateValidation = PromptTemplatingEngine.ValidateTemplate(definition.PromptTemplate.TemplateContent);
                    if (!templateValidation.IsValid)
                    {
                        result.Errors.AddRange(templateValidation.Errors.Select(e => $"Prompt模板错误: {e}"));
                    }
                }
                else
                {
                    result.Errors.Add("Prompt模板不能为空");
                }

                // 验证查询定义
                if (definition.QueryDefinitions != null)
                {
                    var duplicateViewNames = definition.QueryDefinitions
                        .GroupBy(q => q.OutputViewName)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateViewNames.Any())
                    {
                        result.Errors.Add($"存在重复的输出视图名称: {string.Join(", ", duplicateViewNames)}");
                    }

                    foreach (var query in definition.QueryDefinitions)
                    {
                        if (string.IsNullOrWhiteSpace(query.OutputViewName))
                        {
                            result.Warnings.Add("存在未命名的输出视图");
                        }

                        if (string.IsNullOrWhiteSpace(query.SourceSchemaName))
                        {
                            result.Errors.Add($"查询 {query.OutputViewName} 缺少源Schema名称");
                        }
                    }
                }

                // 验证执行约束
                if (definition.ExecutionConstraints != null)
                {
                    if (definition.ExecutionConstraints.MaxConcurrentAICalls <= 0)
                    {
                        result.Warnings.Add("最大并发AI调用数应大于0，将使用默认值");
                    }

                    if (definition.ExecutionConstraints.MaxEstimatedCostUSD <= 0)
                    {
                        result.Warnings.Add("最大预估成本应大于0，将使用默认值");
                    }
                }

                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"验证过程中发生错误: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }
    }
} 