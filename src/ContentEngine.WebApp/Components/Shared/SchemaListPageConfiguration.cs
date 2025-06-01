using ContentEngine.Core.DataPipeline.Models;
using MudBlazor;

namespace ContentEngine.WebApp.Components.Shared;

/// <summary>
/// Schema列表页面配置类
/// </summary>
public class SchemaListPageConfiguration
{
    /// <summary>
    /// 页面标题
    /// </summary>
    public string PageTitle { get; set; } = "数据管理";

    /// <summary>
    /// 页面描述
    /// </summary>
    public string PageDescription { get; set; } = "管理您的数据结构定义";

    /// <summary>
    /// 搜索框占位符文本
    /// </summary>
    public string SearchPlaceholder { get; set; } = "搜索数据结构...";

    /// <summary>
    /// 加载中文本
    /// </summary>
    public string LoadingText { get; set; } = "正在加载数据结构列表...";

    /// <summary>
    /// 空状态图标
    /// </summary>
    public string EmptyStateIcon { get; set; } = Icons.Material.Filled.LibraryBooks;

    /// <summary>
    /// 空状态标题
    /// </summary>
    public string EmptyStateTitle { get; set; } = "暂无数据结构";

    /// <summary>
    /// 空状态描述
    /// </summary>
    public string EmptyStateDescription { get; set; } = "点击右上角创建新数据结构开始吧！";

    /// <summary>
    /// 无搜索结果标题
    /// </summary>
    public string NoSearchResultsTitle { get; set; } = "未找到匹配的数据结构";

    /// <summary>
    /// 无搜索结果描述
    /// </summary>
    public string NoSearchResultsDescription { get; set; } = "尝试修改您的搜索词或清除筛选条件。";

    /// <summary>
    /// 是否显示创建按钮
    /// </summary>
    public bool ShowCreateButton { get; set; } = false;

    /// <summary>
    /// 创建按钮文本
    /// </summary>
    public string CreateButtonText { get; set; } = "创建新数据结构";

    /// <summary>
    /// 创建按钮点击事件
    /// </summary>
    public Action? OnCreateClick { get; set; }

    /// <summary>
    /// Schema卡片模式
    /// </summary>
    public SchemaCard.SchemaCardMode SchemaCardMode { get; set; } = SchemaCard.SchemaCardMode.Management;

    /// <summary>
    /// 删除请求事件处理器
    /// </summary>
    public Func<SchemaDefinition, Task>? OnDeleteRequested { get; set; }

    /// <summary>
    /// 是否启用分页
    /// </summary>
    public bool EnablePagination { get; set; } = false;

    /// <summary>
    /// 每页显示数量
    /// </summary>
    public int PageSize { get; set; } = 8;
} 