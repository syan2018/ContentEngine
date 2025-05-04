# ConfigurableAIProvider 模块文档

## 1. 概述

`ConfigurableAIProvider` 是一个 .NET 类库，旨在为应用程序（特别是 `ContentEngine`）提供一种灵活、可配置的方式来创建和管理 Semantic Kernel (`Kernel`) 实例。它允许开发者通过外部 YAML 配置文件定义 AI 服务连接、模型选择、Agent 行为以及插件加载，从而将 AI 相关的配置与应用程序代码解耦，提高了可维护性和灵活性。

## 2. 核心概念

### 2.1. 分层配置

模块采用分层的配置策略，以提高灵活性和安全性：

*   **`connections.yaml` (基础连接配置)**: 定义所有可用的 AI 服务连接（如 OpenAI, Azure OpenAI, Ollama 等）。此文件通常包含非敏感信息，如服务类型、端点（对于 Azure/Ollama）或基础 URL。**敏感信息（如 API Key）应使用占位符**。
*   **`connections.<environment>.yaml` (环境特定覆盖)**: 可选文件，用于覆盖特定环境（如 `dev`, `prod`）的基础连接配置。
*   **`models.yaml` (模型定义)**: 定义具体的 AI 模型配置，包括模型 ID（如 `gpt-3.5-turbo`, `text-embedding-ada-002`）、端点类型（`ChatCompletion`, `TextCompletion`, `Embedding`等），并**引用** `connections.yaml` 中定义的连接名称。
*   **`models.<environment>.yaml` (模型环境特定覆盖)**: 可选文件，用于覆盖特定环境的模型配置。
*   **`agent.yaml` (Agent 配置)**: 定义特定 Agent 或 Kernel 实例的配置。它包含：
    *   `models`: 一个字典，将**逻辑模型名称** (如 `chat`, `embedder`) 映射到 `models.yaml` 中定义的**模型定义 ID**。
    *   `plugins`: 一个列表，包含要加载的插件的**引用**（相对于 Agent 目录或全局插件目录的路径）。
    *   `logLevel`: 可选，覆盖全局日志级别。
*   **`agent.<environment>.yaml` (Agent 环境特定覆盖)**: 可选文件，用于覆盖特定 Agent 在特定环境下的配置。
*   **`PluginName.yaml` (插件配置)**: 位于插件目录下（Agent 本地或全局），定义插件的 `name` (可选，否则从目录名推断)、`description` 以及 `functions` 节点。`functions` 节点下包含 `semanticFunctions` 和 `cSharpFunctions` 列表。
    *   `semanticFunctions`: 包含指向**函数配置文件** (`FunctionName.yaml`) 的相对路径 (`path`)。
    *   `cSharpFunctions`: 包含原生 C# 函数的配置，包括程序集相对路径 (`dll`, 可选，为空则加载执行程序集) 和完全限定类名 (`className`)。
*   **`FunctionName.yaml` (语义函数配置)**: 定义单个语义函数的详细信息，包括 `description`, `prompt`, `templateFormat`, `inputVariables` (参数) 和 `executionSettings` (模型参数，如 temperature, maxTokens)。函数名从文件名推断。
*   **`appsettings.json`**: 用于配置 `ConfigurableAIProvider` 模块本身的行为，例如配置文件根目录、环境名称、以及各种配置文件的相对路径 (Agents, Connections, Models, Global Plugins)。

### 2.2. 占位符解析

为了安全地处理 API Key 等敏感信息，模块在加载 `connections.yaml` 时支持占位符，格式为 `{{ENVIRONMENT_VARIABLE_NAME}}`。`ConnectionProvider` 会自动查找匹配的环境变量，并将其值替换到相应的配置项中。如果环境变量未设置且配置项需要值，将会抛出错误。

## 3. 关键组件

### 3.1. 配置模型 (`Models/`)

*   **`ConfigurableAIOptions` (`Configuration/`)**: 用于从 `appsettings.json` 加载模块设置。
*   **`ConnectionsConfig` / `ConnectionConfig` (`Configuration/`)**: 表示 `connections.yaml` 结构，定义 AI 服务连接。`ConnectionConfig` 包含占位符。
*   **`ModelsConfig` / `ModelConfig` (`Configuration/`)**: 表示 `models.yaml` 结构，定义具体模型及其使用的连接。
*   **`AgentConfig` (`Configuration/`)**: 表示 `agent.yaml` 结构，定义 Agent 使用的逻辑模型映射、插件引用列表和日志级别。
*   **`ServiceType` / `EndpointType` (enums, `Configuration/`)**: 定义支持的 AI 服务类型和端点类型。
*   **`PluginConfig`**: 表示 `PluginName.yaml` 文件的结构，包含插件元数据及 `semanticFunctions` 和 `cSharpFunctions` 的配置列表。
*   **`PromptConfig`**: 表示 `FunctionName.yaml` 文件的结构，定义语义函数的提示、输入变量和执行设置。

### 3.2. 服务 (`Services/`)

#### 3.2.1. 提供者 (`Providers/`)

