# SchemaCard 组件重构文档

## 重构概述

本次重构将原本分散在多个页面中的 Schema 卡片展示逻辑统一抽象为一个通用的 `SchemaCard` 组件，并引入了 `SchemaCardConfiguration` 配置类来集中管理显示和行为逻辑，大大提高了代码复用性、可维护性和可扩展性。

## 重构前的问题

1. **代码重复**：`SchemaManagement/Index.razor`、`DataEntry/Index.razor`、`DataExplorer/Index.razor` 三个页面都有相似的 Schema 卡片展示代码
2. **样式不统一**：各页面的卡片样式略有差异，维护困难
3. **功能分散**：导航逻辑分散在各个页面中，难以统一管理
4. **配置逻辑复杂**：多个 `Get...ByMode()` 方法分散在组件中，难以理解和维护
5. **同步阻塞**：记录数获取使用同步方式，可能阻塞UI渲染

## 重构后的架构

### 核心设计理念

#### 1. 配置驱动架构
引入 `SchemaCardConfiguration` 类来集中管理所有显示和行为配置：
- **统一配置**：所有显示属性、行为逻辑集中在一个配置对象中
- **工厂模式**：通过静态工厂方法为不同模式生成预设配置
- **灵活覆盖**：支持通过参数覆盖默认配置的特定项
- **易于扩展**：新增模式或配置项只需修改配置类

#### 2. 异步优先设计
- **异步记录数获取**：使用 `OnParametersSetAsync` 异步加载记录数
- **加载状态显示**：记录数加载时显示"加载中..."
- **容错处理**：加载失败时显示"N/A"，不影响页面渲染
- **性能优化**：支持传入已知记录数，避免重复查询

#### 3. 现代C#语法
- **集合表达式**：使用 `[...]` 语法替代 `new List<>()`
- **模式匹配**：使用 `is { Property: value }` 进行空值检查
- **简化表达式**：使用三元运算符和集合初始化器

### SchemaCard 组件特性

- **统一样式**：基于 `schema-management` 页面的最佳样式设计
- **配置驱动**：通过 `SchemaCardConfiguration` 类管理所有显示和行为
- **模式驱动**：通过 `SchemaCardMode` 枚举自动配置不同场景下的显示和行为
- **内置导航**：将常用的页面跳转逻辑内聚到组件中
- **高度可配置**：支持自定义配置完全覆盖默认行为
- **异步友好**：记录数获取等操作使用异步模式

### SchemaCardConfiguration 配置类

```csharp
public class SchemaCardConfiguration
{
    public bool ShowHeader { get; set; } = true;
    public bool ShowMenu { get; set; } = true;
    public bool IsCardClickable { get; set; } = false;
    public Func<SchemaDefinition, Task>? CardClickAction { get; set; }
    public Func<SchemaDefinition, Task>? TitleClickAction { get; set; }
    public bool ShowDescription { get; set; } = true;
    public int DescriptionLines { get; set; } = 2;
    public bool ShowMetadata { get; set; } = true;
    public bool ShowRecordCount { get; set; } = true;
    public bool ShowUpdateTime { get; set; } = true;
    public List<SchemaCardMenuItem>? MenuItems { get; set; }
    public List<SchemaCardActionButton>? ActionButtons { get; set; }

    // 静态工厂方法
    public static SchemaCardConfiguration ForManagementMode(SchemaCard component);
    public static SchemaCardConfiguration ForDataEntryMode(SchemaCard component);
    public static SchemaCardConfiguration ForDataExplorerMode(SchemaCard component);
}
```

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
- **交互行为**：
  - 标题点击跳转到详情页

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
- **交互行为**：
  - 标题点击跳转到 AI 辅助录入页

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
  - 标题点击跳转到数据浏览页

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

#### 使用参数覆盖默认配置
```razor
<!-- 覆盖默认配置的特定项 -->
<SchemaCard Schema="@schema"
           Mode="SchemaCard.SchemaCardMode.Management"
           ActionButtons="@customActionButtons"
           ShowRecordCount="false"
           DescriptionLines="3" />

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

#### 使用完全自定义配置
```razor
<!-- 使用完全自定义的配置 -->
<SchemaCard Schema="@schema"
           CustomConfiguration="@GetMyCustomConfiguration()" />
