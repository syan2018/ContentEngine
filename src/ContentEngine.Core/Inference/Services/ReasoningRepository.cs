using ContentEngine.Core.Inference.Models;
using LiteDB;
using ContentEngine.Core.Storage;
using Microsoft.Extensions.Logging;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理相关数据的仓储实现
    /// </summary>
    public class ReasoningRepository : IReasoningRepository
    {
        private readonly LiteDbContext _context;
        private readonly ILogger<ReasoningRepository> _logger;

        public ReasoningRepository(LiteDbContext context, ILogger<ReasoningRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region 定义管理

        public async Task<ReasoningTransactionDefinition> CreateDefinitionAsync(ReasoningTransactionDefinition definition)
        {
            try
            {
                _context.ReasoningDefinitions.Insert(definition);
                _logger.LogInformation("创建推理事务定义: {DefinitionName} (ID: {DefinitionId})", definition.Name, definition.Id);
                return definition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建推理事务定义失败: {DefinitionId}", definition.Id);
                throw;
            }
        }

        public async Task<ReasoningTransactionDefinition?> GetDefinitionByIdAsync(string definitionId)
        {
            try
            {
                var result = _context.ReasoningDefinitions
                    .Query()
                    .Where(x => x.Id == definitionId)
                    .FirstOrDefault();
                
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推理事务定义失败: {DefinitionId}", definitionId);
                throw;
            }
        }

        public async Task<List<ReasoningTransactionDefinition>> GetAllDefinitionsAsync()
        {
            try
            {
                var result = _context.ReasoningDefinitions
                    .Query()
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
                
                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有推理事务定义失败");
                throw;
            }
        }

        public async Task<ReasoningTransactionDefinition> UpdateDefinitionAsync(ReasoningTransactionDefinition definition)
        {
            try
            {
                definition.UpdatedAt = DateTime.UtcNow;
                _context.ReasoningDefinitions.Update(definition);
                _logger.LogInformation("更新推理事务定义: {DefinitionName} (ID: {DefinitionId})", definition.Name, definition.Id);
                return definition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新推理事务定义失败: {DefinitionId}", definition.Id);
                throw;
            }
        }

        public async Task<bool> DeleteDefinitionAsync(string definitionId)
        {
            try
            {
                // 首先检查是否有关联的实例
                var hasInstances = _context.ReasoningInstances
                    .Query()
                    .Where(x => x.DefinitionId == definitionId)
                    .Exists();

                if (hasInstances)
                {
                    throw new InvalidOperationException("无法删除存在关联实例的推理事务定义，请先删除相关实例");
                }

                // 使用条件删除
                var deletedCount = _context.ReasoningDefinitions.DeleteMany(x => x.Id == definitionId);
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("删除推理事务定义: {DefinitionId}", definitionId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除推理事务定义失败: {DefinitionId}", definitionId);
                throw;
            }
        }

        #endregion

        #region 实例管理

        public async Task<ReasoningTransactionInstance> CreateInstanceAsync(ReasoningTransactionInstance instance)
        {
            try
            {
                _context.ReasoningInstances.Insert(instance);
                _logger.LogInformation("创建推理事务实例: {InstanceId}", instance.InstanceId);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建推理事务实例失败: {InstanceId}", instance.InstanceId);
                throw;
            }
        }

        public async Task<ReasoningTransactionInstance?> GetInstanceByIdAsync(string instanceId)
        {
            try
            {
                var result = _context.ReasoningInstances
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

        public async Task<List<ReasoningTransactionInstance>> GetInstancesAsync(string? definitionId = null, TransactionStatus? status = null)
        {
            try
            {
                var query = _context.ReasoningInstances.Query();

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

        public async Task<ReasoningTransactionInstance> UpdateInstanceAsync(ReasoningTransactionInstance instance)
        {
            try
            {
                _context.ReasoningInstances.Update(instance);
                _logger.LogInformation("更新推理事务实例: {InstanceId}", instance.InstanceId);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新推理事务实例失败: {InstanceId}", instance.InstanceId);
                throw;
            }
        }

        public async Task<bool> DeleteInstanceAsync(string instanceId)
        {
            try
            {
                // 使用条件删除
                var deletedCount = _context.ReasoningInstances.DeleteMany(x => x.InstanceId == instanceId);
                
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
        }

        #endregion
    }
} 