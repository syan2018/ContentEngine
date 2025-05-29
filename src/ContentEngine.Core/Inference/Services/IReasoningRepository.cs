using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理相关数据的仓储接口
    /// </summary>
    public interface IReasoningRepository
    {
        // 定义管理
        Task<ReasoningTransactionDefinition> CreateDefinitionAsync(ReasoningTransactionDefinition definition);
        Task<ReasoningTransactionDefinition?> GetDefinitionByIdAsync(string definitionId);
        Task<List<ReasoningTransactionDefinition>> GetAllDefinitionsAsync();
        Task<ReasoningTransactionDefinition> UpdateDefinitionAsync(ReasoningTransactionDefinition definition);
        Task<bool> DeleteDefinitionAsync(string definitionId);

        // 实例管理
        Task<ReasoningTransactionInstance> CreateInstanceAsync(ReasoningTransactionInstance instance);
        Task<ReasoningTransactionInstance?> GetInstanceByIdAsync(string instanceId);
        Task<List<ReasoningTransactionInstance>> GetInstancesAsync(string? definitionId = null, TransactionStatus? status = null);
        Task<ReasoningTransactionInstance> UpdateInstanceAsync(ReasoningTransactionInstance instance);
        Task<bool> DeleteInstanceAsync(string instanceId);
    }
} 