using System.Text.RegularExpressions;

namespace ContentEngine.Core.Utils;

/// <summary>
/// Data URI 图片提取器
/// </summary>
public static class DataUriImageExtractor
{
    // Data URI 图片的正则表达式
    private static readonly Regex DataUriImageRegex = new(
        @"!\[([^\]]*)\]\(data:image/([^;]+);base64,([A-Za-z0-9+/=]+)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// 从 Markdown 内容中提取所有图片信息
    /// </summary>
    /// <param name="markdownContent">Markdown 内容</param>
    /// <returns>图片信息列表</returns>
    public static List<ImageInfo> ExtractImages(string markdownContent)
    {
        var images = new List<ImageInfo>();
        var matches = DataUriImageRegex.Matches(markdownContent);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var altText = match.Groups[1].Value;
            var mimeType = match.Groups[2].Value;
            var base64Data = match.Groups[3].Value;

            images.Add(new ImageInfo
            {
                Index = i,
                AltText = string.IsNullOrEmpty(altText) ? $"图片 {i + 1}" : altText,
                MimeType = $"image/{mimeType}",
                Base64Data = base64Data,
                DataUri = $"data:image/{mimeType};base64,{base64Data}", // 构建完整的 Data URI
                FullMatch = match.Value
            });
        }

        return images;
    }

    /// <summary>
    /// 将 Markdown 内容中的图片替换为占位符
    /// </summary>
    /// <param name="markdownContent">原始 Markdown 内容</param>
    /// <param name="placeholder">占位符模板，{0} 为图片索引，{1} 为 alt 文本</param>
    /// <returns>替换后的内容</returns>
    public static string ReplaceImagesWithPlaceholder(string markdownContent, string placeholder = "[图片 {0}: {1}]")
    {
        var index = 0;
        return DataUriImageRegex.Replace(markdownContent, match =>
        {
            var altText = match.Groups[1].Value;
            var displayText = string.IsNullOrEmpty(altText) ? "无描述" : altText;
            return string.Format(placeholder, ++index, displayText);
        });
    }

    /// <summary>
    /// 检查内容是否包含 Data URI 图片
    /// </summary>
    /// <param name="content">要检查的内容</param>
    /// <returns>是否包含图片</returns>
    public static bool ContainsImages(string content)
    {
        return DataUriImageRegex.IsMatch(content);
    }

    /// <summary>
    /// 获取内容中的图片数量
    /// </summary>
    /// <param name="content">要检查的内容</param>
    /// <returns>图片数量</returns>
    public static int GetImageCount(string content)
    {
        return DataUriImageRegex.Matches(content).Count;
    }
}

/// <summary>
/// 图片信息
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// 图片索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Alt 文本
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// MIME 类型
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Base64 数据
    /// </summary>
    public string Base64Data { get; set; } = string.Empty;

    /// <summary>
    /// 完整的 Data URI（不包含 Markdown 语法）
    /// </summary>
    public string DataUri { get; set; } = string.Empty;

    /// <summary>
    /// 完整的 Markdown 匹配文本
    /// </summary>
    public string FullMatch { get; set; } = string.Empty;

    /// <summary>
    /// 获取估算的文件大小（字节）
    /// </summary>
    public long EstimatedSize => (long)(Base64Data.Length * 0.75); // Base64 编码大约增加 33% 大小
} 