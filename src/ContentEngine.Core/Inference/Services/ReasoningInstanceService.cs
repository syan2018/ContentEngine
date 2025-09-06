using ContentEngine.Core.Inference.Models;
using ContentEngine.Core.Storage;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理事务实例管理服务实现
    /// </summary>
    public class ReasoningInstanceService : IReasoningInstanceService
    {
        private readonly LiteDbContext _dbContext;
        private readonly IReasoningDefinitionService _definitionService;
        private readonly ILogger<ReasoningInstanceService> _logger;

        public ReasoningInstanceService(
            LiteDbContext dbContext,
            IReasoningDefinitionService definitionService,
            ILogger<ReasoningInstanceService> logger)
        {
            _dbContext = dbContext;
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

            try
            {
                _dbContext.ReasoningInstances.Insert(instance);
                _logger.LogInformation("创建推理事务实例: {InstanceId} (定义: {DefinitionId})", instance.InstanceId, definitionId);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建推理事务实例失败: {InstanceId}", instance.InstanceId);
                throw;
            }
        }

        public async Task<List<ReasoningTransactionInstance>> GetInstancesAsync(
            string? definitionId = null, 
            TransactionStatus? status = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbContext.ReasoningInstances.Query();

                if (!string.IsNullOrWhiteSpace(definitionId))
                {
                    query = query.Where(x => x.DefinitionId == definitionId);
                }

                if (status.HasValue)
                {
                    query = query.Where(x => x.Status == status.Value);
                }

                var result = query.OrderByDescending(x => x.StartedAt).ToList();
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推理事务实例列表失败");
                throw;
            }
        }

        public async Task<ReasoningTransactionInstance?> GetInstanceByIdAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return null;

            try
            {
                var result = _dbContext.ReasoningInstances
                    .Query()
                    .Where(x => x.InstanceId == instanceId)
                    .FirstOrDefault();
                
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推理事务实例失败: {InstanceId}", instanceId);
                throw;
            }
        }

        public async Task<ReasoningTransactionInstance> UpdateInstanceAsync(
            ReasoningTransactionInstance instance, 
            CancellationToken cancellationToken = default)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            try
            {
                _dbContext.ReasoningInstances.Update(instance);
                _logger.LogInformation("更新推理事务实例: {InstanceId}", instance.InstanceId);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新推理事务实例失败: {InstanceId}", instance.InstanceId);
                throw;
            }
        }

        public async Task<bool> DeleteInstanceAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return false;

            try
            {
                // 使用条件删除
                var deletedCount = _dbContext.ReasoningInstances.DeleteMany(x => x.InstanceId == instanceId);
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("删除推理事务实例: {InstanceId}", instanceId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除推理事务实例失败: {InstanceId}", instanceId);
                throw;
            }
            // 考虑关联删除裸 Definition
            
        }

        public async Task<InstanceBasicStats> GetInstanceBasicStatsAsync(
            string instanceId, 
            CancellationToken cancellationToken = default)
        {
            var instance = await GetInstanceByIdAsync(instanceId, cancellationToken);
            if (instance == null)
                throw new InvalidOperationException($"推理事务实例不存在: {instanceId}");

            return new InstanceBasicStats
            {
                InstanceId = instanceId,
                DefinitionId = instance.DefinitionId,
                StartedAt = instance.StartedAt,
                CompletedAt = instance.CompletedAt,
                FinalStatus = instance.Status,
                TotalCombinations = instance.Metrics.TotalCombinations,
                SuccessfulOutputs = instance.Metrics.SuccessfulOutputs,
                FailedOutputs = instance.Metrics.FailedOutputs,
                ActualCostUSD = instance.Metrics.ActualCostUSD,
                TotalExecutionTime = instance.Metrics.ElapsedTime,
                CriticalErrors = instance.Errors.Where(e => !e.IsRetriable).ToList()
            };
        }
    }
} 