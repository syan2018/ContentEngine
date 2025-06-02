using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ContentEngine.Core.DataPipeline.Services;

/// <summary>
/// 文件转换选项
/// </summary>
public class FileConversionOptions
{
    /// <summary>
    /// 是否包含图片内容（Data URI）
    /// </summary>
    public bool IncludeImages { get; set; } = true;
    
    /// <summary>
    /// 是否保留 Data URI 格式的图片
    /// </summary>
    public bool KeepDataUris { get; set; } = true;
}

/// <summary>
/// 文件转换结果
/// </summary>
public class FileConversionResult
{
    /// <summary>
    /// 转换后的文本内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否包含图片
    /// </summary>
    public bool HasImages { get; set; }
    
    /// <summary>
    /// 图片数量
    /// </summary>
    public int ImageCount { get; set; }
    
    /// <summary>
    /// 不包含图片的纯文本内容（用于 AI 处理）
    /// </summary>
    public string TextOnlyContent { get; set; } = string.Empty;
}

/// <summary>
/// 文件转换服务实现
/// </summary>
public class FileConversionService : IFileConversionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileConversionService> _logger;
    private readonly string _markItDownApiUrl;

    // Data URI 图片的正则表达式
    private static readonly Regex DataUriImageRegex = new(
        @"!\[([^\]]*)\]\(data:image/[^;]+;base64,[A-Za-z0-9+/=]+\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

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
        
        // 为 Jina Reader API 配置 User-Agent
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ContentEngine/1.0 (Data Processing Tool)");
        }
    }

    public async Task<string> ConvertFileToTextAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var result = await ConvertFileToTextWithOptionsAsync(file, new FileConversionOptions(), cancellationToken);
        return result.Content;
    }

    public async Task<string> ConvertFileToTextAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        var result = await ConvertFileToTextWithOptionsAsync(file, new FileConversionOptions(), cancellationToken);
        return result.Content;
    }

    /// <summary>
    /// 使用选项转换文件到文本
    /// </summary>
    public async Task<FileConversionResult> ConvertFileToTextWithOptionsAsync(IFormFile file, FileConversionOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("文件不能为空", nameof(file));
        }

        options ??= new FileConversionOptions();

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
                var textContent = await reader.ReadToEndAsync(cancellationToken);
                return new FileConversionResult
                {
                    Content = textContent,
                    TextOnlyContent = textContent,
                    HasImages = false,
                    ImageCount = 0
                };
            }

            // 调用 MarkItDown API
            using var formContent = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            formContent.Add(streamContent, "file", file.FileName);
            formContent.Add(new StringContent(options.KeepDataUris.ToString().ToLower()), "keep_data_uris");

            var response = await _httpClient.PostAsync($"{_markItDownApiUrl}/convert/", formContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MarkItDown API 调用失败: {StatusCode}, {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"文件转换失败: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MarkItDownResponse>(responseContent);
            var markdownContent = result?.MarkdownContent ?? string.Empty;

            return ProcessMarkdownContent(markdownContent, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件转换过程中发生错误: {FileName}", file.FileName);
            throw;
        }
    }

    /// <summary>
    /// 使用选项转换文件到文本
    /// </summary>
    public async Task<FileConversionResult> ConvertFileToTextWithOptionsAsync(IBrowserFile file, FileConversionOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Size == 0)
        {
            throw new ArgumentException("文件不能为空", nameof(file));
        }

        options ??= new FileConversionOptions();

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
                var textContent = await reader.ReadToEndAsync(cancellationToken);
                return new FileConversionResult
                {
                    Content = textContent,
                    TextOnlyContent = textContent,
                    HasImages = false,
                    ImageCount = 0
                };
            }

            // 调用 MarkItDown API
            using var formContent = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
            using var streamContent = new StreamContent(fileStream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            formContent.Add(streamContent, "file", file.Name);
            formContent.Add(new StringContent(options.KeepDataUris.ToString().ToLower()), "keep_data_uris");

            var response = await _httpClient.PostAsync($"{_markItDownApiUrl}/convert/", formContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MarkItDown API 调用失败: {StatusCode}, {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"文件转换失败: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MarkItDownResponse>(responseContent);
            var markdownContent = result?.MarkdownContent ?? string.Empty;

            return ProcessMarkdownContent(markdownContent, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件转换过程中发生错误: {FileName}", file.Name);
            throw;
        }
    }

    /// <summary>
    /// 处理 Markdown 内容，分离图片和文本
    /// </summary>
    private FileConversionResult ProcessMarkdownContent(string markdownContent, FileConversionOptions options)
    {
        var imageMatches = DataUriImageRegex.Matches(markdownContent);
        var hasImages = imageMatches.Count > 0;
        
        // 创建不包含图片的纯文本版本
        var textOnlyContent = DataUriImageRegex.Replace(markdownContent, match =>
        {
            var altText = match.Groups[1].Value;
            return string.IsNullOrEmpty(altText) ? "[图片]" : $"[图片: {altText}]";
        });

        var finalContent = options.IncludeImages ? markdownContent : textOnlyContent;

        _logger.LogInformation("处理 Markdown 内容完成，包含 {ImageCount} 张图片", imageMatches.Count);

        return new FileConversionResult
        {
            Content = finalContent,
            TextOnlyContent = textOnlyContent,
            HasImages = hasImages,
            ImageCount = imageMatches.Count
        };
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
            // 使用 Jina Reader API 获取LLM友好的纯文本内容
            var jinaReaderUrl = $"https://r.jina.ai/{url}";
            _logger.LogInformation("使用 Jina Reader API 获取URL内容: {JinaUrl}", jinaReaderUrl);
            
            var response = await _httpClient.GetAsync(jinaReaderUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Jina Reader 返回的是 Markdown 格式的纯文本内容，非常适合LLM处理
            _logger.LogInformation("成功从URL获取内容，长度: {ContentLength} 字符", content.Length);
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "使用 Jina Reader API 获取URL内容失败: {Url}", url);
            
            // 如果 Jina Reader API 失败，回退到直接获取原始内容
            _logger.LogInformation("回退到直接获取原始URL内容: {Url}", url);
            try
            {
                var fallbackResponse = await _httpClient.GetAsync(uri, cancellationToken);
                fallbackResponse.EnsureSuccessStatusCode();
                
                var fallbackContent = await fallbackResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("使用回退方式获取到原始HTML内容，长度: {ContentLength} 字符", fallbackContent.Length);
                return fallbackContent;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "回退方式也失败，无法获取URL内容: {Url}", url);
                throw new HttpRequestException($"无法获取URL内容，Jina Reader API和直接访问都失败: {url}", fallbackEx);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从URL获取内容过程中发生未预期错误: {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// 使用 Jina Reader API 的高级选项获取URL内容
    /// </summary>
    /// <param name="url">URL地址</param>
    /// <param name="options">Jina Reader 选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>网页内容</returns>
    public async Task<string> GetContentFromUrlWithOptionsAsync(string url, JinaReaderOptions? options = null, CancellationToken cancellationToken = default)
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
            // 构建 Jina Reader URL
            var jinaReaderUrl = $"https://r.jina.ai/{url}";
            _logger.LogInformation("使用 Jina Reader API 获取URL内容: {JinaUrl}", jinaReaderUrl);
            
            // 创建请求消息以添加自定义头部
            using var request = new HttpRequestMessage(HttpMethod.Get, jinaReaderUrl);
            
            // 添加 Jina Reader 特定的头部选项
            if (options != null)
            {
                if (options.WithGeneratedAlt)
                {
                    request.Headers.Add("X-With-Generated-Alt", "true");
                }
                
                if (options.NoCache)
                {
                    request.Headers.Add("X-No-Cache", "true");
                }
                
                if (options.CacheTolerance.HasValue)
                {
                    request.Headers.Add("X-Cache-Tolerance", options.CacheTolerance.Value.ToString());
                }
                
                if (!string.IsNullOrEmpty(options.TargetSelector))
                {
                    request.Headers.Add("X-Target-Selector", options.TargetSelector);
                }
                
                if (!string.IsNullOrEmpty(options.WaitForSelector))
                {
                    request.Headers.Add("X-Wait-For-Selector", options.WaitForSelector);
                }
                
                if (options.Timeout.HasValue)
                {
                    request.Headers.Add("X-Timeout", options.Timeout.Value.ToString());
                }
            }
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation("成功从URL获取内容，长度: {ContentLength} 字符", content.Length);
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "使用 Jina Reader API 获取URL内容失败: {Url}", url);
            
            // 如果 Jina Reader API 失败，回退到直接获取原始内容
            _logger.LogInformation("回退到直接获取原始URL内容: {Url}", url);
            try
            {
                var fallbackResponse = await _httpClient.GetAsync(uri, cancellationToken);
                fallbackResponse.EnsureSuccessStatusCode();
                
                var fallbackContent = await fallbackResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("使用回退方式获取到原始HTML内容，长度: {ContentLength} 字符", fallbackContent.Length);
                return fallbackContent;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "回退方式也失败，无法获取URL内容: {Url}", url);
                throw new HttpRequestException($"无法获取URL内容，Jina Reader API和直接访问都失败: {url}", fallbackEx);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从URL获取内容过程中发生未预期错误: {Url}", url);
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

    /// <summary>
    /// Jina Reader API 选项
    /// </summary>
    public class JinaReaderOptions
    {
        /// <summary>
        /// 是否为图片生成alt文本描述
        /// </summary>
        public bool WithGeneratedAlt { get; set; } = false;
        
        /// <summary>
        /// 是否绕过缓存
        /// </summary>
        public bool NoCache { get; set; } = false;
        
        /// <summary>
        /// 缓存容忍度（秒）
        /// </summary>
        public int? CacheTolerance { get; set; }
        
        /// <summary>
        /// 目标CSS选择器，用于提取页面特定部分
        /// </summary>
        public string? TargetSelector { get; set; }
        
        /// <summary>
        /// 等待特定元素出现的CSS选择器
        /// </summary>
        public string? WaitForSelector { get; set; }
        
        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int? Timeout { get; set; }
    }
} 