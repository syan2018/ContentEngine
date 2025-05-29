using ContentEngine.Core.Inference.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ContentEngine.Core.Inference.Extensions
{
    /// <summary>
    /// Inference模块的依赖注入扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加Inference模块的所有服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInferenceServices(this IServiceCollection services)
        {
            // 注册仓储服务
            services.AddScoped<IReasoningRepository, ReasoningRepository>();

            // 注册Query处理服务
            services.AddScoped<IQueryProcessingService, QueryProcessingService>();

            // 注册模块化的推理服务
            services.AddScoped<IReasoningDefinitionService, ReasoningDefinitionService>();
            services.AddScoped<IReasoningInstanceService, ReasoningInstanceService>();
            services.AddScoped<IReasoningExecutionService, ReasoningExecutionService>();
            services.AddScoped<IReasoningEstimationService, ReasoningEstimationService>();
            services.AddScoped<IReasoningCombinationService, ReasoningCombinationService>();


            return services;
        }

        /// <summary>
        /// 添加Inference模块的核心服务（不包括AI相关服务）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInferenceCoreServices(this IServiceCollection services)
        {
            // 注册仓储服务
            services.AddScoped<IReasoningRepository, ReasoningRepository>();

            // 注册Query处理服务
            services.AddScoped<IQueryProcessingService, QueryProcessingService>();

            // 注册定义和实例管理服务
            services.AddScoped<IReasoningDefinitionService, ReasoningDefinitionService>();
            services.AddScoped<IReasoningInstanceService, ReasoningInstanceService>();

            return services;
        }

        /// <summary>
        /// 添加Inference模块的执行相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInferenceExecutionServices(this IServiceCollection services)
        {
            // 注册执行相关服务
            services.AddScoped<IReasoningExecutionService, ReasoningExecutionService>();
            services.AddScoped<IReasoningEstimationService, ReasoningEstimationService>();
            services.AddScoped<IReasoningCombinationService, ReasoningCombinationService>();

            return services;
        }

        /// <summary>
        /// 添加Inference模块的预估服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInferenceEstimationServices(this IServiceCollection services)
        {
            services.AddScoped<IReasoningEstimationService, ReasoningEstimationService>();
            return services;
        }

        /// <summary>
        /// 添加Inference模块的组合处理服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInferenceCombinationServices(this IServiceCollection services)
        {
            services.AddScoped<IReasoningCombinationService, ReasoningCombinationService>();
            return services;
        }
    }
} 