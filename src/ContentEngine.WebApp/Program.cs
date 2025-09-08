using ContentEngine.WebApp.Components;
using ContentEngine.WebApp.Components.Hubs;
using ContentEngine.Core.Storage;
using ContentEngine.Core.DataPipeline.Services;
using ContentEngine.Core.AI.Services;
using ConfigurableAIProvider.Extensions;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using MudBlazor.Services;
using ContentEngine.Core.Inference.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// 添加 SignalR 服务
builder.Services.AddSignalR();

// 1. 添加本地化服务
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 注册 LiteDB 上下文为单例服务
builder.Services.AddSingleton<LiteDbContext>();

// 注册 Schema 管理服务 (可以使用 Scoped 或 Transient)
builder.Services.AddScoped<ISchemaDefinitionService, SchemaDefinitionService>();

// 注册数据实例管理服务 (可以使用 Scoped 或 Transient)
builder.Services.AddScoped<IDataEntryService, DataEntryService>();

// 注册字段编辑服务
builder.Services.AddScoped<IFieldEditService, FieldEditService>();

// *** 测试：注册 ConfigurableAIProvider 相关服务 ***
builder.Services.AddConfigurableAIProvider(builder.Configuration);
// ******************************************

// 注册 Schema 建议服务
builder.Services.AddScoped<ISchemaSuggestionService, SchemaSuggestionService>();

// 注册文件转换服务
builder.Services.AddHttpClient<IFileConversionService, FileConversionService>();

// 注册表格数据服务
builder.Services.AddScoped<ITableDataService, TableDataService>();

// 注册数据结构化服务
builder.Services.AddScoped<IDataStructuringService, DataStructuringService>();

// *** 使用新的Inference模块依赖注入扩展方法 ***
builder.Services.AddInferenceServices();

// 注册Prompt执行服务（来自AI模块）
builder.Services.AddScoped<ContentEngine.Core.Inference.Services.IPromptExecutionService, ContentEngine.Core.AI.Services.PromptExecutionService>();

// 注册 SignalR 推理进度通知后台服务
builder.Services.AddHostedService<SignalRProgressNotificationService>();

// 2. 配置请求本地化选项 (可选，但推荐)
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // 定义支持的语言文化
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("zh")
    };
    options.DefaultRequestCulture = new RequestCulture("zh");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 3. 使用本地化中间件 (重要：放在请求处理管道的早期，但在路由之后通常是好的)
app.UseRequestLocalization();

// 仅当配置要求时启用 HTTPS 重定向（生产通常由反向代理处理 TLS）
var enforceHttps = builder.Configuration.GetValue<bool?>("EnforceHttps") ?? app.Environment.IsDevelopment();
if (enforceHttps)
{
    app.UseHttpsRedirection();
}

// 反向代理转发头（X-Forwarded-For/Proto/Host）
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
// 可选：从配置读取已知代理
var knownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
foreach (var proxy in knownProxies)
{
    if (System.Net.IPAddress.TryParse(proxy, out var ip))
    {
        forwardedHeadersOptions.KnownProxies.Add(ip);
    }
}
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// 映射 SignalR Hub
app.MapHub<ReasoningProgressHub>("/reasoning-progress-hub");

app.Run();
