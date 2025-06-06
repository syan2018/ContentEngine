using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Forms;
using static ContentEngine.Core.DataPipeline.Services.FileConversionService;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 文件转换服务接口
/// </summary>
public interface IFileConversionService
{
    /// <summary>
    /// 将文件转换为纯文本内容
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换后的文本内容</returns>
    Task<string> ConvertFileToTextAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将浏览器文件转换为纯文本内容
    /// </summary>
    /// <param name="file">浏览器上传的文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换后的文本内容</returns>
    Task<string> ConvertFileToTextAsync(IBrowserFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用选项将文件转换为文本内容
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="options">转换选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换结果</returns>
    Task<FileConversionResult> ConvertFileToTextWithOptionsAsync(IFormFile file, FileConversionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用选项将浏览器文件转换为文本内容
    /// </summary>
    /// <param name="file">浏览器上传的文件</param>
    /// <param name="options">转换选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>转换结果</returns>
    Task<FileConversionResult> ConvertFileToTextWithOptionsAsync(IBrowserFile file, FileConversionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查文件是否支持转换
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="mimeType">MIME类型</param>
    /// <returns>是否支持转换</returns>
    bool IsFileSupported(string fileName, string mimeType);

    /// <summary>
    /// 从URL获取内容
    /// </summary>
    /// <param name="url">URL地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>网页内容</returns>
    Task<string> GetContentFromUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用 Jina Reader API 的高级选项获取URL内容
    /// </summary>
    /// <param name="url">URL地址</param>
    /// <param name="options">Jina Reader 选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>网页内容</returns>
    Task<string> GetContentFromUrlWithOptionsAsync(string url, JinaReaderOptions? options = null, CancellationToken cancellationToken = default);
} 