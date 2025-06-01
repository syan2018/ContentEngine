# ContentEngine 页面布局模板指南

本文档定义了 ContentEngine 项目中所有页面的统一布局模板和设计规范，确保整个应用的视觉一致性和用户体验。

## 基础页面模板

### 1. 标准页面结构

```razor
@page "/your-route"
@using ContentEngine.WebApp.Components.Shared
@inject YourService Service
@inject ISnackbar Snackbar

<PageTitle>页面标题 - ContentEngine</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="pa-4">
    <MudStack Spacing="6">
        <!-- 统一的面包屑导航 -->
        <PageBreadcrumb Page="PageBreadcrumb.PageType.YourPageType" 
                       SchemaName="@dynamicName" 
                       SchemaId="@dynamicId" />

        @if (isLoading)
        {
            <MudPaper Class="pa-8 text-center" Elevation="0">
                <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
                <MudText Class="mt-2">正在加载...</MudText>
            </MudPaper>
        }
        else if (hasError)
        {
            <MudPaper Class="pa-8 text-center" Elevation="0">
                <MudIcon Icon="@Icons.Material.Filled.Error" Size="Size.Large" Color="Color.Error" Class="mb-4" />
                <MudText Typo="Typo.h5" GutterBottom="true">错误标题</MudText>
                <MudText Class="mud-text-secondary">错误描述信息</MudText>
            </MudPaper>
        }
        else
        {
            <!-- 页面标题 -->
            <div>
                <MudText Typo="Typo.h4" GutterBottom="true" Class="page-title">页面主标题</MudText>
                <MudText Typo="Typo.body1" Color="Color.Secondary">页面描述信息</MudText>
            </div>

            <!-- 主要内容区域 -->
            <div>
                <!-- 您的页面内容 -->
            </div>
        }
    </MudStack>
</MudContainer>

<style>
    .page-title {
        font-weight: 600;
    }
</style>

@code {
    private bool isLoading = true;
    private bool hasError = false;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }
    
    private async Task LoadData()
    {
        isLoading = true;
        hasError = false;
        try
        {
            // 加载数据逻辑
        }
        catch (Exception ex)
        {
            hasError = true;
            Snackbar.Add($"加载失败: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

## 设计规范

### 1. 容器和间距

- **容器宽度**: 次级页面尽量使用 `MaxWidth.Large`，仅对部分关注信息展示的页面使用 `MaxWidth.ExtraLarge`
- **容器内边距**: `Class="pa-4"`
- **主要元素间距**: `<MudStack Spacing="6">`
- **次要元素间距**: 使用 `Class="mt-4"` 或 `Class="mb-4"`

### 2. 页面标题

- **标题层级**: 使用 `Typo.h4`
- **标题样式**: `Class="page-title"` (font-weight: 600)
- **描述文本**: `Typo.body1` + `Color.Secondary`
- **标题容器**: 包装在 `<div>` 中，作为独立的 Stack 项

### 3. 加载状态

```razor
<MudPaper Class="pa-8 text-center" Elevation="0">
    <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
    <MudText Class="mt-2">正在加载...</MudText>
</MudPaper>
```

### 4. 错误状态

```razor
<MudPaper Class="pa-8 text-center" Elevation="0">
    <MudIcon Icon="@Icons.Material.Filled.Error" Size="Size.Large" Color="Color.Error" Class="mb-4" />
    <MudText Typo="Typo.h5" GutterBottom="true">错误标题</MudText>
    <MudText Class="mud-text-secondary">错误描述</MudText>
</MudPaper>
```

### 5. 面包屑导航

- 统一使用 `PageBreadcrumb` 组件
- 根据页面类型选择合适的 `PageType`
- 传递必要的动态参数（如 SchemaName、SchemaId 等）

## 页面类型模板

### 1. 列表页面模板

```razor
<!-- 适用于：数据结构管理、AI推理列表等 -->
<div>
    <!-- 页面标题和操作按钮 -->
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
        <div>
            <MudText Typo="Typo.h4" Class="page-title">列表标题</MudText>
            <MudText Typo="Typo.body1" Color="Color.Secondary">列表描述</MudText>
        </div>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add">
            创建新项
        </MudButton>
    </MudStack>
</div>

<div>
    <!-- 列表内容 -->
    <MudDataGrid Items="@items" />
</div>
```

### 2. 详情页面模板

```razor
<!-- 适用于：数据浏览、推理实例详情等 -->
<div>
    <MudText Typo="Typo.h4" Class="page-title">@item.Name</MudText>
    <MudText Typo="Typo.body1" Color="Color.Secondary">@item.Description</MudText>
</div>

<div>
    <MudTabs Elevation="0" Rounded="false">
        <MudTabPanel Text="概览" Icon="@Icons.Material.Filled.Dashboard">
            <div class="mt-4">
                <!-- 概览内容 -->
            </div>
        </MudTabPanel>
        <MudTabPanel Text="详细信息" Icon="@Icons.Material.Filled.Info">
            <div class="mt-4">
                <!-- 详细信息内容 -->
            </div>
        </MudTabPanel>
    </MudTabs>
