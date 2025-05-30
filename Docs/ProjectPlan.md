# ContentEngine 项目推进规划

**文档版本:** 1.0
**日期:** 2025-05-07
**核心目标:** 构建一个利用 AI 对非结构化数据进行理解、结构化处理、存储和推理的引擎，实现内容自动化生成与管理。

---

## 总体路线图

项目将分阶段进行，每个阶段都有明确的目标和可交付成果。这种迭代方法有助于风险管理、早期反馈收集和持续价值交付。

1.  **阶段一: 夯实 Schema 管理和手动数据闭环 (基础核心)**
2.  **阶段二: 引入 AI 辅助 Schema 生成 (初步智能化)**
3.  **阶段三: 实现 AI 辅助数据结构化与录入 (核心 AI 功能)**
4.  **阶段四: 增强数据检阅与查询能力 (数据应用与洞察)**
5.  **阶段五: AI 配置与系统完善 (易用性与扩展性)**
6.  **阶段六及以后: 探索高级特性 (远期目标)**

---

## 阶段一: 夯实 Schema 管理和手动数据闭环

**目标:** 确保系统能够稳定地定义、存储和管理数据结构 (Schema)，并实现完整的手动数据录入、查看、编辑和删除流程。为后续 AI 功能的集成打下坚实基础。

**预计周期:** [例如: 4-6 周]

**核心模块:**
*   `ContentEngine.Core`: Models, Services (SchemaDefinitionService, DataEntryService), Storage (LiteDbContext)
*   `ContentEngine.WebApp`: Components (Schema管理页面, 手动数据录入页面, 数据浏览页面)

**主要任务与可交付成果:**

1.  **Schema 定义模块增强:**
    *   [ ] **任务:** 完善 `SchemaDefinition` 模型，支持多种基础字段类型 (如 Text, Number, Date, Boolean) 及复杂类型 (如嵌套对象定义，列表/数组定义)。
    *   [ ] **任务:** 优化 `SchemaDefinitionService`，确保对 Schema 的增、删、改、查操作稳定可靠。
    *   [ ] **可交付成果:** 能够通过后端服务创建和管理具有不同字段类型的 Schema。

2.  **Schema 管理页面 (WebApp):**
    *   [ ] **任务:** 优化现有 `Schema管理页面` UI/UX。
    *   [ ] **任务:** 实现 Schema 列表展示、创建新 Schema (动态添加字段、选择字段类型)、编辑现有 Schema、删除 Schema 的完整前端交互。
    *   [ ] **可交付成果:** 用户可以通过界面直观地管理所有 Schema 定义。

3.  **手动数据录入模块:**
    *   [ ] **任务:** 设计和实现 `DataEntryService` (或在现有服务中扩展)，用于根据指定的 Schema 保存 `BsonDocument` 数据。
    *   [ ] **任务:** 开发 `手动数据录入页面`:
        *   [ ] 用户选择一个已存在的 Schema。
        *   [ ] 页面根据选定的 Schema 动态生成数据输入表单。
        *   [ ] 用户填写表单后，数据能正确转换为 `BsonDocument` 并保存到 LiteDB 中对应 Schema 名称的集合。
    *   [ ] **可交付成果:** 用户能够为任何已定义的 Schema 手动录入结构化数据。

4.  **数据浏览与编辑模块 (基础版):**
    *   [ ] **任务:** 开发 `数据浏览页面`:
        *   [ ] 用户选择一个 Schema。
        *   [ ] 列表展示该 Schema 下所有已录入的数据记录 (考虑分页)。
        *   [ ] 支持查看单条记录的详细内容。
        *   [ ] 支持编辑单条记录 (动态表单)。
        *   [ ] 支持删除单条记录。
    *   [ ] **可交付成果:** 用户能够浏览、查看、修改和删除已录入的数据。

5.  **数据存储层健壮性:**
    *   [ ] **任务:** 确保 `LiteDbContext` 的配置和使用正确无误，Schema 定义与数据实例分开存储且关联正确。
    *   [ ] **任务:** 建立基本的错误处理和日志记录机制。
    *   [ ] **可交付成果:** 稳定可靠的数据持久化层。

**阶段验收标准:**
*   所有 Schema 管理功能按预期工作。
*   可以为任意定义的 Schema 成功录入、查看、修改和删除数据。
*   核心数据流转顺畅，无明显 Bug。

---

## 阶段二: 引入 AI 辅助 Schema 生成

**目标:** 初步集成 AI 能力，通过 LLM 辅助用户根据自然语言描述或样例数据快速生成 Schema 结构，提高 Schema 定义效率。

**预计周期:** [例如: 3-4 周]

**核心模块:**
*   `ConfigurableAIProvider`: 提供 AI Kernel 实例。
*   `ContentEngine.Core.AI`: 新增 `SchemaSuggestionService`。
*   `ContentEngine.WebApp`: 改造 `Schema管理页面` 或创建新的 `智能建表页面`。

**主要任务与可交付成果:**

