# 数据提取调试指南

## 概述

本指南介绍如何使用新增的调试功能来诊断和解决数据提取过程中的问题。

## 新增的调试功能

### 1. AI 原始输出查看器

在数据提取预览页面，每个数据源的结果标签页中都包含一个"查看 AI 原始输出"的折叠面板。

**功能特点：**
- 显示 AI 的完整原始响应
- 支持复制原始输出（手动复制）
- 支持重新解析原始输出

### 2. 增强的错误日志

`DataStructuringService` 现在提供更详细的错误信息：
- 记录原始 AI 响应
- 记录清理后的响应
- 详细的 JSON 解析错误信息

## 常见问题诊断

### 问题 1: 批量提取返回空结果

**症状：** 使用批量模式时，AI 返回空数组或解析失败

**诊断步骤：**
1. 打开"查看 AI 原始输出"面板
2. 检查 AI 是否返回了有效的 JSON 数组
3. 查看是否包含 markdown 代码块标记

**常见原因：**
- AI 返回了单个对象而不是数组
- JSON 格式不正确
- 包含额外的解释文本

**解决方案：**
- 使用"重新解析"功能
- 检查 prompt 是否明确要求返回数组格式
- 调整 Schema 字段定义

### 问题 2: 字段值解析错误

**症状：** 某些字段值为 null 或类型不正确

**诊断步骤：**
1. 查看原始输出中的字段名称是否与 Schema 定义匹配
2. 检查字段值的格式是否符合预期类型
3. 验证字段映射配置

**解决方案：**
- 调整字段映射指令
- 修改 Schema 字段定义
- 优化 prompt 中的字段描述

### 问题 3: JSON 解析失败

**症状：** 出现 "AI返回的数据格式无效" 错误

**诊断步骤：**
1. 查看原始输出是否包含有效的 JSON
2. 检查是否有多余的 markdown 标记
3. 验证 JSON 语法是否正确

**解决方案：**
- 手动清理 JSON 格式后使用"重新解析"
- 调整 prompt 强调 JSON 格式要求
- 降低 AI 模型的 temperature 参数

## 最佳实践

### 1. 使用调试功能

- 在开发和测试阶段，始终检查 AI 原始输出
- 保存有问题的原始输出用于 prompt 优化
- 使用重新解析功能快速测试修复方案

### 2. Prompt 优化

- 明确指定输出格式（单个对象 vs 数组）
- 强调不要包含额外的解释文本
- 提供具体的字段示例

### 3. Schema 设计

- 字段名称要清晰明确
- 提供详细的字段描述
- 合理设置必填字段

## 技术细节

### 原始输出存储

```csharp
public class ExtractionResult
{
    // ... 其他属性
    
    /// <summary>
    /// AI 原始输出（用于调试）
    /// </summary>
    public string? RawAIOutput { get; set; }
}
```

### 重新解析接口

```csharp
public interface IDataStructuringService
{
    /// <summary>
    /// 解析AI原始输出为结构化记录
    /// </summary>
    Task<List<BsonDocument>> ParseRawOutput(string rawOutput, SchemaDefinition schema);
}
```

## 日志配置

为了获得详细的调试信息，建议在 `appsettings.json` 中配置日志级别：

```json
{
  "Logging": {
    "LogLevel": {
      "ContentEngine.Core.AI.Services.DataStructuringService": "Debug"
    }
  }
}
``` 