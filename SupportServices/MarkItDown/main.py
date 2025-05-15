import os
import shutil
import tempfile
from fastapi import FastAPI, File, UploadFile, HTTPException
from markitdown import MarkItDown

# 初始化 FastAPI 应用
app = FastAPI(
    title="MarkItDown FastAPI Service",
    description="一个使用 MarkItDown 包将文件转换为 Markdown 的 FastAPI 服务。",
    version="0.1.0",
)

# 在应用启动时创建 MarkItDown 转换器实例
# 默认情况下，插件是禁用的，并且没有配置 Azure Document Intelligence。
# 如果需要这些功能，可以在这里配置 MarkItDown 的初始化参数。
# 例如: md_converter = MarkItDown(enable_plugins=True, docintel_endpoint="YOUR_ENDPOINT")
try:
    md_converter = MarkItDown(enable_plugins=False)
except Exception as e:
    # 如果 MarkItDown 初始化失败，应用可能无法正常工作。
    # 考虑记录错误或采取其他措施。
    print(f"Error initializing MarkItDown: {e}")
    # 可以选择让应用在无法初始化转换器时无法启动，或者允许启动但端点会失败。
    # 这里我们允许启动，但端点调用时会因为 md_converter 未正确初始化而失败。
    md_converter = None 

@app.post("/convert/", summary="将文件转换为 Markdown", tags=["Conversion"])
async def convert_file_to_markdown(file: UploadFile = File(..., description="要转换为 Markdown 的文件。")):
    """
    接收一个文件，使用 MarkItDown 将其内容转换为 Markdown 格式，并返回结果。
    """
    if md_converter is None:
        raise HTTPException(status_code=500, detail="MarkItDown converter is not initialized.")

    tmp_file_path = None  # 初始化变量以确保在 finally 中可用
    try:
        # 为了让 MarkItDown 能够处理文件，我们首先将其保存到临时位置。
        # NamedTemporaryFile 用于获取一个文件名，这通常是 MarkItDown.convert() 所需的。
        # delete=False 确保文件在 with 块关闭后不会立即删除，以便 MarkItDown 可以访问它。
        # 后缀取自上传的文件名，这可能有助于 MarkItDown 正确识别文件类型。
        with tempfile.NamedTemporaryFile(delete=False, suffix=os.path.splitext(file.filename)[1]) as tmp_file:
            shutil.copyfileobj(file.file, tmp_file)
            tmp_file_path = tmp_file.name
        
        # 使用 MarkItDown 执行转换
        # MarkItDown().convert() 方法接受一个文件路径。
        result = md_converter.convert(tmp_file_path)
        
        # 返回包含原始文件名和 Markdown 内容的 JSON 响应
        return {
            "filename": file.filename,
            "markdown_content": result.text_content
        }
    except Exception as e:
        # 如果转换过程中发生任何错误，则返回 HTTP 500 错误。
        raise HTTPException(status_code=500, detail=f"Error converting file: {str(e)}")
    finally:
        # 清理：确保关闭上传的文件流。
        if file:
            await file.close()
        # 清理：删除创建的临时文件。
        if tmp_file_path and os.path.exists(tmp_file_path):
            os.remove(tmp_file_path)

# 如果您想直接运行此文件进行测试 (例如，使用 uvicorn main:app --reload)
# 可以取消注释以下代码块：
# if __name__ == "__main__":
#     import uvicorn
#     uvicorn.run(app, host="0.0.0.0", port=8000) 