```

```csharp
// 在父组件的 @code 块中
private SchemaCardConfiguration GetMyCustomConfiguration()
{
    return new SchemaCardConfiguration
    {
        ShowHeader = true,
        ShowMenu = false,
        IsCardClickable = true,
        CardClickAction = async (s) => Navigation.NavigateTo($"/my-custom-view/{s.Id}"),
        TitleClickAction = async (s) => Console.WriteLine($"Title clicked for {s.Name}"),
        ShowDescription = true,
        DescriptionLines = 1,
        ShowMetadata = true,
        ShowRecordCount = true,
        ShowUpdateTime = false,
        ActionButtons = [
            new SchemaCardActionButton { Text = "自定义操作", Action = async (s) => { /* ... */ } }
        ]
    };
}
```

## 内置功能

### 导航方法

SchemaCard 组件内置了以下导航方法，无需在使用页面中重复实现：

- `NavigateToDetails()` - 跳转到 Schema 详情页
- `NavigateToEdit()` - 跳转到 Schema 编辑页
- `NavigateToManualEntry()` - 跳转到手动数据录入页
- `NavigateToAIEntry()` - 跳转到 AI 辅助录入页
- `NavigateToDataView()` - 跳转到数据浏览页

### 异步记录数获取

组件内置了智能的异步记录数获取逻辑：

```csharp
private async Task LoadRecordCountIfNeededAsync()
{
    if (_effectiveConfiguration.ShowRecordCount && !_currentRecordCount.HasValue && DataEntryService != null)
    {
        _isLoadingRecordCount = true;
        try
        {
            _currentRecordCount = (int?)await DataEntryService.CountDataAsync(Schema.Name);
        }
        catch (Exception ex)
        {
            _currentRecordCount = null; // 显示 N/A
        }
        finally
        {
            _isLoadingRecordCount = false;
            StateHasChanged();
        }
    }
}

private string GetActualDisplayRecordCount()
{
    if (_isLoadingRecordCount) return "加载中...";
    return _currentRecordCount?.ToString() ?? "N/A";
}
```

**优势**：
- **异步加载**：不阻塞UI渲染，提供更好的用户体验
- **加载状态**：显示"加载中..."状态，用户体验友好
- **自动获取**：如果没有传入 RecordCount 参数，组件会自动异步获取
- **缓存友好**：如果已有记录数，可以传入避免重复查询
- **容错处理**：查询失败时显示 "N/A"，不会影响页面渲染
- **内聚性**：各页面无需重复实现相同的获取逻辑

### 配置优先级

组件的配置优先级如下（从高到低）：

1. **CustomConfiguration** - 完全自定义配置，优先级最高
2. **参数覆盖** - 通过 MenuItems、ActionButtons、ShowMetadata 等参数覆盖
3. **Mode 默认配置** - 根据 SchemaCardMode 生成的默认配置

## 重构效果

### 代码减少
- **SchemaManagement/Index.razor**：减少约 50 行代码（包括 GetRecordCount 方法）
- **DataEntry/Index.razor**：减少约 40 行代码（包括 GetRecordCount 方法）
- **DataExplorer/Index.razor**：减少约 30 行代码（包括 GetRecordCount 方法）
- **SchemaCard.razor**：移除了约 80 行冗余的 `Get...ByMode()` 方法
- **总计**：减少约 200 行重复和冗余代码

### 架构改进
- **配置集中化**：所有显示和行为逻辑集中在 `SchemaCardConfiguration` 中
- **消除重复方法**：移除了 `GetShowHeaderByMode()`、`GetMenuItemsByMode()` 等冗余方法
- **异步优化**：记录数获取改为异步，提升用户体验
- **现代语法**：使用集合表达式、模式匹配等现代C#语法
- **类型安全**：Action 类型改为 `Func<SchemaDefinition, Task?>` 支持异步

### 维护性提升
- **统一配置管理**：所有配置逻辑集中在一个类中
- **集中的导航逻辑**：导航方法内聚到组件中
- **内聚的记录数获取**：无需在各页面重复实现 GetRecordCount 方法
- **一致的用户体验**：统一的加载状态和错误处理
- **三个界面使用相同的 UI 布局和样式**

### 扩展性增强
- **新增模式简单**：只需在 `SchemaCardConfiguration` 中添加工厂方法
- **配置灵活**：支持完全自定义配置或部分覆盖
- **组件可复用**：可在其他页面轻松复用
- **智能记录数显示**：组件自动异步获取记录数，也支持手动传入

### 功能改进
- **修复了标题点击问题**：使用 div 包装解决事件冲突
- **统一了路由规范**：DataView 路由改为使用 ID 而非名称
- **统一了 UI 样式**：三个界面都采用数据管理页面的最佳样式设计
- **高度内聚**：记录数获取逻辑内聚到组件中，各页面无需重复实现
- **异步友好**：记录数获取使用异步模式，提供加载状态
- **简化设计**：移除过度设计，统一芯片颜色、按钮样式、标签文本和时间格式
- **精简参数**：移除冗余的样式参数，直接在HTML中使用固定值，便于本地化

## 技术改进

### 现代C#语法应用

#### 集合表达式
```csharp
// 旧语法
MenuItems = new List<SchemaCardMenuItem>
{
    new() { Text = "编辑", Action = async _ => component.NavigateToEdit() },
    new() { Text = "删除", Action = async schema => await component.OnDeleteRequested.InvokeAsync(schema) }
};