*   **`IConnectionProvider` / `ConnectionProvider`**:
    *   加载和合并 `connections.yaml` 及环境覆盖文件。
    *   提供 `GetResolvedConnectionAsync(string connectionName)` 方法，解析占位符并验证配置。
    *   缓存已解析的连接配置。
*   **`IModelProvider` / `ModelProvider`**:
    *   加载和合并 `models.yaml` 及环境覆盖文件。
    *   提供 `GetModelDefinitionAsync(string modelId)` 方法获取模型定义。
    *   缓存模型定义。

#### 3.2.2. 加载器 (`Loaders/`)

*   **`IAgentConfigLoader` / `AgentConfigLoader`**:
    *   加载和合并 `agent.yaml` 及环境覆盖文件（基于 `AgentsDirectory` 配置）。
    *   提供 `LoadConfigAsync(string agentName)` 获取最终 Agent 配置。

#### 3.2.3. 工厂 (`Factories/`)

*   **`IAIKernelFactory` / `DefaultAIKernelFactory`**:
    *   核心服务，用于异步创建 `Kernel` 实例 (`BuildKernelAsync(string agentName)`)。
    *   依赖 `IAgentConfigLoader`, `IModelProvider`, `IConnectionProvider` 获取所需配置。
    *   使用 `IAIServiceConfigurator` 集合（通过 DI 注入，如 `OpenAIConfigurator`, `AzureOpenAIConfigurator` 等）根据解析后的模型和连接信息配置 `KernelBuilder` 的 AI 服务。
    *   调用 `LoadPluginsAsync` 方法加载 Agent 配置中指定的插件。
    *   `LoadPluginsAsync` 内部逻辑：
        *   解析插件引用路径：首先尝试相对于 Agent 配置目录解析，然后尝试相对于全局插件目录 (`GlobalPluginsDirectory`) 解析。
        *   查找配置文件：在解析到的插件目录下查找 `PluginName.yaml`（`PluginName` 从目录名推断）。
        *   调用扩展方法：使用 `KernelExtensions.ImportPluginFromConfig`（传入配置文件路径和插件目录路径）加载插件。
        *   注册插件：将成功加载返回的 `KernelPlugin` 对象添加到 `kernel.Plugins` 集合中。
    *   返回构建好的 `Kernel` 实例。

### 3.3. 扩展 (`Extensions/`)

*   **`KernelExtensions`**:
    *   提供 `ImportPluginFromConfig(this Kernel kernel, ...)` 扩展方法，作为加载插件的主要入口。
    *   内部包含私有帮助方法，用于加载语义函数 (`LoadSemanticFunction`) 和 C# 原生函数 (`LoadCSharpFunctions`)。
    *   `LoadSemanticFunction`: 解析 `FunctionName.yaml`，创建 `PromptTemplateConfig`，并使用 `KernelFunctionFactory.CreateFromPrompt` 创建 `KernelFunction`。
    *   `LoadCSharpFunctions`: 根据 `PluginConfig` 中的 `dll` (可选) 和 `className` 配置，使用 `Assembly.LoadFrom` 或 `Assembly.GetExecutingAssembly` 加载程序集，使用 `Activator.CreateInstance` 创建类实例（**需要无参构造函数**），然后使用 `KernelFunctionFactory.CreateFromMethod` 从标记了 `[KernelFunction]` 的方法创建 `KernelFunction` 列表。
*   **`DependencyInjectionExtensions` (`Configuration/`)**:
    *   提供 `AddConfigurableAIProvider(this IServiceCollection services, IConfiguration configuration)` 扩展方法。
    *   注册 `ConfigurableAIProvider` 的所有服务 (`ConnectionProvider`, `ModelProvider`, `AgentConfigLoader`, `DefaultAIKernelFactory` 以及各种 `IAIServiceConfigurator` 实现) 到 DI 容器。
    *   从 `IConfiguration` 绑定 `ConfigurableAIOptions`。

## 4. 配置文件结构示例

### `appsettings.json` (部分)

```json
{
  "ConfigurableAI": {
    "Environment": "dev", // 或 "Production", etc.
    "AgentsDirectory": "Profiles/Agents",
    "ConnectionsFilePath": "Profiles/connections.yaml",
    "ModelsFilePath": "Profiles/models.yaml",
    "GlobalPluginsDirectory": "Profiles/Plugins"
  }
  // ... 其他配置
}
```

### `Profiles/connections.yaml`

```yaml
connections:
  default_openai:
    serviceType: OpenAI
    apiKey: "{{OPENAI_API_KEY}}" # Placeholder
    # orgId: "{{OPENAI_ORG_ID}}"
  default_azure:
    serviceType: AzureOpenAI
    endpoint: "{{AZURE_OPENAI_ENDPOINT}}"
    apiKey: "{{AZURE_OPENAI_API_KEY}}"
  local_ollama:
    serviceType: Ollama
    baseUrl: "http://localhost:11434"
```

### `Profiles/models.yaml`

