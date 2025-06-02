import os
import io
import shutil
import tempfile
from fastapi import FastAPI, File, UploadFile, HTTPException
from markitdown import MarkItDown

# 初始化 FastAPI 应用
app = FastAPI(
    title="MarkItDown FastAPI Service",
    description="一个使用 MarkItDown 包将文件转换为 Markdown 的 FastAPI 服务，支持图片转换为 Data URI。",
    version="0.2.0",
)

# 在应用启动时创建 MarkItDown 转换器实例
# 启用插件以支持更多文件格式和图片处理
try:
    md_converter = MarkItDown(enable_plugins=True)
except Exception as e:
    # 如果 MarkItDown 初始化失败，应用可能无法正常工作
    print(f"Error initializing MarkItDown: {e}")
    md_converter = None 

@app.post("/convert/", summary="将文件转换为 Markdown", tags=["Conversion"])
async def convert_file_to_markdown(
    file: UploadFile = File(..., description="要转换为 Markdown 的文件。"),
    keep_data_uris: bool = True
):
    """
    接收一个文件，使用 MarkItDown 将其内容转换为 Markdown 格式。
    默认启用图片转换为 Data URI，以支持文档中的图片内联显示。
    """
    if md_converter is None:
        raise HTTPException(status_code=500, detail="MarkItDown converter is not initialized.")

    try:
        # 读取文件内容到内存
        content = await file.read()
        
        # 使用 convert_stream 方法处理文件流
        # 这样可以避免创建临时文件，提高性能
        result = md_converter.convert_stream(
            io.BytesIO(content),
            file_extension=os.path.splitext(file.filename)[1] if file.filename else None,
            keep_data_uris=keep_data_uris
        )
        
        # 返回包含原始文件名和 Markdown 内容的 JSON 响应
        return {
            "filename": file.filename,
            "markdown_content": result.text_content
        }
    except Exception as e:
        # 如果转换过程中发生任何错误，则返回 HTTP 500 错误
        raise HTTPException(status_code=500, detail=f"Error converting file: {str(e)}")
    finally:
        # 确保关闭上传的文件流
        if file:
            await file.close()

@app.get("/health", summary="健康检查", tags=["Health"])
async def health_check():
    """
    健康检查端点，用于验证服务是否正常运行。
    """
    return {
        "status": "healthy",
        "converter_initialized": md_converter is not None
    }

# 如果您想直接运行此文件进行测试 (例如，使用 uvicorn main:app --reload)
# 可以取消注释以下代码块：
# if __name__ == "__main__":
#     import uvicorn
#     uvicorn.run(app, host="0.0.0.0", port=8000) 