# ContentEngine.WebApp Components 组织建议

## 🎯 当前结构分析

当前的 `Components` 文件夹结构已经相当不错，基本遵循了功能模块化的原则，但还有一些可以改进的地方。

## 📁 建议的优化结构

```
Components/
├── App.razor                          # 应用根组件
├── Routes.razor                       # 路由配置
├── _Imports.razor                     # 全局引用
│
├── Layout/                            # 布局组件
│   ├── MainLayout.razor
│   ├── MainLayout.razor.css
│   ├── NavMenu.razor
│   ├── NavMenu.razor.css
│   ├── Header.razor                   # 新增：顶部导航栏
│   ├── Sidebar.razor                  # 重构：侧边栏独立
│   └── Footer.razor                   # 新增：页脚（可选）
│
├── Pages/                             # 页面组件
│   ├── Dashboard/                     # 首页/仪表板
│   │   ├── Index.razor
│   │   └── Components/
│   │       ├── DashboardStats.razor
│   │       ├── QuickActions.razor
│   │       └── RecentActivity.razor
│   │
│   ├── SchemaManagement/              # 数据结构管理
│   │   ├── Index.razor
│   │   ├── CreatePage.razor
│   │   ├── EditPage.razor             # 新增：编辑页面
│   │   ├── DetailsPage.razor          # 新增：详情页面
│   │   ├── AiAssistedSchemaCreationForm.razor
│   │   ├── ManualSchemaCreationForm.razor
│   │   └── Components/
│   │       ├── SchemaCard.razor       # 新增：Schema 卡片组件
│   │       ├── SchemaTable.razor      # 新增：Schema 表格组件
│   │       └── FieldEditor.razor      # 新增：字段编辑器
│   │
│   ├── DataEntry/                     # 数据录入
│   │   ├── Index.razor
│   │   ├── AIDataEntry.razor
│   │   ├── ManualDataEntry.razor      # 新增：手动录入页面
│   │   └── Components/
│   │       ├── AIDataEntryWizard.razor
│   │       ├── SourceSelectionStep.razor
│   │       ├── MappingConfigStep.razor
│   │       ├── ExtractionPreviewStep.razor
│   │       ├── ResultsReviewStep.razor
│   │       └── DataEntryForm.razor    # 重构：通用数据录入表单
│   │
│   ├── DataExplorer/                  # 新增：数据浏览
│   │   ├── Index.razor
│   │   ├── [SchemaName]/
│   │   │   ├── Index.razor            # 数据列表页
│   │   │   └── [RecordId]/
│   │   │       └── Details.razor     # 记录详情页
│   │   └── Components/
│   │       ├── DataTable.razor
│   │       ├── DataFilter.razor
│   │       ├── RecordViewer.razor
│   │       └── NaturalLanguageQuery.razor
│   │
│   ├── AIInference/                   # 新增：AI 推理
│   │   ├── Index.razor
│   │   └── Components/
│   │       ├── TaskConfiguration.razor
│   │       ├── ResultsViewer.razor
│   │       └── PromptEditor.razor
│   │
│   ├── Settings/                      # 新增：设置
│   │   ├── Index.razor
│   │   └── Components/
│   │       ├── AIServiceSettings.razor
│   │       ├── SystemSettings.razor
│   │       └── UserSettings.razor
│   │
│   └── Others/                        # 其他页面
│       ├── Counter.razor
│       ├── Weather.razor
│       ├── Home.razor
│       └── Error.razor
│
├── Shared/                            # 共享组件
│   ├── ConfirmationDialog.razor
│   ├── FieldDefinitionTable.razor
│   ├── LoadingSpinner.razor           # 新增：加载指示器
│   ├── ErrorBoundary.razor            # 新增：错误边界
│   ├── Breadcrumbs.razor              # 新增：面包屑导航
│   └── UI/                            # 新增：基础 UI 组件
│       ├── Button.razor
│       ├── Card.razor
│       ├── Modal.razor
│       ├── Table.razor
│       ├── Form/
│       │   ├── Input.razor
│       │   ├── Select.razor
│       │   ├── Checkbox.razor
│       │   └── DatePicker.razor
│       └── Layout/
│           ├── Container.razor
│           ├── Grid.razor
│           └── Stack.razor
│
└── DataPipeline/                      # 数据管道相关组件
    ├── DynamicDataForm.razor
    └── Components/
        ├── FieldRenderer.razor        # 新增：字段渲染器
        ├── ValidationSummary.razor    # 新增：验证摘要
        └── DataPreview.razor          # 新增：数据预览
```

