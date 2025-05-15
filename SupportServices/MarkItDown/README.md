# MarkItDown FastAPI Service

这是一个使用 `markitdown` Python 包将各种文件格式转换为 Markdown 文本的 FastAPI 服务。

## 功能

- 提供一个 HTTP API 端点 `/convert/` 用于文件转换。
- 支持 `markitdown` 包所支持的所有文件类型。
- 易于部署和使用。

## 依赖

- 本地存在 Python 环境，且版本最好高于 3.11

## 设置开发环境

项目提供了跨平台的设置脚本来帮助您快速配置本地开发环境，包括创建 Python 虚拟环境和安装必要的依赖项。

请根据您的操作系统选择并运行相应的脚本：

### Windows (PowerShell)

1.  打开 PowerShell。
2.  如果您首次运行本地脚本，可能需要调整执行策略。以管理员身份打开 PowerShell 并运行：
    ```powershell
    Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
    ```
    然后按 `Y` 确认。
3.  导航到项目根目录并运行设置脚本：
    ```powershell
    .\setup_venv.ps1
    ```
4.  脚本执行完毕后，根据提示激活虚拟环境：
    ```powershell
    .\.venv\Scripts\Activate.ps1
    ```

### Linux / macOS (Bash Shell)

1.  打开您的终端。
2.  导航到项目根目录。
3.  首先，给脚本添加执行权限：
    ```bash
    chmod +x setup_venv.sh
    ```
4.  运行设置脚本：
    ```bash
    ./setup_venv.sh
    ```
5.  脚本执行完毕后，根据提示激活虚拟环境：
    ```bash
    source .venv/bin/activate
    ```

激活虚拟环境后，您的终端提示符通常会显示 `(.venv)` 前缀，表明虚拟环境已激活。

## 运行 FastAPI 服务

有两种方式可以运行 FastAPI 服务：

### 1. (推荐) 手动激活环境后运行

确保您的虚拟环境已激活 (如上一节所述)，然后在项目根目录下运行以下命令启动 FastAPI 服务：

```bash
uvicorn main:app --reload
```

### 2. 使用运行脚本 (尝试自动使用 .venv)

项目也提供了便捷的运行脚本，它们会尝试使用当前目录下的 `.venv` 环境来启动服务。在项目根目录下，根据您的操作系统运行：

-   **PowerShell**:
    ```powershell
    .\run_service.ps1
    ```
-   **Linux / macOS** (确保已执行 `chmod +x run_service.sh`):
    ```bash
    ./run_service.sh
    ```

无论哪种方式，服务默认都运行在 `http://127.0.0.1:8000`。

`--reload` 参数会在您修改代码时自动重新加载服务，非常适合开发环境。

## API 端点

### `POST /convert/`

此端点用于将上传的文件转换为 Markdown 文本。

-   **请求**: `multipart/form-data`
    -   `file`: 要转换的文件 (必需)。
-   **成功响应 (200 OK)**:
    ```json
    {
        "filename": "your_uploaded_file.ext",
        "markdown_content": "... Converted markdown text ..."
    }
    ```
-   **错误响应**:
    -   `422 Unprocessable Entity`: 如果没有上传文件。
    -   `500 Internal Server Error`: 如果转换过程中发生错误或 `MarkItDown` 初始化失败。

您可以在浏览器中访问 `http://127.0.0.1:8000/docs` 来查看和测试自动生成的 OpenAPI (Swagger) 文档。

## 项目结构

```
MarkitDown/
├── .venv/                  # Python 虚拟环境 (由设置脚本创建)
├── main.py                 # FastAPI 应用逻辑
├── requirements.txt        # Python 依赖列表
├── setup_venv.ps1          # Windows PowerShell 设置脚本
├── setup_venv.sh           # Linux/macOS Shell 设置脚本
├── run_service.ps1         # Windows PowerShell 快速运行脚本
├── run_service.sh          # Linux/macOS 快速运行脚本
└── README.md               # 本文档
```

## 注意事项

-   当前 `MarkItDown` 转换器在 `main.py` 中是以插件禁用 (`enable_plugins=False`) 且未配置 Azure Document Intelligence 的方式初始化的。如果需要这些高级功能，请修改 `main.py` 中 `MarkItDown()` 的初始化参数。
