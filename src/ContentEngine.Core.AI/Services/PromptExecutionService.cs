using ConfigurableAIProvider.Services.Factories;
using ContentEngine.Core.Inference.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContentEngine.Core.AI.Services
{
    /// <summary>
    /// Prompt执行服务实现
    /// </summary>
    public class PromptExecutionService : IPromptExecutionService
    {
        private readonly IAIKernelFactory _kernelFactory;
        private readonly ILogger<PromptExecutionService> _logger;

        public PromptExecutionService(
            IAIKernelFactory kernelFactory,
            ILogger<PromptExecutionService> logger)
        {
            _kernelFactory = kernelFactory;
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
    }
} 