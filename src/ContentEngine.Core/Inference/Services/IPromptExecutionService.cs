using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 执行单个Prompt的AI调用的结果
    /// </summary>
    public class PromptExecutionResult
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// AI生成的文本结果
        /// </summary>
        public string GeneratedText { get; set; } = string.Empty;

        /// <summary>
        /// 失败原因（如果失败）
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// 执行耗时
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// 消耗的成本（美元）
        /// </summary>
        public decimal CostUSD { get; set; }

        /// <summary>
        /// 输入Token数量
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// 输出Token数量
        /// </summary>
        public int OutputTokens { get; set; }
    }

    /// <summary>
    /// Prompt执行服务接口，用于执行AI推理调用
    /// </summary>
    public interface IPromptExecutionService
    {
        /// <summary>
        /// 执行单个Prompt
        /// </summary>
        /// <param name="promptText">Prompt文本</param>
        /// <param name="agentName">使用的Agent名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        Task<PromptExecutionResult> ExecutePromptAsync(
            string promptText, 
            string agentName = "ContentEngineHelper", 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量执行多个Prompt
        /// </summary>
        /// <param name="prompts">Prompt列表</param>
        /// <param name="agentName">使用的Agent名称</param>
        /// <param name="maxConcurrency">最大并发数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果列表</returns>
        Task<List<PromptExecutionResult>> ExecutePromptsBatchAsync(
            List<string> prompts, 
            string agentName = "ContentEngineHelper", 
            int maxConcurrency = 5, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 预估执行成本
        /// </summary>
        /// <param name="promptText">Prompt文本</param>
        /// <param name="agentName">使用的Agent名称</param>
        /// <returns>预估成本（美元）</returns>
        Task<decimal> EstimateCostAsync(string promptText, string agentName = "ContentEngineHelper");

        /// <summary>
        /// 估算Token数量
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>Token数量</returns>
        int EstimateTokenCount(string text);
    }
} 