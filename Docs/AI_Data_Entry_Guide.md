# AI 辅助数据录入功能使用指南

## 概述

AI 辅助数据录入功能允许用户通过上传文件、输入URL或直接粘贴文本，利用AI自动提取结构化数据并保存到数据库中。

## 功能特点

- **多数据源支持**: 文件上传、URL抓取、文本输入
- **智能文件转换**: 集成MarkItDown API，支持PDF、Word、Excel、PowerPoint等格式
- **AI智能提取**: 使用ConfigurableAIProvider调用各种AI服务进行数据结构化
- **灵活映射配置**: 支持自动映射和手动字段映射两种模式
- **实时预览验证**: 提取过程可视化，数据质量检查
- **批量处理**: 支持一对一和批量提取两种模式

## 使用流程

### 1. 准备工作

#### 1.1 启动MarkItDown服务
```bash
cd SupportServices/MarkItDown
python main.py
```
服务将在 `http://localhost:8000` 启动。

#### 1.2 配置AI服务
确保在 `Profiles/` 目录下有正确的AI配置文件，例如：
- `Profiles/Agents/ContentEngineHelper/agent.yaml`
- `Profiles/Connections/` 下的连接配置

### 2. 创建数据结构定义

在使用AI辅助录入前，需要先创建目标数据结构：

1. 访问 "Schema Management" 页面
2. 创建新的Schema定义，包含所需字段
3. 设置字段类型（文本、数字、布尔、日期）和是否必填

### 3. AI辅助录入流程

#### 步骤1: 选择数据源
- **文件上传**: 支持拖拽上传，支持格式包括PDF、Word、Excel、PowerPoint、文本文件等
- **URL输入**: 输入网页地址，系统自动抓取内容
- **文本输入**: 直接粘贴包含目标数据的文本内容
- **提取模式选择**:
  - 一对一提取：每个数据源生成一条记录
  - 批量提取：从每个数据源提取多条记录

#### 步骤2: 配置字段映射
- **自动映射**: AI自动分析数据源并提取所有字段，可添加自定义提取指令
- **手动映射**: 为每个字段指定具体的提取规则或路径

#### 步骤3: AI提取预览
- 系统调用AI服务进行数据提取
- 实时显示提取进度和状态
- 提供提取结果概览（成功/失败数量、总记录数）
- 支持查看每个数据源的详细提取结果
- 可以重新提取失败的数据源

#### 步骤4: 确认保存
- 数据质量检查：显示字段填充率、有效值统计
- 数据预览表格：查看所有提取的记录
- 保存选项：可选择跳过验证或允许部分保存
- 最终保存到数据库

## 测试功能

### 快速测试
1. 访问 "Test Data Entry" 页面
2. 点击"创建测试Schema"按钮
3. 使用提供的示例文本进行测试

### 示例数据
系统提供了角色信息的示例数据，包含：
- 姓名、种族、职业、等级、技能、所属等字段
- 多个角色的结构化信息

## 技术架构

### 核心组件
- **FileConversionService**: 文件转换服务，集成MarkItDown API
- **DataStructuringService**: AI数据结构化服务，使用ConfigurableAIProvider
- **AIDataEntryWizard**: 主要的UI向导组件
- **SourceSelectionStep**: 数据源选择组件
- **MappingConfigStep**: 字段映射配置组件
- **ExtractionPreviewStep**: AI提取预览组件
- **ResultsReviewStep**: 结果确认组件

### 数据流
1. 用户选择数据源 → DataSource模型
2. 配置字段映射 → Dictionary<string, string>
3. AI提取数据 → ExtractionResult模型
4. 验证和保存 → BsonDocument存储到LiteDB

## 配置说明

### appsettings.json
```json
{
  "MarkItDownApi": {
    "BaseUrl": "http://localhost:8000"
  }
}
```

### 支持的文件格式
- 文档: PDF, Word (.docx, .doc), PowerPoint (.pptx, .ppt)
- 表格: Excel (.xlsx, .xls), CSV
- 文本: TXT, Markdown, HTML, JSON, XML, RTF

## 故障排除

### 常见问题
1. **MarkItDown API连接失败**: 确保Python服务正在运行
2. **AI提取失败**: 检查AI配置文件和连接设置
3. **文件上传失败**: 确认文件格式是否支持
4. **数据保存失败**: 检查数据库连接和Schema定义

### 日志查看
系统会记录详细的操作日志，包括：
- 文件转换过程
- AI调用结果
- 数据验证错误
- 保存操作状态

## 扩展开发

### 添加新的文件格式支持
在 `FileConversionService` 中添加新的MIME类型和扩展名。

### 自定义AI提取逻辑
修改 `DataStructuringService` 中的提示词构建逻辑。

### 添加新的验证规则
在 `ValidateRecordsAsync` 方法中添加自定义验证逻辑 