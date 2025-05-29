using ContentEngine.WebApp.Components;
using ContentEngine.Core.Storage;
using ContentEngine.Core.DataPipeline.Services;
using ContentEngine.Core.AI.Services;
using ConfigurableAIProvider.Extensions;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// 1. 添加本地化服务
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 注册 LiteDB 上下文为单例服务
builder.Services.AddSingleton<LiteDbContext>();

// 注册 Schema 管理服务 (可以使用 Scoped 或 Transient)
builder.Services.AddScoped<ISchemaDefinitionService, SchemaDefinitionService>();

// 注册数据实例管理服务 (可以使用 Scoped 或 Transient)
builder.Services.AddScoped<IDataEntryService, DataEntryService>();

// *** 测试：注册 ConfigurableAIProvider 相关服务 ***
builder.Services.AddConfigurableAIProvider(builder.Configuration);
// ******************************************

// 注册 Schema 建议服务
builder.Services.AddScoped<ISchemaSuggestionService, SchemaSuggestionService>();

// 注册文件转换服务
builder.Services.AddHttpClient<IFileConversionService, FileConversionService>();

// 注册数据结构化服务
builder.Services.AddScoped<IDataStructuringService, DataStructuringService>();

// 注册Prompt执行服务
builder.Services.AddScoped<ContentEngine.Core.Inference.Services.IPromptExecutionService, ContentEngine.Core.AI.Services.PromptExecutionService>();

// 注册推理引擎服务
builder.Services.AddScoped<ContentEngine.Core.Inference.Services.IReasoningService, ContentEngine.Core.Inference.Services.ReasoningService>();

// 注册Query处理服务
builder.Services.AddScoped<ContentEngine.Core.AI.Services.IQueryProcessingService, ContentEngine.Core.AI.Services.QueryProcessingService>();

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

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