1.  **`ConfigurableAIProvider` 集成与配置:**
    *   [ ] **任务:** 确保 `ConfigurableAIProvider` 能够正确加载 AI模型配置 (如 OpenAI API Key, Endpoint, Model ID 等)。
    *   [ ] **任务:** 编写基础的 Prompt 工程脚本，用于指导 LLM 从文本生成 Schema 建议。
    *   [ ] **可交付成果:** AI Kernel 能够被成功调用并返回结果。

2.  **`SchemaSuggestionService` 开发 (`ContentEngine.Core.AI`):**
    *   [ ] **任务:** 定义 `ISchemaSuggestionService` 接口。
    *   [ ] **任务:** 实现 `SchemaSuggestionService`，该服务接收用户输入 (如文本描述、样例数据)，调用 AI Kernel (通过 `IAIKernelFactory`)，并解析 LLM 返回结果，将其转换为 `SchemaDefinition` 对象或其中间表示。
    *   [ ] **可交付成果:** 后端服务能够根据输入生成 Schema 建议。

3.  **前端集成 - AI 辅助建表:**
    *   [ ] **任务:** 在 `Schema管理页面` 增加“AI辅助创建”入口，或开发独立的 `智能建表页面`。
    *   [ ] **任务:** 用户界面允许输入：
        *   一段描述项目或数据需求的文本。
        *   (可选) 一些样例数据片段。
    *   [ ] **任务:** 调用后端 `SchemaSuggestionService`。
    *   [ ] **任务:** 展示 AI 返回的 Schema 建议（字段名、推测的类型等），并允许用户进行修改、增删字段，最终确认创建 Schema。
    *   [ ] **可交付成果:** 用户可以通过 AI 辅助快速创建 Schema。

**阶段验收标准:**
*   AI 服务配置正确，能够成功调用 LLM。
*   用户输入描述后，系统能够生成合理的 Schema 建议。
*   用户可以基于 AI 的建议进行调整并成功创建 Schema。

---

## 阶段三: 实现 AI 辅助数据结构化与录入

**目标:** 实现项目的核心 AI 功能之一：将用户输入的非结构化或半结构化文本，根据选定的 Schema，自动转换为结构化数据并支持录入。

**预计周期:** [例如: 4-6 周]

**核心模块:**
*   `ConfigurableAIProvider`
*   `ContentEngine.Core.AI`: 新增 `DataStructuringService`。
*   `ContentEngine.WebApp`: 开发 `智能数据录入页面`。

**主要任务与可交付成果:**

1.  **`DataStructuringService` 开发 (`ContentEngine.Core.AI`):**
    *   [ ] **任务:** 定义 `IDataStructuringService` 接口。
    *   [ ] **任务:** 实现 `DataStructuringService`，该服务接收：
        *   一个已选定的 `SchemaDefinition`。
        *   用户输入的非结构化/半结构化文本数据。
    *   [ ] **任务:** 精心设计 Prompt，指导 LLM 根据给定的 Schema 从文本中提取信息并结构化。
    *   [ ] **任务:** 解析 LLM 返回的结构化数据 (可能是 JSON 或类似格式)，并将其转换为符合目标 Schema 的 `BsonDocument` (或中间字典结构)。
    *   [ ] **可交付成果:** 后端服务能够将文本输入根据 Schema 结构化。

2.  **`智能数据录入页面` 开发 (`ContentEngine.WebApp`):**
    *   [ ] **任务:** 用户选择一个已存在的 `Schema`。
    *   [ ] **任务:** 用户输入大段非结构化或半结构化文本。
    *   [ ] **任务:** 调用后端 `DataStructuringService`。
    *   [ ] **任务:** 页面展示 AI 处理后的结构化数据预览 (例如，以表单形式预填，或 JSON 预览)，并允许用户进行编辑和修正。
    *   [ ] **任务:** 用户确认后，将结构化数据保存到对应的 LiteDB 集合中。
    *   [ ] **(可选) 任务:** 探索批量处理接口，允许用户上传文件或粘贴多条记录进行批量结构化。
    *   [ ] **可交付成果:** 用户可以通过 AI 辅助将非结构化文本快速录入为结构化数据。

**阶段验收标准:**
*   用户选择 Schema 并输入文本后，系统能够生成符合 Schema 的结构化数据预览。
*   用户可以编辑 AI 生成的数据并成功保存。
*   对于常见的文本格式和 Schema 结构，AI 结构化准确率达到可接受水平。

---

## 阶段四: 增强数据检阅与查询能力

**目标:** 提供更灵活、更强大的数据访问、查询和分析能力，使用户能更好地利用已结构化的数据。

**预计周期:** [例如: 3-5 周]

**核心模块:**
*   `ContentEngine.Core`: 增强 `DataEntryService` 或新增查询服务。
*   `ContentEngine.WebApp`: 增强 `数据浏览与查询页面`。
*   `(可选) ContentEngine.Core.AI`: `QueryGenerationService`。

**主要任务与可交付成果:**

