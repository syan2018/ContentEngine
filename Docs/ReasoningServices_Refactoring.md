# 推理服务重构总结

## 重构背景

在原有的实现中，`ReasoningExecutionService` 和 `ReasoningCombinationService` 存在以下问题：

1. **重复的组合执行逻辑**：两个服务都实现了相似的组合执行代码
2. **持久化不一致**：`ReasoningCombinationService` 单独执行组合时没有持久化结果到实例中
3. **职责边界模糊**：两个服务的职责划分不够清晰

## 重构目标

1. **明确职责边界**：
   - `ReasoningExecutionService`：专注于事务级别的执行控制和监控
   - `ReasoningCombinationService`：专注于组合级别的操作（生成、执行、查询、重试）

2. **消除重复代码**：将组合执行的核心逻辑统一到 `ReasoningCombinationService` 中

3. **确保数据一致性**：所有组合执行都要正确持久化到实例中

## 重构内容

### ReasoningCombinationService 重构

#### 新增核心方法
- `ExecuteSingleCombinationAsync()`: 执行单个组合的核心逻辑
- `PersistCombinationResultAsync()`: 持久化组合结果到实例中
- `ProcessSingleCombinationInBatchAsync()`: 批量处理中的单个组合处理

#### 完善现有方法
- `ExecuteCombinationAsync()`: 现在会正确持久化结果
- `BatchExecuteCombinationsAsync()`: 完整实现批量执行功能
- `GetCombinationDetailsAsync()`: 完整实现组合详情查询
- `PreviewCombinationPromptAsync()`: 完整实现 Prompt 预览
- `RetryFailedCombinationAsync()`: 完整实现失败组合重试
- `GetFailedCombinationsAsync()`: 完整实现失败组合查询
- `RegenerateAndResetInstanceAsync()`: 重新生成组合并重置实例状态

#### 数据一致性保证
- 所有组合执行都会更新实例的 `Outputs`、`Metrics` 和 `Errors`
- 支持组合结果的覆盖更新（重试场景）
- 正确处理并发访问（使用锁机制）

#### 重新生成组合功能
- `RegenerateAndResetInstanceAsync()`: 重新生成组合并完全重置实例状态
  - 清空所有现有输出结果 (`Outputs`)
  - 重置执行指标 (`Metrics`)
  - 清空错误记录 (`Errors`)
  - 将已完成或失败的实例状态重置为 `Pending`
  - 重置时间戳（保留创建时间）
  - 前端提供确认对话框防止误操作

#### 性能优化
- **发现**：`ResolvedViews` 在执行时实际上不被使用，`InputCombination.DataMap` 已包含所有需要的数据
- **优化**：当实例已有组合时，跳过视图数据获取，避免不必要的数据库查询和内存开销
- **原理**：Prompt 填充使用的是 `combination.DataMap`，而不是 `ResolvedViews`

#### 批量执行持久化修复
- **问题**：批量执行时只在最后持久化一次，如果执行过程中发生异常，所有结果都会丢失
- **对比**：单个执行每次都立即持久化，所以结果能够保存
- **修复**：
  - 添加定期持久化机制：每处理10个组合就持久化一次
  - 改进最终持久化：添加异常处理和重试机制
  - 确保批量执行的结果不会因为意外中断而丢失

#### 实例状态同步问题修复
- **严重问题**：`ExecuteTransactionAsync` 在开始时获取实例副本，但批量执行会更新数据库中的实例，最后标记完成时使用的仍是最初的副本，导致所有执行结果被覆盖
- **问题表现**：批量执行的所有 `Outputs`、`Metrics` 等都丢失，只保留最后的状态更新
- **根本原因**：实例对象在内存中的状态与数据库中的状态不同步
- **修复方案**：
  - 在标记完成前重新获取最新的实例状态
  - 在异常处理中也使用最新的实例状态
  - 确保不会覆盖批量执行的结果

### ReasoningExecutionService 重构

#### 简化职责
- 移除了重复的组合执行逻辑
- 专注于事务级别的流程控制
- 使用 `ReasoningCombinationService` 的批量执行功能

#### 核心变更
- `GenerateOutputsAsync()`: 现在委托给 `ReasoningCombinationService.BatchExecuteCombinationsAsync()`
- 移除了 `ProcessCombinationAsync()` 私有方法
- 更新了依赖注入：`IPromptExecutionService` → `IReasoningCombinationService`

#### 修复数据同步问题
- **问题**：`ProcessDataAsync` 方法强制重新生成组合，覆盖用户手动操作
- **解决方案**：
  - 检查实例是否已有组合，如果有则保留用户的组合
  - 新增 `IQueryProcessingService.ResolveViewDataAsync()` 方法，只获取视图数据而不生成组合
  - 当实例有组合但缺少视图数据时，使用新方法获取视图数据
  - 确保用户手动重新生成的组合不会被执行时覆盖

## 架构优势

### 1. 单一职责原则
- **ExecutionService**: 事务生命周期管理、状态控制、预检查
- **CombinationService**: 组合生成、执行、查询、重试

### 2. 代码复用
- 组合执行逻辑统一在 `ReasoningCombinationService` 中
- 支持单个执行、批量执行、重试等多种场景

### 3. 数据一致性
- 所有组合操作都会正确更新实例状态
- 支持断点续传和错误恢复

### 4. 可扩展性
- 新的组合操作可以轻松添加到 `ReasoningCombinationService`
- 事务级别的控制逻辑与组合执行逻辑解耦

## 使用示例

### 执行完整事务
```csharp
var instance = await executionService.ExecuteTransactionAsync(instanceId);
```

### 执行单个组合
```csharp
var output = await combinationService.ExecuteCombinationAsync(instanceId, combinationId);
```

### 批量执行组合
```csharp
var result = await combinationService.BatchExecuteCombinationsAsync(
    instanceId, combinationIds, maxConcurrency: 5);
```

### 重试失败的组合
```csharp
var output = await combinationService.RetryFailedCombinationAsync(instanceId, combinationId);
```

### 重新生成组合并重置实例状态
```csharp
var newCombinations = await combinationService.RegenerateAndResetInstanceAsync(instanceId);
```

## 兼容性

- 所有现有的接口保持不变
- 依赖注入配置无需修改
- 现有的调用代码无需更改

## 测试建议

1. **单元测试**：验证组合执行逻辑的正确性
2. **集成测试**：验证事务执行的完整流程
3. **并发测试**：验证批量执行的线程安全性
4. **错误恢复测试**：验证失败重试和断点续传功能 