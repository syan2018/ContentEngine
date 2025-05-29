using ContentEngine.Core.Inference.Models;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理事务定义管理服务接口
    /// 专门负责推理事务定义的创建、读取、更新、删除操作
    /// </summary>
    public interface IReasoningDefinitionService
    {
        /// <summary>
        /// 创建新的推理事务定义
        /// </summary>
        /// <param name="definition">推理事务定义</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的推理事务定义</returns>
        Task<ReasoningTransactionDefinition> CreateDefinitionAsync(
            ReasoningTransactionDefinition definition, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有推理事务定义
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务定义列表</returns>
        Task<List<ReasoningTransactionDefinition>> GetAllDefinitionsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取推理事务定义
        /// </summary>
        /// <param name="definitionId">定义ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务定义，如果不存在返回null</returns>
        Task<ReasoningTransactionDefinition?> GetDefinitionByIdAsync(
            string definitionId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新推理事务定义
        /// </summary>
        /// <param name="definition">更新的推理事务定义</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的推理事务定义</returns>
        Task<ReasoningTransactionDefinition> UpdateDefinitionAsync(
            ReasoningTransactionDefinition definition, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除推理事务定义
        /// </summary>
        /// <param name="definitionId">定义ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteDefinitionAsync(
            string definitionId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证推理事务定义的有效性
        /// </summary>
        /// <param name="definition">推理事务定义</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        Task<DefinitionValidationResult> ValidateDefinitionAsync(
            ReasoningTransactionDefinition definition, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 定义验证结果
    /// </summary>
    public class DefinitionValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
} 