// 新语法
MenuItems = [
    new SchemaCardMenuItem { Text = "编辑", Action = async _ => component.NavigateToEdit() },
    new SchemaCardMenuItem { Text = "删除", Action = async schema => await component.OnDeleteRequested.InvokeAsync(schema) }
];
```

#### 模式匹配
```csharp
// 旧语法
else if (_effectiveConfiguration.IsCardClickable && _effectiveConfiguration.CardClickAction != null)

// 新语法
else if (_effectiveConfiguration is { IsCardClickable: true, CardClickAction: not null })
```

#### 简化表达式
```csharp
// 旧语法
var styles = new List<string>();
if (!string.IsNullOrEmpty(Height))
    styles.Add($"height: {Height}");
else
    styles.Add("height: 100%");

// 新语法
var styles = new List<string> { !string.IsNullOrEmpty(Height) ? $"height: {Height}" : "height: 100%" };
```

### 异步架构改进

#### 组件生命周期
```csharp
protected override async Task OnParametersSetAsync()
{
    await base.OnParametersSetAsync();
    InitializeConfiguration();
    await LoadRecordCountIfNeededAsync(); // 异步加载记录数
}
```

#### 异步Action支持
```csharp
// 菜单项和按钮Action都支持异步
public Func<SchemaDefinition, Task?>? Action { get; set; }
```

## 参数精简

### 移除的冗余参数
在最终重构中，我们移除了以下冗余参数，直接在HTML中使用固定值：

- `ShowHeader` / `ShowMenu` / `IsClickable` - 通过 SchemaCardConfiguration 自动决定
- `RecordCountLabel` - 固定为"记录数"
- `UpdateTimeFormat` - 固定为"yyyy-MM-dd"
- `ChipColor` - 固定为Color.Default
- `ChipVariant` - 固定为Variant.Outlined
- `ChipClass` - 固定为"schema-card-chip"
- `TitleTypo` - 固定为Typo.h6

### 保留的核心参数
- `Schema` - 核心数据对象
- `Mode` - 决定组件行为的关键参数（当未提供 CustomConfiguration 时）
- `CustomConfiguration` - 完全自定义配置，优先级最高
- `RecordCount` - 可选的性能优化参数
- `ActionButtons` / `MenuItems` - 自定义覆盖（当未提供 CustomConfiguration 时）
- `OnCardClick` / `OnTitleClick` / `OnDeleteRequested` - 事件回调
- `ShowMetadata` / `ShowRecordCount` / `ShowUpdateTime` - 显示控制（当未提供 CustomConfiguration 时）
- `DescriptionLines` / `Height` / `Class` / `ChildContent` - 样式和扩展

### 优势
- **简化使用**：减少了不必要的参数传递
- **便于本地化**：固定文本直接在HTML中，便于后续国际化
- **减少错误**：避免了样式参数的不一致配置
- **提高性能**：减少了参数绑定和计算开销
- **配置清晰**：通过 CustomConfiguration 提供完全的自定义能力

## 最佳实践

1. **优先使用模式**：尽量使用预定义的模式，避免过度自定义
2. **保持一致性**：同一场景下使用相同的模式和配置
3. **合理传参**：只传递必要的参数，让组件自动处理其他配置
4. **事件处理**：对于特殊的删除逻辑，通过 `OnDeleteRequested` 回调处理
5. **性能优化**：如果已知记录数，通过 `RecordCount` 参数传入避免重复查询
6. **完全自定义**：对于复杂场景，使用 `CustomConfiguration` 提供完全的控制能力
7. **异步友好**：利用组件的异步记录数加载，提供更好的用户体验

## 未来扩展

可以考虑添加以下功能：
- **更多预定义模式**：如只读模式、紧凑模式等
- **主题切换支持**：支持深色主题等
- **动画效果配置**：卡片悬停、点击动画
- **批量操作支持**：多选、批量删除等
- **虚拟化支持**：大量卡片时的性能优化
- **国际化支持**：多语言文本配置
- **可访问性增强**：键盘导航、屏幕阅读器支持 