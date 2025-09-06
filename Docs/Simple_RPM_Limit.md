# 简单RPM限制实现

## 概述

实现了一个简单的**模型级别RPM限制**机制，避免触发AI API的速率限制。

## 核心特性

- ✅ **模型级别限制**：每个模型独立的RPM配置
- ✅ **简单配置**：只需在`models.yaml`中添加`requestsPerMinute`字段
- ✅ **自动检查**：在执行AI调用前自动检查RPM限制
- ✅ **0表示无限制**：本地模型或无限制服务可设为0

## 配置方式

在 `Profiles/models.yaml` 中为需要限制的模型添加 `requestsPerMinute` 字段：

```yaml
models:
  # OpenAI Plus用户
  openai-gpt4-precise:
    connection: personalOpenAI
    modelId: "gpt-4"
    endpointType: ChatCompletion
    requestsPerMinute: 500      # 每分钟最多500次请求

  # Azure OpenAI
  azure-gpt4o-mini-std:
    connection: primaryAzure   
    modelId: "gpt-4o-mini"
    endpointType: ChatCompletion
    requestsPerMinute: 120      # 每分钟最多120次请求

  # 本地模型（无限制）
  ollama-qwen3-8b:
    connection: Ollama
    modelId: "qwen3:8b"
    endpointType: ChatCompletion
    requestsPerMinute: 0        # 0 = 无限制
```

## 工作原理

1. **Agent配置**：Agent使用某个模型定义ID
2. **RPM检查**：执行前检查该模型的RPM限制
3. **滑动窗口**：记录最近1分钟的请求时间
4. **拒绝/允许**：超过限制则拒绝，否则允许执行

## 与现有并发控制的关系

- **SemaphoreSlim**：控制同时执行的请求数量（如5个并发）
- **RPM限制**：控制每分钟的请求频率（如500 RPM）

两者协同工作，提供更全面的保护。

## 代码实现

- `ModelConfig.RequestsPerMinute`：模型配置中的RPM字段
- `ISimpleRateLimiter`：简单的RPM限制器接口
- `SimpleRateLimiter`：使用滑动窗口算法的实现
- `PromptExecutionService`：集成RPM检查

## 使用建议

1. **根据API套餐设置**：
   - OpenAI 免费：60 RPM
   - OpenAI Plus：500 RPM
   - Azure：根据部署配置

2. **本地服务设置为0**：避免不必要的限制

3. **监控日志**：注意RPM拒绝的警告日志

就这么简单！🎉
