using ContentEngine.Core.Inference.Models;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理预估服务实现
    /// </summary>
    public class ReasoningEstimationService : IReasoningEstimationService
    {
        private readonly IReasoningDefinitionService _definitionService;
        private readonly IQueryProcessingService _queryProcessingService;
        private readonly IPromptExecutionService _promptExecutionService;
        private readonly ILogger<ReasoningEstimationService> _logger;

        public ReasoningEstimationService(
            IReasoningDefinitionService definitionService,
            IQueryProcessingService queryProcessingService,
            IPromptExecutionService promptExecutionService,
            ILogger<ReasoningEstimationService> logger)
        {
            _definitionService = definitionService;
            _queryProcessingService = queryProcessingService;
            _promptExecutionService = promptExecutionService;
            _logger = logger;
        }

        public async Task<decimal> EstimateExecutionCostAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
                return 0;

            // 使用QueryProcessingService获取更准确的组合数量预估
            var estimatedCombinations = await _queryProcessingService.EstimateCombinationCountAsync(definition, cancellationToken);
            
            // 每个组合的预估成本
            var costPerCombination = await _promptExecutionService.EstimateCostAsync(
                definition.PromptTemplate.TemplateContent, 
                "ContentEngineHelper");

            var totalCost = costPerCombination * estimatedCombinations;
            
            _logger.LogDebug("预估执行成本: 组合数={EstimatedCombinations}, 单次成本=${CostPerCombination:F4}, 总成本=${TotalCost:F2}", 
                estimatedCombinations, costPerCombination, totalCost);

            return totalCost;
        }

        public async Task<TimeSpan> EstimateExecutionTimeAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
                return TimeSpan.Zero;

            var estimatedCombinations = await _queryProcessingService.EstimateCombinationCountAsync(definition, cancellationToken);
            
            // 考虑并发执行的影响
            var maxConcurrency = definition.ExecutionConstraints.MaxConcurrentAICalls;
            var secondsPerCombination = 2; // 假设每个组合平均需要2秒处理
            
            var totalSeconds = (estimatedCombinations * secondsPerCombination) / maxConcurrency;
            var estimatedTime = TimeSpan.FromSeconds(totalSeconds);
            
            _logger.LogDebug("预估执行时间: 组合数={EstimatedCombinations}, 并发数={MaxConcurrency}, 预估时间={EstimatedTime}", 
                estimatedCombinations, maxConcurrency, estimatedTime);

            return estimatedTime;
        }

        public async Task<int> EstimateCombinationCountAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
                return 0;

            return await _queryProcessingService.EstimateCombinationCountAsync(definition, cancellationToken);
        }

        public async Task<DetailedEstimation> GetDetailedEstimationAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default)
        {
            var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
            if (definition == null)
                throw new InvalidOperationException($"推理事务定义不存在: {definitionId}");

            var estimatedCombinations = await EstimateCombinationCountAsync(definitionId, executionParams, cancellationToken);
            var estimatedCost = await EstimateExecutionCostAsync(definitionId, executionParams, cancellationToken);
            var estimatedTime = await EstimateExecutionTimeAsync(definitionId, executionParams, cancellationToken);

            var breakdown = new EstimationBreakdown
            {
                CostPerCombination = estimatedCombinations > 0 ? estimatedCost / estimatedCombinations : 0,
                MaxConcurrency = definition.ExecutionConstraints.MaxConcurrentAICalls,
                SecondsPerCombination = 2.0
            };

            // 获取查询记录数量
            foreach (var query in definition.QueryDefinitions)
            {
                try
                {
                    var stats = await _queryProcessingService.GetQueryStatisticsAsync(query, cancellationToken);
                    breakdown.QueryRecordCounts[query.OutputViewName] = stats.FilteredRecords;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取查询 {ViewName} 统计信息失败", query.OutputViewName);
                    breakdown.QueryRecordCounts[query.OutputViewName] = 0;
                }
            }

            return new DetailedEstimation
            {
                DefinitionId = definitionId,
                DefinitionName = definition.Name,
                EstimatedCombinations = estimatedCombinations,
                EstimatedCostUSD = estimatedCost,
                EstimatedTime = estimatedTime,
                Breakdown = breakdown,
                Assumptions = new List<string>
                {
                    "假设每个组合平均处理时间为2秒",
                    "基于当前数据量进行预估",
                    "不包括网络延迟和系统负载影响"
                }
            };
        }

        public async Task<List<BatchEstimationResult>> BatchEstimateAsync(
            List<string> definitionIds, 
            CancellationToken cancellationToken = default)
        {
            var results = new List<BatchEstimationResult>();

            foreach (var definitionId in definitionIds)
            {
                try
                {
                    var definition = await _definitionService.GetDefinitionByIdAsync(definitionId, cancellationToken);
                    if (definition == null)
                    {
                        results.Add(new BatchEstimationResult
                        {
                            DefinitionId = definitionId,
                            DefinitionName = "未知",
                            IsSuccess = false,
                            ErrorMessage = "推理事务定义不存在"
                        });
                        continue;
                    }

                    var estimatedCombinations = await EstimateCombinationCountAsync(definitionId, cancellationToken: cancellationToken);
                    var estimatedCost = await EstimateExecutionCostAsync(definitionId, cancellationToken: cancellationToken);
                    var estimatedTime = await EstimateExecutionTimeAsync(definitionId, cancellationToken: cancellationToken);

                    results.Add(new BatchEstimationResult
                    {
                        DefinitionId = definitionId,
                        DefinitionName = definition.Name,
                        IsSuccess = true,
                        EstimatedCombinations = estimatedCombinations,
                        EstimatedCostUSD = estimatedCost,
                        EstimatedTime = estimatedTime
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new BatchEstimationResult
                    {
                        DefinitionId = definitionId,
                        DefinitionName = "未知",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return results;
        }

        public async Task<EstimationAccuracyStats> GetEstimationAccuracyAsync(
            string? definitionId = null, 
            CancellationToken cancellationToken = default)
        {
            // 这里应该从历史执行记录中计算准确性统计
            // 为了简化，返回模拟数据
            return await Task.FromResult(new EstimationAccuracyStats
            {
                TotalExecutions = 0,
                CostAccuracyPercentage = 85.0,
                TimeAccuracyPercentage = 78.0,
                CombinationAccuracyPercentage = 92.0,
                AverageCostDeviation = 0.15m,
                AverageTimeDeviation = TimeSpan.FromMinutes(5),
                AverageCombinationDeviation = 0.08,
                LastUpdated = DateTime.UtcNow
            });
        }
    }
} 