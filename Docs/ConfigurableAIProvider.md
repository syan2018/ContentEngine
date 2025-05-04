# ConfigurableAIProvider 模块文档

## 1. 概述

`ConfigurableAIProvider` 是一个 .NET 类库，旨在为应用程序（特别是 `ContentEngine`）提供一种灵活、可配置的方式来创建和管理 Semantic Kernel (`IKernel`) 实例。它允许开发者通过外部配置文件（YAML）定义 AI 服务连接、模型选择和插件加载，从而将 AI 相关的配置与应用程序代码解耦。

## 2. 核心概念

### 2.1. 分层配置

模块采用分层的配置策略，以提高灵活性和安全性：

*   **`connections.yaml` (基础连接配置)**: 定义所有可用的 AI 服务连接（如 OpenAI, Azure OpenAI, Ollama 等）。此文件通常包含非敏感信息，如服务类型、端点（对于 Azure/Ollama）或基础 URL。**敏感信息（如 API Key）应使用占位符**。
*   **`connections.<environment>.yaml` (环境特定覆盖)**: 可选文件，用于覆盖特定环境（如 `Development`, `Production`）的基础连接配置。例如，开发环境可能使用不同的 API Key 或模型。
*   **`agent.yaml` (Agent/Kernel 配置)**: 定义特定 Agent 或 Kernel 实例的配置。它会引用 `connections.yaml` 中定义的连接名称，并指定要使用的模型（服务 ID）、要加载的插件等。
*   **`agent.<environment>.yaml` (Agent 环境特定覆盖)**: 可选文件，用于覆盖特定 Agent 在特定环境下的配置。
*   **`appsettings.json`**: 用于配置 `ConfigurableAIProvider` 模块本身的行为，例如配置文件路径和当前环境名称。

### 2.2. 占位符解析

为了安全地处理 API Key 等敏感信息，模块支持在 YAML 配置文件中使用占位符，格式为 `{{ENVIRONMENT_VARIABLE_NAME}}`。在加载配置时，`ConnectionProvider` 会自动查找匹配的环境变量，并将其值替换到相应的配置项中。如果环境变量未设置，将会抛出错误。

## 3. 关键组件

### 3.1. 配置模型 (`Configuration/`)

*   **`ConfigurableAIOptions`**: 用于从 `appsettings.json` 加载模块设置（如配置文件路径、环境名称）。
*   **`ConnectionsConfig`**: 表示 `connections.yaml` 文件的结构，包含一个连接名称到 `ConnectionConfig` 的字典。
*   **`ConnectionConfig`**: 定义单个 AI 服务连接的详细信息（`ServiceId`, `ServiceType`, `Endpoint`, `BaseUrl`, `ApiKey`, `OrgId`）。包含占位符。
*   **`AgentConfig`**: 表示 `agent.yaml` 文件的结构，定义了 Agent 使用的模型（引用 `ConnectionConfig` 的名称）、插件列表等。（*未来可能扩展*）
*   **`ServiceType` (enum)**: 定义支持的 AI 服务类型（`OpenAI`, `AzureOpenAI`, `Ollama` 等）。

### 3.2. 服务 (`Services/`)

#### 3.2.1. 提供者 (`Providers/`)

*   **`IConnectionProvider` / `ConnectionProvider`**:
    *   负责加载 `connections.yaml` 和环境特定的 `connections.<environment>.yaml` 文件。
    *   合并基础配置和环境特定配置。
    *   提供 `GetResolvedConnectionAsync(string connectionName)` 方法，该方法：
        *   查找指定名称的原始连接配置。
        *   解析配置中的占位符 (`{{...}}`)，用环境变量替换它们。
        *   验证解析后的配置（例如，检查特定服务类型所需的字段是否已提供）。
        *   缓存已解析的 `ConnectionConfig` 以提高性能。

#### 3.2.2. 加载器 (`Loaders/`)

*   **`IAgentConfigLoader` / `AgentConfigLoader`**:
    *   负责加载 `agent.yaml` 和环境特定的 `agent.<environment>.yaml` 文件。
    *   合并基础 Agent 配置和环境特定配置。
    *   提供获取最终 Agent 配置的方法。（*具体实现待定*）

#### 3.2.3. 工厂 (`Factories/`)

