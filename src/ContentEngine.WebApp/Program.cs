using ContentEngine.WebApp.Components;
using ContentEngine.WebApp.Core.Storage;
using ContentEngine.WebApp.Core.DataPipeline.Services;
using ConfigurableAIProvider.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 注册 LiteDB 上下文为单例服务
builder.Services.AddSingleton<LiteDbContext>();

// 注册 Schema 管理服务 (可以使用 Scoped 或 Transient)
builder.Services.AddScoped<ISchemaDefinitionService, SchemaDefinitionService>();

// 注册数据实例管理服务 (可以使用 Scoped 或 Transient)
builder.Services.AddScoped<IDataEntryService, DataEntryService>();

// *** 测试：注册 ConfigurableAIProvider 相关服务 ***
builder.Services.AddConfigurableAIProvider(builder.Configuration);
// ******************************************

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
