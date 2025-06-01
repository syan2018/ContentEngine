# 页面布局快速参考

## 🚀 快速开始

### 基础模板
```razor
<MudContainer MaxWidth="MaxWidth.Large" Class="pa-4">
    <MudStack Spacing="6">
        <PageBreadcrumb Page="PageBreadcrumb.PageType.YourType" />
        
        <!-- 加载状态 -->
        @if (isLoading) { /* 加载组件 */ }
        else if (hasError) { /* 错误组件 */ }
        else {
            <!-- 页面标题 -->
            <div>
                <MudText Typo="Typo.h4" Class="page-title">标题</MudText>
                <MudText Typo="Typo.body1" Color="Color.Secondary">描述</MudText>
            </div>
            
            <!-- 主要内容 -->
            <div>内容区域</div>
        }
    </MudStack>
</MudContainer>

<style>
    .page-title { font-weight: 600; }
</style>
```

## 📐 设计规范

| 元素 | 规范 |
|------|------|
| 容器宽度 | 尽量使用 `MaxWidth.Large` |
| 容器内边距 | `Class="pa-4"` |
| 主要间距 | `<MudStack Spacing="6">` |
| 页面标题 | `Typo.h4` + `page-title` 类 |
| 描述文本 | `Typo.body1` + `Color.Secondary` |

## 🎨 常用组件

### 加载状态
```razor
<MudPaper Class="pa-8 text-center" Elevation="0">
    <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
    <MudText Class="mt-2">正在加载...</MudText>
</MudPaper>
```

### 错误状态
```razor
<MudPaper Class="pa-8 text-center" Elevation="0">
    <MudIcon Icon="@Icons.Material.Filled.Error" Size="Size.Large" Color="Color.Error" Class="mb-4" />
    <MudText Typo="Typo.h5" GutterBottom="true">错误标题</MudText>
    <MudText Class="mud-text-secondary">错误描述</MudText>
</MudPaper>
```

### 标签页
```razor
<MudTabs Elevation="0" Rounded="false">
    <MudTabPanel Text="标签名" Icon="@Icons.Material.Filled.Icon">
        <div class="mt-4">内容</div>
    </MudTabPanel>
</MudTabs>
```

### 卡片
```razor
<MudCard Elevation="2">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">标题</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>内容</MudCardContent>
    <MudCardActions>
        <MudButton>操作</MudButton>
    </MudCardActions>
</MudCard>
```

## 🔗 面包屑类型

| 页面类型 | PageType |
|----------|----------|
| 仪表板 | `Dashboard` |
| 数据结构管理 | `SchemaManagement` |
| 创建数据结构 | `SchemaCreate` |
| 手动录入 | `DataEntryManual` |
| AI 辅助录入 | `DataEntryAI` |
| 数据浏览 | `DataExplorer` |
| AI推理 | `AIInference` |
| 推理详情 | `AIInferenceDetail` |

## 🎯 页面类型模板

### 列表页面
```razor
<div>
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
        <div>
            <MudText Typo="Typo.h4" Class="page-title">列表标题</MudText>
            <MudText Typo="Typo.body1" Color="Color.Secondary">描述</MudText>
        </div>
        <MudButton Variant="Variant.Filled" Color="Color.Primary">创建</MudButton>
    </MudStack>
</div>
```

### 表单页面
```razor
<div>
    <MudText Typo="Typo.h4" Class="page-title">表单标题</MudText>
    <MudText Typo="Typo.body1" Color="Color.Secondary">描述</MudText>
</div>

<div>
    <MudForm @ref="form">表单内容</MudForm>
</div>

<div>
    <MudPaper Elevation="0" Class="pa-4" Style="border-top: 1px solid var(--mud-palette-divider);">
        <MudStack Row="true" Justify="Justify.SpaceBetween">
            <MudButton Variant="Variant.Text" Color="Color.Error">取消</MudButton>
            <MudButton Variant="Variant.Filled" Color="Color.Primary">保存</MudButton>
        </MudStack>
    </MudPaper>
</div>
```

## 🎨 颜色参考

| 用途 | 颜色 |
|------|------|
| 主色 | `Color.Primary` |
| 次要文本 | `Color.Secondary` |
| 错误 | `Color.Error` |
| 成功 | `Color.Success` |
| 警告 | `Color.Warning` |

## 📱 响应式断点

| 断点 | 用途 |
|------|------|
| `xs="12"` | 手机端全宽 |
| `sm="6"` | 平板端半宽 |
| `md="4"` | 桌面端三分之一 |
| `lg="3"` | 大屏幕四分之一 |

## ✅ 检查清单

- [ ] 使用 `MaxWidth.Large` 容器
- [ ] 设置 `pa-4` 内边距
- [ ] 使用 `MudStack Spacing="6"`
- [ ] 添加 `PageBreadcrumb` 组件
- [ ] 页面标题使用 `Typo.h4` + `page-title` 类
- [ ] 统一的加载和错误状态
- [ ] 内容区域包装在 `<div>` 中
- [ ] 添加 `.page-title` 样式 