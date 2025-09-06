using ConfigurableAIProvider.Services.Factories;
using ConfigurableAIProvider.Services;
using ConfigurableAIProvider.Services.Loaders;
using ConfigurableAIProvider.Services.Providers;
using ContentEngine.Core.Inference.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace ContentEngine.Core.AI.Services
{
    /// <summary>
    /// Prompt执行服务实现
    /// </summary>
    public class PromptExecutionService : IPromptExecutionService
    {
        private readonly IAIKernelFactory _kernelFactory;
        private readonly ISimpleRateLimiter _rateLimiter;
        private readonly IAgentConfigLoader _agentConfigLoader;
        private readonly IModelProvider _modelProvider;
        private readonly ILogger<PromptExecutionService> _logger;

        public PromptExecutionService(
            IAIKernelFactory kernelFactory,
            ISimpleRateLimiter rateLimiter,
            IAgentConfigLoader agentConfigLoader,
            IModelProvider modelProvider,
            ILogger<PromptExecutionService> logger)
        {
            _kernelFactory = kernelFactory;
            _rateLimiter = rateLimiter;
            _agentConfigLoader = agentConfigLoader;
            _modelProvider = modelProvider;
            _logger = logger;
        }

        public async Task<PromptExecutionResult> ExecutePromptAsync(
            string promptText, 
            string agentName = "ContentEngineHelper", 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(promptText))
                throw new ArgumentException("Prompt文本不能为空", nameof(promptText));

            var stopwatch = Stopwatch.StartNew();
            var result = new PromptExecutionResult();

            try
            {
                // 获取模型定义ID（用于RPM检查）
                var modelDefinitionId = await GetPrimaryModelDefinitionIdAsync(agentName);
                
                // 等待RPM许可（会自动排队）
                if (!string.IsNullOrEmpty(modelDefinitionId))
                {
                    await _rateLimiter.WaitForPermissionAsync(modelDefinitionId, cancellationToken);
                    _logger.LogDebug("获得RPM许可: {AgentName}, 模型: {ModelId}", agentName, modelDefinitionId);
                }

                // 获取Kernel实例
                var kernel = await _kernelFactory.BuildKernelAsync(agentName);
                
                _logger.LogDebug("执行Prompt: {AgentName}, 长度: {PromptLength}", agentName, promptText.Length);

                // 执行Prompt
                var response = await kernel.InvokePromptAsync(promptText, cancellationToken: cancellationToken);
                
                stopwatch.Stop();
                
                result.IsSuccess = true;
                result.GeneratedText = response.ToString();
                result.ExecutionTime = stopwatch.Elapsed;
                
                // 估算Token使用和成本
                result.InputTokens = EstimateTokenCount(promptText);
                result.OutputTokens = EstimateTokenCount(result.GeneratedText);
                result.CostUSD = CalculateCost(result.InputTokens, result.OutputTokens);

                _logger.LogDebug("Prompt执行成功: {AgentName}, 耗时: {ElapsedMs}ms, 成本: ${Cost}", 
                    agentName, stopwatch.ElapsedMilliseconds, result.CostUSD);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                result.IsSuccess = false;
                result.FailureReason = ex.Message;
                result.ExecutionTime = stopwatch.Elapsed;

                _logger.LogError(ex, "Prompt执行失败: {AgentName}, 耗时: {ElapsedMs}ms", 
                    agentName, stopwatch.ElapsedMilliseconds);

                return result;
            }
        }

        public async Task<PromptExecutionResult> ExecutePromptWithOptionsAsync(
            string promptText,
            PromptExecutionOptions options,
            string agentName = "ContentEngineHelper",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(promptText))
                throw new ArgumentException("Prompt文本不能为空", nameof(promptText));

            options ??= new PromptExecutionOptions();

            var stopwatch = Stopwatch.StartNew();
            var result = new PromptExecutionResult();

            try
            {
                // 获取模型定义ID（用于RPM检查）
                var modelDefinitionId = await GetPrimaryModelDefinitionIdAsync(agentName);
                
                // 等待RPM许可（会自动排队）
                if (!string.IsNullOrEmpty(modelDefinitionId))
                {
                    await _rateLimiter.WaitForPermissionAsync(modelDefinitionId, cancellationToken);
                    _logger.LogDebug("获得RPM许可(带选项): {AgentName}, 模型: {ModelId}", agentName, modelDefinitionId);
                }

                var kernel = await _kernelFactory.BuildKernelAsync(agentName);

                if (options.ForceJsonOutput)
                {
                    // 构建包含格式说明的增强 Prompt
                    var enhancedPrompt = ContentEngine.Core.Utils.JsonParsingUtils.BuildEnhancedPrompt(promptText, options.OutputFields ?? new());
                    
                    _logger.LogDebug("执行结构化Prompt: {AgentName}, 原始长度: {OriginalLength}, 增强长度: {EnhancedLength}, 字段数: {FieldCount}", 
                        agentName, promptText.Length, enhancedPrompt.Length, options.OutputFields?.Count ?? 0);
                    
                    // 使用 OpenAI 结构化输出（严格 JSON Schema）
                    var schemaJson = BuildFlatJsonSchema(options.OutputFields ?? new());
                    var responseFormat = ChatResponseFormat.ForJsonSchema(schemaJson);

                    var exec = new OpenAIPromptExecutionSettings
                    {
                        ResponseFormat = responseFormat
                    };

                    var response = await kernel.InvokePromptAsync(enhancedPrompt, new KernelArguments(exec), cancellationToken: cancellationToken);
                    
                    stopwatch.Stop();
                    var text = response.ToString();
                    result.IsSuccess = true;
                    result.GeneratedText = text;
                    result.ExecutionTime = stopwatch.Elapsed;
                    result.InputTokens = EstimateTokenCount(promptText);
                    result.OutputTokens = EstimateTokenCount(text);
                    result.CostUSD = CalculateCost(result.InputTokens, result.OutputTokens);
                    return result;
                }
                else
                {
                    _logger.LogDebug("执行普通Prompt: {AgentName}, 长度: {PromptLength}", agentName, promptText.Length);
                    var response = await kernel.InvokePromptAsync(promptText, cancellationToken: cancellationToken);

                    stopwatch.Stop();
                    var text = response.ToString();
                    result.IsSuccess = true;
                    result.GeneratedText = text;
                    result.ExecutionTime = stopwatch.Elapsed;
                    result.InputTokens = EstimateTokenCount(promptText);
                    result.OutputTokens = EstimateTokenCount(text);
                    result.CostUSD = CalculateCost(result.InputTokens, result.OutputTokens);
                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.IsSuccess = false;
                result.FailureReason = ex.Message;
                result.ExecutionTime = stopwatch.Elapsed;
                _logger.LogError(ex, "Prompt执行失败(带选项): {AgentName}, 耗时: {ElapsedMs}ms", agentName, stopwatch.ElapsedMilliseconds);
                return result;
            }
        }


        private static JsonElement BuildFlatJsonSchema(List<ContentEngine.Core.DataPipeline.Models.FieldDefinition> fields)
        {
            // 构建平坦对象 Schema（严格、禁止额外属性）
            var properties = new Dictionary<string, object?>();
            var required = new List<string>();

            foreach (var f in fields ?? new())
            {
                var type = f.Type switch
                {
                    ContentEngine.Core.DataPipeline.Models.FieldType.Text => "string",
                    ContentEngine.Core.DataPipeline.Models.FieldType.Number => "number",
                    ContentEngine.Core.DataPipeline.Models.FieldType.Boolean => "boolean",
                    ContentEngine.Core.DataPipeline.Models.FieldType.Date => "string", // JSON Schema 中日期通常用 string 表示，可附带 format
                    ContentEngine.Core.DataPipeline.Models.FieldType.Reference => "string",
                    _ => "string"
                };

                properties[f.Name] = new Dictionary<string, object?>
                {
                    ["type"] = type,
                    ["description"] = f.Comment
                };

                if (f.IsRequired)
                {
                    required.Add(f.Name);
                }
            }

            var schema = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = properties,
                ["required"] = required
            };

            // 核心改动：将 schema 对象直接序列化为 JsonElement
            return JsonSerializer.SerializeToElement(schema);
        }

        public async Task<List<PromptExecutionResult>> ExecutePromptsBatchAsync(
            List<string> prompts, 
            string agentName = "ContentEngineHelper", 
            int maxConcurrency = 5, 
            CancellationToken cancellationToken = default)
        {
            if (prompts == null || !prompts.Any())
                return new List<PromptExecutionResult>();

            _logger.LogInformation("开始批量执行Prompt: {PromptCount}个, 最大并发: {MaxConcurrency}", 
                prompts.Count, maxConcurrency);

            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = prompts.Select(async prompt =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await ExecutePromptAsync(prompt, agentName, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            
            var successCount = results.Count(r => r.IsSuccess);
            var totalCost = results.Sum(r => r.CostUSD);
            
            _logger.LogInformation("批量Prompt执行完成: 成功 {SuccessCount}/{TotalCount}, 总成本: ${TotalCost}", 
                successCount, prompts.Count, totalCost);

            return results.ToList();
        }

        public async Task<decimal> EstimateCostAsync(string promptText, string agentName = "ContentEngineHelper")
        {
            if (string.IsNullOrWhiteSpace(promptText))
                return 0;

            var inputTokens = EstimateTokenCount(promptText);
            
            // 估算输出token数量（通常是输入的20-50%）
            var estimatedOutputTokens = (int)(inputTokens * 0.35);
            
            return CalculateCost(inputTokens, estimatedOutputTokens);
        }

        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // 简单的Token估算：
            // 英文: 约4个字符 = 1个Token
            // 中文: 约1个字符 = 1个Token
            
            var chineseCharCount = text.Count(c => c >= 0x4e00 && c <= 0x9fff);
            var otherCharCount = text.Length - chineseCharCount;
            
            var estimatedTokens = chineseCharCount + (otherCharCount / 4);
            
            return Math.Max(1, estimatedTokens);
        }

        private decimal CalculateCost(int inputTokens, int outputTokens)
        {
            // 基于常见的AI模型定价（如GPT-4）估算成本
            // 输入Token: $0.03 / 1K tokens
            // 输出Token: $0.06 / 1K tokens
            
            const decimal inputCostPer1K = 0.03m;
            const decimal outputCostPer1K = 0.06m;
            
            var inputCost = (inputTokens / 1000m) * inputCostPer1K;
            var outputCost = (outputTokens / 1000m) * outputCostPer1K;
            
            return inputCost + outputCost;
        }

        /// <summary>
        /// 获取Agent的主要模型定义ID（用于RPM检查）
        /// </summary>
        /// <param name="agentName">Agent名称</param>
        /// <returns>模型定义ID，如果获取失败返回null</returns>
        private async Task<string?> GetPrimaryModelDefinitionIdAsync(string agentName)
        {
            try
            {
                var agentConfig = await _agentConfigLoader.LoadConfigAsync(agentName);
                
                // 获取第一个模型配置
                if (agentConfig.Models?.Any() == true)
                {
                    var firstModelEntry = agentConfig.Models.First();
                    var modelDefinitionId = firstModelEntry.Value;
                    
                    if (!string.IsNullOrWhiteSpace(modelDefinitionId))
                    {
                        return modelDefinitionId;
                    }
                }
                
                _logger.LogDebug("无法从Agent {AgentName} 获取模型定义ID: 没有配置模型", agentName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "获取Agent {AgentName} 模型定义ID时发生错误", agentName);
                return null;
            }
        }
    }
} 