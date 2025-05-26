# 数据结构化服务插件化重构

## 概述

本次重构将 `DataStructuringService` 中硬编码的 prompt 逻辑移植到了语义插件中，提高了代码的可维护性和灵活性。

## 重构内容

### 1. 创建了新的语义插件

**插件位置**: `Profiles/Agents/ContentEngineHelper/DataStructuring/`

- **DataStructuring.yaml**: 插件主配置文件
- **Prompts/ExtractData.yaml**: 数据提取的语义函数配置

### 2. 修改了 DataStructuringService

**主要变更**:
- 删除了 `BuildExtractionPrompt` 方法（硬编码 prompt 构建）
- 重写了 `CallAIForExtractionAsync` 方法，使用语义插件
- 添加了三个辅助方法来构建插件参数：
  - `BuildFieldDefinitionsText`: 构建字段定义文本
  - `BuildFieldMappingsText`: 构建字段映射指令文本
  - `BuildOutputFormatExample`: 构建输出格式示例

### 3. 更新了 Agent 配置

在 `Profiles/Agents/ContentEngineHelper/agent.yaml` 中添加了对 `DataStructuring` 插件的引用。

## 优势

1. **可维护性**: Prompt 逻辑从代码中分离，可以独立修改和优化
2. **可配置性**: 通过 YAML 文件可以轻松调整 prompt 模板和参数
3. **可重用性**: 语义函数可以被其他服务或组件复用
4. **版本控制**: Prompt 的变更可以通过版本控制系统跟踪
5. **测试友好**: 可以独立测试 prompt 逻辑

## 使用方式

服务的公共接口保持不变，调用方式无需修改：

```csharp
var results = await dataStructuringService.ExtractDataAsync(
    schema, 
    dataSources, 
    extractionMode, 
    fieldMappings, 
    cancellationToken);
```

## 插件参数说明

ExtractData 语义函数接受以下参数：

- `schemaName`: Schema 名称
- `schemaDescription`: Schema 描述
- `fieldDefinitions`: 字段定义文本
- `extractionMode`: 提取模式描述
- `fieldMappings`: 字段映射指令（可选）
- `outputFormat`: 期望的 JSON 输出格式示例
- `dataContent`: 要处理的实际数据内容

## 注意事项

1. 确保 `ContentEngineHelper` Agent 配置正确
2. 语义插件的 temperature 设置为 0.1，确保输出的一致性
3. maxTokens 设置：单个记录 1500，批量记录 3000
4. **重要**: 避免在 prompt 中使用复杂的条件语法（如 `{{#if}}`），这可能导致 Semantic Kernel 解析错误

## 已知问题和解决方案

### 插件加载错误
如果遇到 "A function named argument must contain a name and value separated by a '=' character" 错误：

1. 检查 prompt 文件中的变量语法，确保使用 `{{$variableName}}` 格式
2. 避免使用条件语法 `{{#if}}`，改为在 C# 代码中处理条件逻辑
3. 确保所有必需参数都有默认值或在调用时提供

参考: [GitHub Issue #4348](https://github.com/microsoft/semantic-kernel/issues/4348) 