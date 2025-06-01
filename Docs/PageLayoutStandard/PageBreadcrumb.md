# PageBreadcrumb 组件使用指南

`PageBreadcrumb` 是一个统一的面包屑导航组件，用于在所有页面中提供一致的导航体验。

## 基本用法

### 1. 使用预定义页面类型

```razor
@using ContentEngine.WebApp.Components.Shared

<!-- 数据结构管理页面 -->
<PageBreadcrumb Page="PageBreadcrumb.PageType.SchemaManagement" />

<!-- 创建数据结构页面 -->
<PageBreadcrumb Page="PageBreadcrumb.PageType.SchemaCreate" />

<!-- 手动录入页面 -->
<PageBreadcrumb Page="PageBreadcrumb.PageType.DataEntryManual" 
               SchemaName="@schema?.Name" 
               SchemaId="@SchemaId.ToString()" />

<!-- AI 辅助录入页面 -->
<PageBreadcrumb Page="PageBreadcrumb.PageType.DataEntryAI" 
               SchemaName="@schema?.Name" 
               SchemaId="@SchemaId" />

<!-- 数据浏览页面 -->
<PageBreadcrumb Page="PageBreadcrumb.PageType.DataExplorer" 
               SchemaName="@schemaDefinition?.Name" 
               SchemaId="@SchemaId" />

<!-- AI推理详情页面 -->
<PageBreadcrumb Page="PageBreadcrumb.PageType.AIInferenceDetail" 
               TaskName="@(definition?.Name ?? "实例详情")" 
               TaskId="@TaskId" />
```

### 2. 使用自定义面包屑

```razor
@{
    var customBreadcrumbs = new List<BreadcrumbItem>
    {
        PageBreadcrumb.CreateItem("首页", "/", Icons.Material.Filled.Home),
        PageBreadcrumb.CreateItem("自定义模块", "/custom", Icons.Material.Filled.Extension),
        PageBreadcrumb.CreateItem("当前页面", null, disabled: true)
    };
}

<PageBreadcrumb CustomItems="@customBreadcrumbs" />
```

### 3. 使用快速路径

```razor
@{
    var breadcrumbs = PageBreadcrumb.QuickPaths.DataEntry();
    breadcrumbs.Add(PageBreadcrumb.CreateItem("自定义页面", null, disabled: true));
}

<PageBreadcrumb CustomItems="@breadcrumbs" />
```

## 参数说明

| 参数名 | 类型 | 说明 |
|--------|------|------|
| `Page` | `PageType` | 预定义的页面类型 |
| `CustomItems` | `List<BreadcrumbItem>?` | 自定义面包屑项列表 |
| `SchemaName` | `string?` | 数据结构名称 |
| `SchemaId` | `string?` | 数据结构ID |
| `TaskName` | `string?` | 任务名称 |
| `TaskId` | `string?` | 任务ID |
| `CurrentPageTitle` | `string?` | 当前页面标题（用于Custom类型） |
| `Class` | `string?` | 额外的CSS类名 |

## 预定义页面类型

- `Dashboard` - 仪表板
- `SchemaManagement` - 数据结构管理
- `SchemaCreate` - 创建数据结构
- `SchemaEdit` - 编辑数据结构
- `SchemaDetails` - 数据结构详情
- `DataEntry` - 信息注入
- `DataEntryManual` - 手动录入
- `DataEntryAI` - AI 辅助录入
- `DataExplorer` - 数据洞察
- `AIInference` - AI推理
- `AIInferenceCreate` - 创建推理事务
- `AIInferenceDetail` - 推理实例详情
- `Custom` - 自定义（需要提供 CustomItems 或 CurrentPageTitle）

## 快速路径方法

- `PageBreadcrumb.QuickPaths.Home()` - 首页
- `PageBreadcrumb.QuickPaths.SchemaManagement()` - 数据结构管理
- `PageBreadcrumb.QuickPaths.DataEntry()` - 信息注入
- `PageBreadcrumb.QuickPaths.DataExplorer()` - 数据洞察
- `PageBreadcrumb.QuickPaths.AIInference()` - AI推理

## 样式自定义

组件包含默认样式，可以通过 `Class` 参数添加额外的CSS类：

```razor
<PageBreadcrumb Page="PageBreadcrumb.PageType.Dashboard" 
               Class="my-custom-breadcrumb" />
```

## 最佳实践

1. **一致性**: 在同类型页面中使用相同的面包屑模式
2. **动态内容**: 对于包含动态内容的页面（如Schema名称），确保传递正确的参数
3. **层级清晰**: 面包屑应该反映真实的页面层级关系
4. **简洁明了**: 避免过长的面包屑路径，保持在3-4级以内

## 迁移指南

### 从旧的 MudBreadcrumbs 迁移

**旧代码:**
```razor
<MudBreadcrumbs Items="breadcrumbItems" Class="mb-4" />

@code {
    private List<BreadcrumbItem> breadcrumbItems = new()
    {
        new BreadcrumbItem("首页", href: "/"),
        new BreadcrumbItem("信息注入", href: "/data-entry"),
        new BreadcrumbItem("手动录入", href: null, disabled: true)
    };
}
```

**新代码:**
```razor
<PageBreadcrumb Page="PageBreadcrumb.PageType.DataEntryManual" 
               SchemaName="@schema?.Name" />
```

这样可以大大简化代码，并确保所有页面的面包屑样式保持一致。 