```yaml
models:
  gpt-3.5-turbo:
    connection: default_openai # References key in connections.yaml
    modelId: "gpt-3.5-turbo"
    endpointType: ChatCompletion
  # Example for Azure
  # azure-gpt4:
  #   connection: default_azure
  #   modelId: "my-deployment-name" # Deployment name in Azure
  #   endpointType: ChatCompletion
  ollama-llama3:
    connection: local_ollama
    modelId: "llama3" # Model name in Ollama
    endpointType: ChatCompletion
```

### `Profiles/Agents/SimpleChat/agent.yaml`

```yaml
# Agent: SimpleChat - Base Configuration
description: "A simple chat agent."

models:
  # Logical name maps to model definition ID from models.yaml
  chat: ollama-llama3 # Use local Ollama Llama3 for chat

plugins:
  # References plugin directory names relative to this agent dir OR global plugin dir
  - Communicator
  - TestPlugin

# Optional: Override default log level
logLevel: Information
```

### `Profiles/Plugins/TestPlugin/TestPlugin.yaml` (全局插件示例)

```yaml
# Plugin: TestPlugin
name: TestPlugin # Optional, inferred from directory if omitted
description: A simple plugin for testing C# function loading.
functions:
  cSharpFunctions:
    # dll is omitted: Load from executing assembly
    - className: ConfigurableAIProvider.Plugins.TestPluginFunctions
```

### `Profiles/Agents/SimpleChat/Communicator/Communicator.yaml` (Agent 局部插件示例)

```yaml
# Plugin: Communicator
description: Handles basic conversation.
functions:
  semanticFunctions:
    - path: ./Prompts/ChatWithUser.yaml # Path relative to this Communicator.yaml file
```

### `Profiles/Agents/SimpleChat/Communicator/Prompts/ChatWithUser.yaml` (语义函数示例)

```yaml
# Function: ChatWithUser (name inferred from filename)
description: Returns a conversational response based on history.
type: completion
prompt: |
  The bot is a friendly, helpful conversationalist.

  {{$history}}
  User: {{$input}}
  Bot:
inputVariables:
  parameters:
    - name: history
      description: The conversation history.
      isRequired: true
    - name: input
      description: The user's latest message.
      isRequired: true
executionSettings:
  default: # Settings for the default service
    temperature: 0.7
    maxTokens: 500
```

## 5. 使用方法

1.  **配置 `appsettings.json`**: 设置 `ConfigurableAI` 部分。
2.  **创建 YAML 配置文件**: 定义 `connections`, `models`, `agent` 以及各插件和函数的配置文件 (`PluginName.yaml`, `FunctionName.yaml`)。
3.  **设置环境变量**: 确保 `connections.yaml` 中使用的占位符对应的环境变量已设置。
4.  **注册服务**: 在 `Program.cs` 中调用 `builder.Services.AddConfigurableAIProvider(builder.Configuration);`。
5.  **注入和使用**: 在需要的地方注入 `IAIKernelFactory`。
6.  **获取 Kernel**: 调用 `await _kernelFactory.BuildKernelAsync("AgentName");` 获取 `Kernel` 实例。
7.  **调用函数**: 使用 `kernel.InvokeAsync("PluginName", "FunctionName", arguments)` 或 `kernel.InvokePromptAsync(...)`。

```csharp
// Example usage in a Razor component
@page "/test-ai"
@using Microsoft.SemanticKernel
@using ConfigurableAIProvider.Services
@inject IAIKernelFactory AiKernelFactory
@inject ILogger<MyAIService> Logger // Inject logger if needed

<h3>AI Test Page</h3>

@if (kernel != null)
{
    <button @onclick="RunAICalls" disabled="@isLoading">Run AI Calls</button>
    @if (isLoading) { <p>Running...</p> }
    <pre>Chat Response: @chatResult</pre>
    <pre>Plugin Response: @pluginResult</pre>
} else {
    <p>Kernel could not be built.</p>
}


@code {
    private Kernel? kernel;
    private bool isLoading = false;
    private string? chatResult;
    private string? pluginResult;

    protected override async Task OnInitializedAsync()
    {
        try {
             kernel = await AiKernelFactory.BuildKernelAsync("SimpleChat");
        } catch (Exception ex) {
             Logger.LogError(ex, "Failed to build Kernel for SimpleChat");
             // Handle error appropriately
        }
    }

    private async Task RunAICalls()
    {
        if (kernel == null) return;

        isLoading = true;
        chatResult = pluginResult = null; // Clear previous results
        StateHasChanged(); // Update UI

        try
        {
            // Invoke semantic function
            var commArgs = new KernelArguments {
                { "input", "Write a haiku about clouds." },
                { "history", "" }
            };
            var commResult = await kernel.InvokeAsync("Communicator", "ChatWithUser", commArgs);
            chatResult = commResult.GetValue<string>()?.Trim();

            // Invoke C# function
            var pluginArgs = new KernelArguments { { "name", "Tester" } };
            var testResult = await kernel.InvokeAsync("TestPlugin", "SayHello", pluginArgs);
            pluginResult = testResult.GetValue<string>();

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error invoking functions");
            chatResult = $"Error: {ex.Message}"; // Show error in output
        }
        finally
        {
            isLoading = false;
            StateHasChanged(); // Update UI
        }
    }
}
```