</div>
```

### 3. 表单页面模板

```razor
<!-- 适用于：创建数据结构、手动录入等 -->
<div>
    <MudText Typo="Typo.h4" Class="page-title">表单标题</MudText>
    <MudText Typo="Typo.body1" Color="Color.Secondary">表单描述</MudText>
</div>

<div>
    <MudForm @ref="form" Model="@model">
        <!-- 表单字段 -->
    </MudForm>
</div>

<div>
    <MudTabs Elevation="0" Rounded="false">
        <MudTabPanel Text="方式一" Icon="@Icons.Material.Filled.Edit">
            <div class="mt-4">
                <!-- 表单内容 -->
            </div>
        </MudTabPanel>
    </MudTabs>
</div>

<div>
    <!-- 操作按钮 -->
    <MudPaper Elevation="0" Class="pa-4" Style="border-top: 1px solid var(--mud-palette-divider);">
        <MudStack Row="true" Justify="Justify.SpaceBetween">
            <MudButton Variant="Variant.Text" Color="Color.Error">取消</MudButton>
            <MudButton Variant="Variant.Filled" Color="Color.Primary">保存</MudButton>
        </MudStack>
    </MudPaper>
</div>
```

## 组件使用规范

### 1. 标签页 (MudTabs)

```razor
<MudTabs Elevation="0" Rounded="false" Class="modern-tabs">
    <MudTabPanel Text="标签名" Icon="@Icons.Material.Filled.Icon">
        <div class="mt-4">
            <!-- 标签内容 -->
        </div>
    </MudTabPanel>
</MudTabs>

<style>
    .modern-tabs .mud-tabs-toolbar {
        border-bottom: 1px solid #E0E0E0;
    }
    
    .modern-tabs .mud-tab {
        text-transform: none;
        font-weight: 500;
    }
    
    .modern-tabs .mud-tab.mud-tab-active {
        color: #1976D2;
        border-bottom-color: #1976D2;
    }
</style>
```

### 2. 卡片组件 (MudCard)

```razor
<MudCard Elevation="2">
    <MudCardHeader>
        <CardHeaderContent>
            <div class="d-flex align-center">
                <MudIcon Icon="@Icons.Material.Filled.Icon" Class="mr-3" />
                <div>
                    <MudText Typo="Typo.h6">卡片标题</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">卡片描述</MudText>
                </div>
            </div>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudIconButton Icon="@Icons.Material.Filled.Help" Size="Size.Small" />
        </CardHeaderActions>
    </MudCardHeader>
    <MudCardContent>
        <!-- 卡片内容 -->
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Text">取消</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary">确认</MudButton>
    </MudCardActions>
</MudCard>
```

## 响应式设计

### 1. 网格布局

```razor
<MudGrid Spacing="3">
    <MudItem xs="12" sm="6" md="4">
        <!-- 内容 -->
    </MudItem>
</MudGrid>
```

### 2. 断点使用

- `xs="12"`: 手机端全宽
- `sm="6"`: 平板端半宽
- `md="4"`: 桌面端三分之一宽
- `lg="3"`: 大屏幕四分之一宽

## 颜色和主题

### 1. 主要颜色

- **主色**: `Color.Primary` (#1976D2)
- **次要文本**: `Color.Secondary`
- **错误**: `Color.Error`
- **成功**: `Color.Success`
- **警告**: `Color.Warning`

### 2. 文本层级

- **页面标题**: `Typo.h4` + `page-title` 类
- **节标题**: `Typo.h5`
- **卡片标题**: `Typo.h6`
- **正文**: `Typo.body1`
- **辅助文本**: `Typo.body2` + `Color.Secondary`

## 最佳实践

### 1. 代码组织

- 将页面逻辑封装在 `@code` 块中
- 使用有意义的变量名和方法名
- 添加适当的注释和文档

### 2. 错误处理

- 统一使用 `try-catch` 处理异常
- 通过 `ISnackbar` 显示用户友好的错误信息
- 记录详细的错误日志供调试使用

### 3. 性能优化

- 使用 `StateHasChanged()` 手动控制组件更新
- 避免在渲染方法中进行复杂计算
- 合理使用 `@key` 指令优化列表渲染

### 4. 可访问性

- 为所有交互元素提供适当的 ARIA 标签
- 确保键盘导航的可用性
- 使用语义化的 HTML 结构

## 示例页面

参考以下页面的实现：

- **AIDataEntry.razor**: AI 辅助录入页面
- **ManualEntry.razor**: 手动录入页面
- **CreatePage.razor**: 创建数据结构页面
- **DataView.razor**: 数据浏览页面

这些页面都遵循了本文档定义的布局模板和设计规范。 