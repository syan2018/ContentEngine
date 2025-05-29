using ContentEngine.Core.Inference.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ContentEngine.Core.Inference.Services
{
    /// <summary>
    /// 推理引擎服务接口，负责管理推理事务的定义和执行
    /// </summary>
    public interface IReasoningService
    {
        #region 推理事务定义管理

        /// <summary>
        /// 创建新的推理事务定义
        /// </summary>
        /// <param name="definition">推理事务定义</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的推理事务定义</returns>
        Task<ReasoningTransactionDefinition> CreateDefinitionAsync(ReasoningTransactionDefinition definition, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有推理事务定义
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务定义列表</returns>
        Task<List<ReasoningTransactionDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取推理事务定义
        /// </summary>
        /// <param name="definitionId">定义ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务定义，如果不存在返回null</returns>
        Task<ReasoningTransactionDefinition?> GetDefinitionByIdAsync(string definitionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新推理事务定义
        /// </summary>
        /// <param name="definition">更新的推理事务定义</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的推理事务定义</returns>
        Task<ReasoningTransactionDefinition> UpdateDefinitionAsync(ReasoningTransactionDefinition definition, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除推理事务定义
        /// </summary>
        /// <param name="definitionId">定义ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);

        #endregion

        #region 推理事务执行

        /// <summary>
        /// 执行推理事务
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例</returns>
        Task<ReasoningTransactionInstance> ExecuteTransactionAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 恢复执行失败的推理事务
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例</returns>
        Task<ReasoningTransactionInstance> ResumeTransactionAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 暂停正在执行的推理事务
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否暂停成功</returns>
        Task<bool> PauseTransactionAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消正在执行的推理事务
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否取消成功</returns>
        Task<bool> CancelTransactionAsync(string instanceId, CancellationToken cancellationToken = default);

        #endregion

        #region 推理事务实例管理

        /// <summary>
        /// 获取所有推理事务实例
        /// </summary>
        /// <param name="definitionId">定义ID（可选，用于筛选）</param>
        /// <param name="status">状态（可选，用于筛选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例列表</returns>
        Task<List<ReasoningTransactionInstance>> GetInstancesAsync(
            string? definitionId = null, 
            TransactionStatus? status = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取推理事务实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推理事务实例，如果不存在返回null</returns>
        Task<ReasoningTransactionInstance?> GetInstanceByIdAsync(string instanceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除推理事务实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteInstanceAsync(string instanceId, CancellationToken cancellationToken = default);

        #endregion

        #region 成本和执行预估

        /// <summary>
        /// 预估推理事务的执行成本
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预估成本（美元）</returns>
        Task<decimal> EstimateExecutionCostAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 预估推理事务的执行时间
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预估执行时间</returns>
        Task<TimeSpan> EstimateExecutionTimeAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 预估推理事务的组合数量
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="executionParams">执行参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预估组合数量</returns>
        Task<int> EstimateCombinationCountAsync(
            string definitionId, 
            Dictionary<string, object>? executionParams = null, 
            CancellationToken cancellationToken = default);

        #endregion

        #region 实时组合生成

        /// <summary>
        /// 根据推理事务定义实时生成输入组合（不依赖执行记录）
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>输入组合列表</returns>
        Task<List<ReasoningInputCombination>> GenerateInputCombinationsAsync(string definitionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行单个组合
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="combinationId">组合ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        Task<ReasoningOutputItem> ExecuteCombinationAsync(string definitionId, string combinationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取组合的输出结果
        /// </summary>
        /// <param name="definitionId">推理事务定义ID</param>
        /// <param name="combinationId">组合ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>输出结果，如果不存在返回null</returns>
        Task<ReasoningOutputItem?> GetOutputForCombinationAsync(string definitionId, string combinationId, CancellationToken cancellationToken = default);

        #endregion
    }
} 