1.  **高级数据筛选与排序:**
    *   [ ] **任务:** 在 `数据浏览与查询页面` 增加基于字段的动态筛选功能 (例如，等于、包含、大于、小于等)。
    *   [ ] **任务:** 实现基于单个或多个字段的排序功能。
    *   [ ] **可交付成果:** 用户可以更精确地查找和组织数据。

2.  **数据聚合与摘要 (基础):**
    *   [ ] **任务:** 探索在查询结果上方显示简单的聚合信息 (如：总条目数，某个数字字段的平均值/总和等，视 Schema 而定)。
    *   [ ] **可交付成果:** 为用户提供数据的概览性洞察。

3.  **(高级/可选) 自然语言查询接口:**
    *   [ ] **任务:** 研究和设计 `QueryGenerationService` (`ContentEngine.Core.AI`)，将用户的自然语言查询转换为 LiteDB 查询语句 (或等效的过滤条件)。
    *   [ ] **任务:** 在 `数据浏览与查询页面` 集成自然语言查询输入框。
    *   [ ] **可交付成果 (原型):** 用户可以通过简单的自然语言进行数据查询。

4.  **(可选) 数据可视化初步探索:**
    *   [ ] **任务:** 集成简单的图表库 (如 Chart.js)，针对特定类型的查询结果提供基础的图表展示 (如柱状图、饼图)。
    *   [ ] **可交付成果 (原型):** 特定数据的可视化展示。

**阶段验收标准:**
*   数据筛选和排序功能稳定可用。
*   (如果实施) 自然语言查询能够处理常见的简单查询场景。

---

## 阶段五: AI 配置与系统完善

**目标:** 提供必要的系统级配置能力，特别是针对 AI 服务。同时，对现有功能进行全面测试、优化用户体验、完善错误处理和文档。

**预计周期:** [例如: 2-4 周]

**核心模块:**
*   `ContentEngine.WebApp`: 开发 `AI 服务配置页面`。
*   整体项目: 代码审查、测试、文档编写。

**主要任务与可交付成果:**

1.  **`AI 服务配置页面` 开发:**
    *   [ ] **任务:** 允许管理员在界面上配置 AI 提供商的参数 (如 API Keys, Endpoints, 默认模型等)。这些配置应安全存储。
    *   [ ] **(可选) 任务:** 如果 Prompt 逻辑复杂且希望用户可调，提供 Prompt 模板管理功能。
    *   [ ] **可交付成果:** AI 服务参数可由用户通过 UI 配置。

2.  **用户体验 (UX) 优化:**
    *   [ ] **任务:** 收集用户反馈 (即使是内部团队)，优化整体操作流程和界面布局。
    *   [ ] **任务:** 确保所有页面响应速度合理，提供清晰的加载和操作反馈。
    *   [ ] **可交付成果:** 更流畅、更直观的用户操作体验。

3.  **错误处理与日志记录:**
    *   [ ] **任务:** 全面审查和增强错误处理机制，向用户显示友好的错误提示，并在后端记录详细日志。
    *   [ ] **可交付成果:** 系统更健壮，问题更易于追踪和诊断。

4.  **测试:**
    *   [ ] **任务:** 编写单元测试和集成测试，覆盖核心功能模块。
    *   [ ] **任务:** 进行全面的手动测试，确保所有功能符合预期。
    *   [ ] **可交付成果:** 提高代码质量和系统稳定性。

5.  **文档完善:**
    *   [ ] **任务:** 更新或撰写用户手册、开发者文档。
    *   [ ] **任务:** 清理和规范化代码注释。
    *   [ ] **可交付成果:** 完整的项目文档。

**阶段验收标准:**
*   AI 服务可配置。
*   系统整体稳定性和用户体验得到提升。
*   关键模块有测试覆盖，文档齐全。

---

## 阶段六及以后: 探索高级特性

**目标:** 根据项目愿景 (`README.zh-CN.md`)，逐步实现更高级的 AI 功能，如多模态数据理解、推理引擎、离线内容生成等。

**预计周期:** 持续迭代

**可能探索的方向 (具体任务待详细规划):**

*   [ ] **推理引擎 (`InferenceService`):**
    *   基于结构化数据进行逻辑推理、关系推断、行为预测。
    *   例如：`角色特征 + 活动` -> NPC行为模式。
*   [ ] **多模态数据输入支持:**
    *   处理图像、音频等非文本数据，并将其结构化。
*   [ ] **离线内容生成与引擎集成:**
    *   研究如何在本地生成内容，并提供插件接口供外部引擎 (如游戏引擎) 调用。
*   [ ] **更复杂的知识图谱构建与应用。**
*   [ ] **持续优化 AI 模型和 Prompt 工程。**

---

**重要注意事项:**

*   **迭代与反馈:** 每个阶段结束后，进行回顾，收集反馈，并根据实际情况调整后续计划。
*   **技术选型评估:** 在各阶段开始前，确认当前技术选型是否仍然合适，必要时进行调整。
*   **用户中心:** 始终从用户需求和价值出发设计和实现功能。
*   **代码质量:** 遵循最佳实践，保持代码清晰、可维护。

祝项目顺利！