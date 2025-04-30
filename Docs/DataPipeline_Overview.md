# Data Pipeline Overview (Schema Definition & Data Entry)

本文档概述了 ContentEngine 中数据管道模块的核心设计和组件，重点关注用户自定义 Schema 的定义、管理以及基于这些 Schema 的数据录入和存储。

## 核心概念

数据管道的目标是允许用户灵活地定义他们想要管理的数据结构（Schema），并能够录入符合这些结构的数据实例。为了实现最大的灵活性，我们采用以下核心概念：

1.  **动态 Schema 定义**: 用户可以在运行时创建和修改数据结构（Schema），而无需重新编译代码。这些 Schema 定义本身作为数据存储在数据库中。
2.  **Schema 定义存储**: 所有的 `SchemaDefinition` 对象都存储在 LiteDB 数据库的一个**固定集合**中，名为 `_schemas`。
3.  **动态数据存储**: 对于用户定义的**每一种** Schema（例如 "CharacterCard"），系统会创建一个**同名的 LiteDB 集合**（例如 `CharacterCards`）。该 Schema 的所有数据实例都存储在这个对应的集合中。
4.  **BsonDocument**: 由于每个 Schema 的结构不同，对应集合中存储的数据实例采用 `LiteDB.BsonDocument` 类型。这允许每个文档拥有不同的字段和结构，与关系型数据库的固定列模式形成对比。
5.  **表单驱动**: UI 界面（尤其是数据录入表单）需要根据选定的 `SchemaDefinition` 动态生成。

## 主要组件

### 1. 数据模型 (`Core/DataPipeline/Models`)

*   **`SchemaDefinition.cs`**: 定义了数据结构模板的核心模型。包含 Schema 的名称 (`Name`)、描述 (`Description`) 以及字段列表 (`Fields`)。
*   **`FieldDefinition.cs`**: 定义了 Schema 中的单个字段。包含字段名 (`Name`)、数据类型 (`Type`)、是否必填 (`IsRequired`) 以及引用类型所需的目标 Schema 名称 (`ReferenceSchemaName`)。
*   **`FieldType.cs`**: 枚举类型，定义了字段支持的基本数据类型（`Text`, `Number`, `Boolean`, `Date`, `Reference`）。

### 2. 存储层 (`Core/Storage`)

*   **`LiteDbContext.cs`**: 封装了与 LiteDB 数据库的交互。
    *   管理 `LiteDatabase` 实例（通常注册为单例）。
    *   提供对 `_schemas` 集合的强类型访问 (`SchemaDefinitions` 属性)。
    *   提供 `GetDataCollection(string schemaName)` 方法来获取或创建存储具体数据实例的 `ILiteCollection<BsonDocument>`。
    *   配置必要的索引（例如 `SchemaDefinition.Name` 的唯一索引）。

### 3. 服务层 (`Core/DataPipeline/Services`)

*   **`ISchemaDefinitionService.cs` / `SchemaDefinitionService.cs`**: 负责 `SchemaDefinition` 对象的 CRUD 操作。
    *   与 `LiteDbContext.SchemaDefinitions` 集合交互。
    *   提供按 ID 或名称查找 Schema 的方法。
    *   包含基本的验证逻辑（例如，防止重复的 Schema 名称）。
*   **`IDataEntryService.cs` / `DataEntryService.cs`**: 负责处理具体数据实例 (`BsonDocument`) 的 CRUD 操作。
    *   依赖 `LiteDbContext` 来获取目标 Schema 对应的 `ILiteCollection<BsonDocument>`。
    *   依赖 `ISchemaDefinitionService` 来验证目标 Schema 是否存在。
    *   提供创建、读取（单个/列表/分页）、更新、删除数据实例以及计数的方法。

### 4. 工具类 (`Core/Utils`)

*   **`BsonFormUtils.cs`**: 提供静态辅助方法，用于在后端 `BsonDocument` 和前端表单数据（目前使用 `Dictionary<string, object?>`）之间进行转换。
    *   `ConvertBsonToFormData()`: 将 `BsonDocument` 转换为适合表单绑定的字典，处理类型转换和默认值。
    *   `ConvertFormDataToBson()`: 将表单字典转换回 `BsonDocument`，处理类型检查和 BSON 值创建。
    *   `GetDisplayValue()`: 将 `BsonDocument` 中的特定字段值格式化为用户友好的字符串，用于表格显示。
    *   `GetEntryKey()`: 获取 `BsonDocument` 的 `_id` 字符串表示，用作 Blazor `@key` 指令。

