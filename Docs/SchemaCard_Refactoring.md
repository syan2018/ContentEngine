# SchemaCard 组件重构文档

## 重构概述

本次重构将原本分散在多个页面中的 Schema 卡片展示逻辑统一抽象为一个通用的 `SchemaCard` 组件，大大提高了代码复用性和维护性。

## 重构前的问题

1. **代码重复**：`SchemaManagement/Index.razor`、`DataEntry/Index.razor`、`DataExplorer/Index.razor` 三个页面都有相似的 Schema 卡片展示代码
2. **样式不统一**：各页面的卡片样式略有差异，维护困难
3. **功能分散**：导航逻辑分散在各个页面中，难以统一管理

## 重构后的架构

### SchemaCard 组件特性

- **统一样式**：基于 `schema-management` 页面的最佳样式设计
- **模式驱动**：通过 `SchemaCardMode` 枚举自动配置不同场景下的显示和行为
- **内置导航**：将常用的页面跳转逻辑内聚到组件中
- **高度可配置**：支持自定义覆盖默认行为

### 支持的模式

#### 1. Management 模式 (`SchemaCardMode.Management`)
- **使用场景**：Schema 管理页面
- **显示特性**：
  - 完整的卡片头部
  - 右上角菜单（编辑、删除）
  - 默认芯片颜色
  - 显示更新时间（yyyy-MM-dd 格式）
- **操作按钮**：
  - "查看"：跳转到详情页
  - "注入"：跳转到 AI 辅助录入页

#### 2. DataEntry 模式 (`SchemaCardMode.DataEntry`)
- **使用场景**：数据录入页面
- **显示特性**：
  - 简化标题（无头部）
  - 统一芯片样式
  - 显示"记录数"标签
  - 不显示更新时间
- **操作按钮**：
  - "手动录入"：跳转到手动录入页
  - "AI 辅助"：跳转到 AI 辅助录入页

#### 3. DataExplorer 模式 (`SchemaCardMode.DataExplorer`)
- **使用场景**：数据浏览页面
- **显示特性**：
  - 简化标题（无头部）
  - 统一芯片样式
  - 卡片可点击
  - 统一更新时间格式（yyyy-MM-dd）
- **操作按钮**：
  - "浏览数据"：跳转到数据浏览页
- **交互行为**：
  - 点击卡片直接跳转到数据浏览页

## 使用方法

### 基本用法

```razor
<!-- Management 模式 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.Management"
           OnDeleteRequested="@DeleteSchemaAsync" />

<!-- DataEntry 模式 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.DataEntry" />

<!-- DataExplorer 模式 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.DataExplorer" />
```

### 可选的记录数传入

```razor
<!-- 如果已经有记录数，可以直接传入避免重复查询 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.Management"
           RecordCount="@cachedRecordCount"
           OnDeleteRequested="@DeleteSchemaAsync" />
```

### 高级自定义

```razor
<!-- 自定义操作按钮 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.Management"
           ActionButtons="@customActionButtons" />

<!-- 自定义菜单项 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.Management"
           MenuItems="@customMenuItems" />

<!-- 覆盖默认行为 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.DataExplorer"
           OnCardClick="@CustomCardClickHandler"
           OnTitleClick="@CustomTitleClickHandler" />
```

## 内置功能

### 导航方法

SchemaCard 组件内置了以下导航方法，无需在使用页面中重复实现：

- `NavigateToDetails()` - 跳转到 Schema 详情页
- `NavigateToEdit()` - 跳转到 Schema 编辑页
- `NavigateToManualEntry()` - 跳转到手动数据录入页
- `NavigateToAIEntry()` - 跳转到 AI 辅助录入页
- `NavigateToDataView()` - 跳转到数据浏览页

### 记录数获取

组件内置了智能的记录数获取逻辑：

