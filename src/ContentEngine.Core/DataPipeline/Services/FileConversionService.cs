using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 文件转换服务实现
/// </summary>
public class FileConversionService : IFileConversionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileConversionService> _logger;
    private readonly string _markItDownApiUrl;

    // 支持的文件类型
    private readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt",
        ".html", ".htm", ".json", ".xml", ".csv", ".rtf"
    };

    private readonly HashSet<string> _supportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain", "text/markdown", "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-powerpoint",
        "text/html", "application/json", "application/xml", "text/csv",
        "application/rtf"
    };

    public FileConversionService(HttpClient httpClient, IConfiguration configuration, ILogger<FileConversionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _markItDownApiUrl = configuration.GetValue<string>("MarkItDownApi:BaseUrl") ?? "http://localhost:8000";
    }

    public async Task<string> ConvertFileToTextAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("文件不能为空", nameof(file));
        }

        // 检查文件是否支持
        if (!IsFileSupported(file.FileName, file.ContentType))
        {
            throw new NotSupportedException($"不支持的文件类型: {file.FileName}");
        }

        try
        {
            // 如果是纯文本文件，直接读取
            if (file.ContentType?.StartsWith("text/") == true)
            {
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                return await reader.ReadToEndAsync(cancellationToken);
            }

            // 调用 MarkItDown API
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(streamContent, "file", file.FileName);

            var response = await _httpClient.PostAsync($"{_markItDownApiUrl}/convert/", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MarkItDown API 调用失败: {StatusCode}, {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"文件转换失败: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MarkItDownResponse>(responseContent);
            
            return result?.MarkdownContent ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件转换过程中发生错误: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<string> ConvertFileToTextAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Size == 0)
        {
            throw new ArgumentException("文件不能为空", nameof(file));
        }

        // 检查文件是否支持
        if (!IsFileSupported(file.Name, file.ContentType))
        {
            throw new NotSupportedException($"不支持的文件类型: {file.Name}");
        }

        try
        {
            // 如果是纯文本文件，直接读取
            if (file.ContentType?.StartsWith("text/") == true)
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return await reader.ReadToEndAsync(cancellationToken);
            }

            // 调用 MarkItDown API
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
            using var streamContent = new StreamContent(fileStream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(streamContent, "file", file.Name);

            var response = await _httpClient.PostAsync($"{_markItDownApiUrl}/convert/", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MarkItDown API 调用失败: {StatusCode}, {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"文件转换失败: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MarkItDownResponse>(responseContent);
            
            return result?.MarkdownContent ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件转换过程中发生错误: {FileName}", file.Name);
            throw;
        }
    }

    public bool IsFileSupported(string fileName, string mimeType)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        return _supportedExtensions.Contains(extension) || 
               (!string.IsNullOrEmpty(mimeType) && _supportedMimeTypes.Contains(mimeType));
    }

    public async Task<string> GetContentFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("URL不能为空", nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("无效的URL格式", nameof(url));
        }

        try
        {
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // 如果是HTML内容，可以考虑进一步处理提取纯文本
            // 这里简单返回原始内容，实际项目中可能需要HTML解析
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从URL获取内容失败: {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// MarkItDown API 响应模型
    /// </summary>
    private class MarkItDownResponse
    {
        public string? Filename { get; set; }
        
        [JsonPropertyName("markdown_content")]
        public string? MarkdownContent { get; set; }
    }
} 