### 5. UI 层 (`Components/Pages`, `Components/DataPipeline`)

*   **`SchemaManagement.razor`**: 页面组件，用于：
    *   显示所有已定义的 Schema 列表。
    *   提供创建新 Schema（包括定义其字段）的表单。
    *   提供编辑和删除 Schema 的入口点（按钮）。
    *   提供查看特定 Schema 数据的入口点（按钮）。
*   **`DataView.razor`**: 页面组件，用于处理特定 Schema 的数据：
    *   接收 `SchemaName` 作为路由参数。
    *   显示该 Schema 下所有数据实例的分页列表（表格）。
    *   表头和单元格内容根据 `SchemaDefinition` 动态生成。
    *   提供添加新数据实例和编辑/删除现有实例的表单。
    *   使用 `<DynamicDataForm>` 组件来渲染表单。
*   **`DynamicDataForm.razor`**: 可复用的 UI 组件，负责：
    *   接收 `SchemaDefinition` 和 `Dictionary<string, object?>` (FormData) 作为参数。
    *   根据 Schema 动态生成标准的 HTML `<input>` 元素。
    *   处理用户输入，将更改写回 `FormData` 字典，并通过 `EventCallback` 通知父组件。

## 工作流程示例

1.  **定义 Schema**: 用户访问 `/schemas` 页面 (`SchemaManagement`)，填写表单创建一个名为 "NPC" 的 Schema，包含 "Name"(Text), "Health"(Number), "IsHostile"(Boolean) 字段。`SchemaDefinitionService` 将此定义保存到 `_schemas` 集合。
2.  **录入数据**: 用户在 `/schemas` 页面点击 "NPC" Schema 对应的 "View Data" 按钮，导航到 `/data/NPC` (`DataView`)。
3.  **显示表单**: `DataView` 加载 "NPC" 的 `SchemaDefinition`。用户点击 "Add New Entry"。`DataView` 调用 `BsonFormUtils.ConvertBsonToFormData`（传入空 BsonDocument 和 Schema）生成一个包含默认值的 `formData` 字典。`DynamicDataForm` 根据 Schema 和 `formData` 渲染出 Name, Health, IsHostile 的输入框。
4.  **用户输入**: 用户在表单中输入 "Goblin", 50, true。`DynamicDataForm` 的 `@onchange` 事件处理器更新 `DataView` 中的 `formData` 字典，并触发 `HandleFormFieldChanged`。
5.  **验证与保存**: `HandleFormFieldChanged` 调用 `ValidateFormData`。用户点击 "Save Entry"，`HandleDataSubmit` 被调用。它再次调用 `ValidateFormData`，然后调用 `BsonFormUtils.ConvertFormDataToBson` 将 `formData` 转回 `BsonDocument`。最后，调用 `DataEntryService.CreateDataAsync` 将 `BsonDocument` 保存到名为 `NPC` 的 LiteDB 集合中。
6.  **显示数据**: `HandleDataSubmit` 调用 `LoadSchemaAndData` 刷新页面，新录入的 "Goblin" 数据出现在表格中，单元格内容由 `BsonFormUtils.GetDisplayValue` 格式化。

## 后续考虑

*   **数据验证**: 目前验证逻辑比较基础，需要根据 `FieldDefinition` 中的 `IsRequired` 和可能的其他约束（如数字范围、文本长度、正则表达式）进行增强。
*   **引用类型 (Reference)**: `FieldType.Reference` 的 UI 实现（输入和显示）需要完善，例如使用下拉列表或搜索框选择关联数据，并在显示时能展示关联项的关键信息而非仅仅是 ObjectId。
*   **错误处理**: 提供更细粒度的错误反馈给用户。
*   **复杂类型**: 如果需要支持列表或嵌套对象作为字段类型，需要扩展模型、转换逻辑和 UI 组件。
*   **Schema 演化**: 如何处理 Schema 定义变更后，已存在的旧数据实例？ 