```csharp
private string GetDisplayRecordCount()
{
    if (RecordCount.HasValue)
    {
        return RecordCount.Value.ToString();
    }

    try
    {
        // 自动获取记录数
        var count = DataEntryService.CountDataAsync(Schema.Name).GetAwaiter().GetResult();
        return count.ToString();
    }
    catch
    {
        return "N/A";
    }
}
```

**优势**：
- **自动获取**：如果没有传入 RecordCount 参数，组件会自动调用 DataEntryService 获取
- **缓存友好**：如果已有记录数，可以传入避免重复查询
- **容错处理**：查询失败时显示 "N/A"，不会影响页面渲染
- **内聚性**：各页面无需重复实现相同的获取逻辑

## 重构效果

### 代码减少
- **SchemaManagement/Index.razor**：减少约 50 行代码（包括 GetRecordCount 方法）
- **DataEntry/Index.razor**：减少约 40 行代码（包括 GetRecordCount 方法）
- **DataExplorer/Index.razor**：减少约 30 行代码（包括 GetRecordCount 方法）
- **总计**：减少约 120 行重复代码

### 维护性提升
- 统一的样式管理
- 集中的导航逻辑
- **内聚的记录数获取**：无需在各页面重复实现 GetRecordCount 方法
- 一致的用户体验
- 三个界面使用相同的 UI 布局和样式

### 扩展性增强
- 新增模式只需扩展枚举和配置方法
- 自定义行为通过参数覆盖
- 组件可在其他页面复用
- **智能记录数显示**：组件自动获取记录数，也支持手动传入

### 功能改进
- **修复了标题点击问题**：使用 div 包装解决事件冲突
- **统一了路由规范**：DataView 路由改为使用 ID 而非名称
- **统一了 UI 样式**：三个界面都采用数据管理页面的最佳样式设计
- **高度内聚**：记录数获取逻辑内聚到组件中，各页面无需重复实现
- **简化设计**：移除过度设计，统一芯片颜色、按钮样式、标签文本和时间格式
- **精简参数**：移除冗余的样式参数，直接在HTML中使用固定值，便于本地化

## 参数精简

### 移除的冗余参数
在最终重构中，我们移除了以下冗余参数，直接在HTML中使用固定值：

- `ShowHeader` / `ShowMenu` / `IsClickable` - 通过Mode自动决定
- `RecordCountLabel` - 固定为"记录数"
- `UpdateTimeFormat` - 固定为"yyyy-MM-dd"
- `ChipColor` - 固定为Color.Default
- `ChipVariant` - 固定为Variant.Outlined
- `ChipClass` - 固定为"schema-card-chip"
- `TitleTypo` - 固定为Typo.h6

### 保留的核心参数
- `Schema` - 核心数据对象
- `Mode` - 决定组件行为的关键参数
- `RecordCount` - 可选的性能优化参数
- `ActionButtons` / `MenuItems` - 自定义覆盖
- `OnCardClick` / `OnTitleClick` / `OnDeleteRequested` - 事件回调
- `ShowMetadata` / `ShowRecordCount` / `ShowUpdateTime` - 显示控制
- `DescriptionLines` / `Height` / `Class` / `ChildContent` - 样式和扩展

### 优势
- **简化使用**：减少了不必要的参数传递
- **便于本地化**：固定文本直接在HTML中，便于后续国际化
- **减少错误**：避免了样式参数的不一致配置
- **提高性能**：减少了参数绑定和计算开销

## 最佳实践

1. **优先使用模式**：尽量使用预定义的模式，避免过度自定义
2. **保持一致性**：同一场景下使用相同的模式和配置
3. **合理传参**：只传递必要的参数，让组件自动处理其他配置
4. **事件处理**：对于特殊的删除逻辑，通过 `OnDeleteRequested` 回调处理

## 未来扩展

可以考虑添加以下功能：
- 更多预定义模式
- 主题切换支持
- 动画效果配置
- 批量操作支持 