*   **`IAIKernelFactory` / `DefaultAIKernelFactory`**:
    *   核心服务，用于创建 `IKernel` 实例。
    *   依赖 `IAgentConfigLoader` 获取 Agent 配置。
    *   依赖 `IConnectionProvider` 获取解析后的 AI 服务连接信息。
    *   根据 Agent 配置中指定的模型（服务 ID），使用 `IConnectionProvider` 获取对应的 `ConnectionConfig`。
    *   使用获取到的连接信息配置 Semantic Kernel 的 `KernelBuilder`（例如，`AddOpenAIChatCompletion`, `AddAzureOpenAIChatCompletion` 等）。
    *   （*未来*）加载 Agent 配置中指定的插件。
    *   返回构建好的 `IKernel` 实例。

### 3.3. 依赖注入 (`Extensions/`)

*   **`AddConfigurableAIProvider(this IServiceCollection services, IConfiguration configuration)`**:
    *   扩展方法，用于在应用程序的 `IServiceCollection` 中注册 `ConfigurableAIProvider` 的所有必要服务（`ConnectionProvider`, `AgentConfigLoader`, `DefaultAIKernelFactory` 等）。
    *   从 `IConfiguration` (通常来自 `appsettings.json`) 绑定 `ConfigurableAIOptions`。

## 4. 配置文件结构示例

### `appsettings.json` (部分)

```json
{
  "ConfigurableAI": {
    "Environment": "dev", // 或 "Production", etc.
    "AgentsDirectory": "Profiles/Agents",
    "ConnectionsFilePath": "Profiles/connections.yaml",
    "ModelsFilePath": "Profiles/models.yaml",
    "GlobalPluginsDirectory": "Profiles/Plugins",
  }
  // ... 其他配置
}
```

### `Configuration/AI/connections.yaml`

```yaml
connections:
  openai:
    ServiceType: OpenAI
    ApiKey: "" # Placeholder for API Key
    OrgId: "{{OPENAI_ORG_ID}}"   # Optional placeholder
  azure_openai:
    ServiceType: AzureOpenAI
    Endpoint: "{{AZURE_OPENAI_ENDPOINT}}"
    ApiKey: "{{AZURE_OPENAI_API_KEY}}"
  ollama:
    ServiceType: Ollama
    BaseUrl: "http://localhost:11434" # Direct value
```

### `Configuration/AI/connections.dev.yaml` (可选覆盖)

```yaml
connections:
  openai:
    ApiKey: "{{OPENAI_API_KEY}}" # Paste true sk here for private
```

### `Configuration/AI/Agents/SimpleChat/agent.yaml`

```yaml
# Agent: SimpleChat - Base Configuration
Description: "A simple chat agent using OpenAI GPT-4."
ServiceId: openai_gpt4 # Refers to the key in connections.yaml
# PluginDirectory: "Plugins/SimpleChatPlugins" # Example for future plugin loading
ExecutionSettings:
  Default: # Default settings for this agent
    Temperature: 0.7
    MaxTokens: 1000
  # Specific function settings can be added here
```

## 5. 使用方法

1.  **配置 `appsettings.json`**: 设置 `ConfigurableAI` 部分，指定环境和配置文件路径。
2.  **创建 YAML 配置文件**: 根据需要创建 `connections.yaml`, `connections.<environment>.yaml`, `agent.yaml` 等文件，并定义连接和 Agent 配置。
3.  **设置环境变量**: 确保 YAML 文件中使用的占位符 (`{{...}}`) 对应的环境变量已在运行环境中设置。
4.  **注册服务**: 在应用程序的 `Program.cs` 或 `Startup.cs` 中调用 `services.AddConfigurableAIProvider(builder.Configuration);`。
5.  **注入和使用**: 在需要使用 Kernel 的服务或组件中，注入 `IAIKernelFactory`。
6.  **获取 Kernel**: 调用 `IAIKernelFactory.CreateKernelAsync("AgentName")` 来获取特定 Agent 配置对应的 `IKernel` 实例。

```csharp
// Example usage in a service
public class MyAIService
{
    private readonly IAIKernelFactory _kernelFactory;
    private readonly ILogger<MyAIService> _logger;

    public MyAIService(IAIKernelFactory kernelFactory, ILogger<MyAIService> logger)
    {
        _kernelFactory = kernelFactory;
        _logger = logger;
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        try
        {
            // Get the Kernel configured for "SimpleChat" agent
            IKernel kernel = await _kernelFactory.CreateKernelAsync("SimpleChat");

            // Use the kernel...
            // Example: Invoke a function or prompt
            // var result = await kernel.InvokePromptAsync(userMessage);
            // return result.GetValue<string>();

            return "Response from Kernel"; // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interacting with AI Kernel.");
            throw; // Re-throw or handle appropriately
        }
    }
}
```

这个文档应该涵盖了 `ConfigurableAIProvider` 模块的主要方面。如果您有任何具体问题或需要进一步的细节，请告诉我！