## 🔧 具体优化建议

### 1. 页面级组件优化

#### 当前问题：
- `DataView.razor` 直接放在 `DataPipeline` 文件夹下，应该移到 `Pages` 下
- 缺少专门的数据浏览模块
- 页面组件和业务组件混合

#### 建议改进：
```razor
<!-- 移动 DataView.razor 到合适位置 -->
Pages/DataExplorer/[SchemaName]/Index.razor  # 原 DataView.razor
```

### 2. 组件职责分离

#### 当前问题：
- `DynamicDataForm.razor` 既处理显示又处理数据逻辑
- 组件复用性不够高

#### 建议改进：
```razor
<!-- 分离关注点 -->
Shared/UI/Form/DynamicForm.razor           # 通用动态表单
DataPipeline/Components/FieldRenderer.razor # 字段渲染逻辑
DataPipeline/Components/DataValidator.razor # 数据验证逻辑
```

### 3. 布局组件重构

#### 当前问题：
- `MainLayout.razor` 包含了太多逻辑
- 导航菜单硬编码在布局中

#### 建议改进：
```razor
Layout/
├── MainLayout.razor        # 主布局框架
├── Header.razor           # 顶部导航（用户信息、通知等）
├── Sidebar.razor          # 侧边栏导航
└── Breadcrumbs.razor      # 面包屑导航
```

### 4. 共享组件库

#### 建议新增：
```razor
Shared/UI/
├── Button.razor           # 统一按钮样式
├── Card.razor            # 卡片组件
├── Modal.razor           # 模态框
├── Table.razor           # 表格组件
├── Pagination.razor      # 分页组件
└── Form/                 # 表单组件集合
    ├── Input.razor
    ├── Select.razor
    ├── Checkbox.razor
    └── DatePicker.razor
```

## 📋 迁移计划

### 阶段一：基础重构
1. 创建新的文件夹结构
2. 移动现有组件到合适位置
3. 更新命名空间和引用

### 阶段二：组件分离
1. 拆分大型组件
2. 提取可复用逻辑
3. 创建共享组件库

### 阶段三：功能完善
1. 添加缺失的页面组件
2. 完善数据浏览功能
3. 添加设置和帮助页面

## 🎨 命名约定

### 页面组件
- 使用 `PascalCase`
- 页面主组件命名为 `Index.razor`
- 子页面使用描述性名称：`CreatePage.razor`, `EditPage.razor`

### 业务组件
- 使用 `PascalCase`
- 包含业务逻辑的组件添加业务前缀：`SchemaCard.razor`, `DataTable.razor`

### UI 组件
- 使用 `PascalCase`
- 基础 UI 组件使用简洁名称：`Button.razor`, `Card.razor`

## 🔄 组件通信模式

### 父子组件通信
```csharp
// 使用 Parameter 和 EventCallback
[Parameter] public SchemaDefinition Schema { get; set; }
[Parameter] public EventCallback<List<FieldDefinition>> OnFieldsChanged { get; set; }
```

### 跨组件状态管理
```csharp
// 使用依赖注入的服务
@inject ISchemaStateService SchemaState
@inject INotificationService Notifications
```

### 组件生命周期
```csharp
// 统一的生命周期管理
protected override async Task OnInitializedAsync()
{
    await LoadDataAsync();
}

protected override async Task OnParametersSetAsync()
{
    await RefreshDataAsync();
}
```

## 📊 性能优化建议

### 1. 组件懒加载
```razor
@* 使用 Lazy 组件加载 *@
<Lazy Component="typeof(HeavyComponent)" />
```

### 2. 虚拟化长列表
```razor
@* 使用 Virtualize 组件 *@
<Virtualize Items="@largeDataSet" Context="item">
    <ItemTemplate>
        <DataRow Item="@item" />
    </ItemTemplate>
</Virtualize>
```

### 3. 缓存策略
```csharp
// 在服务层实现缓存
@inject ICacheService Cache

private async Task<List<Schema>> GetSchemasAsync()
{
    return await Cache.GetOrSetAsync("schemas", 
        () => SchemaService.GetAllAsync(), 
        TimeSpan.FromMinutes(5));
}
```

这个重构计划将帮助你构建一个更加模块化、可维护和可扩展的组件架构。 