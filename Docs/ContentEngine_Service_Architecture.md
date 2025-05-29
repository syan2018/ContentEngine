# ContentEngine 项目服务架构文档

## 概述

ContentEngine 是一个基于 .NET 9 和 Blazor Server 的智能内容管理系统，采用模块化架构设计，将功能按职责清晰地分布在不同的服务层中。本文档详细描述了整个项目的服务分工、职责划分和依赖关系。

## 项目架构总览

```
ContentEngine.WebApp (UI层)
    ↓ 依赖
ContentEngine.Core.AI (AI应用服务层)
    ↓ 依赖
ContentEngine.Core (核心业务层)
    ↓ 依赖
ConfigurableAIProvider (AI基础设施层)
```

## 模块详细分工

### 1. ContentEngine.WebApp (UI层)

**职责**：用户界面和交互逻辑
- **技术栈**：Blazor Server, MudBlazor UI组件库
- **主要功能**：
  - 提供Web界面和用户交互
  - 处理用户输入和表单验证
  - 调用业务服务完成具体功能
  - 国际化支持（中英文）

**核心组件**：
- `Components/Pages/` - 各功能页面组件
- `Components/Shared/` - 共享UI组件
- `Resources/` - 本地化资源文件

### 2. ContentEngine.Core (核心业务层)

**职责**：核心业务逻辑和数据管理
- **技术栈**：.NET 9, LiteDB
- **主要模块**：

#### 2.1 DataPipeline 模块
**职责**：数据结构定义和数据实例管理

**服务列表**：
- **`ISchemaDefinitionService`** - Schema定义管理
  - 创建、读取、更新、删除Schema定义
  - Schema验证和完整性检查
  - 支持动态字段定义

- **`IDataEntryService`** - 数据实例管理
  - 基于Schema创建数据实例
  - 数据的CRUD操作
  - 分页查询和统计
  - 数据验证

- **`IFileConversionService`** - 文件转换服务
  - 支持多种文件格式转换为文本
  - 网页内容抓取
  - 文件类型检测和验证

#### 2.2 Inference 模块
**职责**：AI推理事务管理和执行

**服务列表**：
- **`IReasoningDefinitionService`** - 推理定义管理
  - 推理事务定义的CRUD操作
  - 定义验证（Prompt模板、查询定义等）
  - 支持复杂的推理流程定义

- **`IReasoningInstanceService`** - 推理实例管理
  - 推理事务实例的生命周期管理
  - 实例状态跟踪和进度监控
  - 实例统计信息收集

- **`IReasoningExecutionService`** - 推理执行控制
  - 推理事务的执行、暂停、恢复、取消
  - 执行前检查和预验证
  - 批量操作支持
  - 并发控制和资源管理

- **`IReasoningEstimationService`** - 成本和时间预估
  - 执行成本预估（基于Token使用）
  - 执行时间预估
  - 组合数量预估
  - 预估准确性统计

- **`IReasoningCombinationService`** - 组合生成和执行
  - 输入数据组合生成
  - 单个组合的执行
  - 批量组合处理
  - 失败重试机制

- **`IQueryProcessingService`** - 查询处理服务
  - 查询定义执行
  - 数据组合生成
  - 查询统计信息
  - 数据预处理

- **`IPromptExecutionService`** - Prompt执行接口
  - 定义AI调用的标准接口
  - 执行结果封装
  - 成本和性能监控

#### 2.3 Storage 模块
**职责**：数据持久化

**服务列表**：
- **`LiteDbContext`** - 数据库上下文
  - LiteDB数据库连接管理
  - 集合访问和映射配置
  - 索引管理和优化

### 3. ContentEngine.Core.AI (AI应用服务层)

**职责**：AI功能的业务应用实现
- **技术栈**：.NET 9, Microsoft Semantic Kernel

**服务列表**：
- **`ISchemaSuggestionService`** - Schema建议服务
  - 基于自然语言描述生成Schema建议
  - Schema字段优化和完善
  - 支持样本数据分析

- **`IDataStructuringService`** - 数据结构化服务
  - 从非结构化数据提取结构化信息
  - 支持多种提取模式（一对一、一对多）
  - 数据验证和格式化
  - AI输出解析和转换

- **`PromptExecutionService`** - Prompt执行实现
  - 实现`IPromptExecutionService`接口
  - 通过Semantic Kernel执行AI调用
  - 成本计算和性能监控
  - 批量执行支持

### 4. ConfigurableAIProvider (AI基础设施层)

**职责**：AI服务的配置化管理
- **技术栈**：Microsoft Semantic Kernel, YAML配置

**核心组件**：

#### 4.1 配置管理
- **`IConnectionProvider`** - 连接配置管理
  - AI服务连接信息管理
  - 支持多种AI提供商（OpenAI、Azure OpenAI、Ollama等）
  - 环境变量和配置文件支持

- **`IModelProvider`** - 模型配置管理
  - AI模型定义和映射
  - 模型参数配置
  - 逻辑模型到物理模型的映射

- **`IAgentConfigLoader`** - Agent配置加载
  - Agent配置文件加载和解析
  - 插件引用管理
  - 环境特定配置覆盖

#### 4.2 服务工厂
- **`IAIKernelFactory`** - Kernel工厂
  - 根据Agent配置创建Semantic Kernel实例
  - 插件动态加载
  - AI服务配置和注册

#### 4.3 服务配置器
- **`IAIServiceConfigurator`** - AI服务配置器接口
  - `OpenAIServiceConfigurator` - OpenAI服务配置
  - `AzureOpenAIServiceConfigurator` - Azure OpenAI服务配置
  - `OllamaServiceConfigurator` - Ollama服务配置

## 依赖注入配置

### 服务注册策略

项目采用扩展方法模式简化服务注册：

```csharp
// Program.cs 中的服务注册
builder.Services.AddConfigurableAIProvider(builder.Configuration);
builder.Services.AddInferenceServices();
builder.Services.AddScoped<ISchemaDefinitionService, SchemaDefinitionService>();
builder.Services.AddScoped<IDataEntryService, DataEntryService>();
builder.Services.AddScoped<ISchemaSuggestionService, SchemaSuggestionService>();
builder.Services.AddScoped<IDataStructuringService, DataStructuringService>();
builder.Services.AddHttpClient<IFileConversionService, FileConversionService>();
```

### 扩展方法

#### Inference模块扩展方法
- `AddInferenceServices()` - 注册所有Inference服务
- `AddInferenceCoreServices()` - 仅注册核心服务（不含AI相关）
- `AddInferenceExecutionServices()` - 仅注册执行相关服务
- `AddInferenceEstimationServices()` - 仅注册预估服务
- `AddInferenceCombinationServices()` - 仅注册组合处理服务

#### ConfigurableAIProvider扩展方法
- `AddConfigurableAIProvider()` - 注册AI基础设施服务

## 服务生命周期管理

### 生命周期策略
- **Singleton**：`LiteDbContext`, AI配置提供者
- **Scoped**：业务服务、AI应用服务
- **Transient**：轻量级工具类

### 资源管理
- 数据库连接通过`LiteDbContext`统一管理
- AI Kernel实例按需创建，支持缓存
- HTTP客户端通过`IHttpClientFactory`管理

## 数据流和交互模式

### 典型数据流
1. **UI层** 接收用户输入
2. **AI应用服务层** 处理AI相关业务逻辑
3. **核心业务层** 执行数据操作和业务规则
4. **AI基础设施层** 提供AI服务支持
5. **存储层** 持久化数据

### 服务间通信
- 通过依赖注入实现服务解耦
- 使用接口定义服务契约
- 异步编程模式支持高并发

