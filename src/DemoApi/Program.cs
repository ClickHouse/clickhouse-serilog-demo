using DemoApi.Endpoints;
using DemoApi.Health;
using DemoApi.Middleware;
using DemoApi.Services;
using Serilog;
using Serilog.Sinks.ClickHouse.ColumnWriters;
using Serilog.Sinks.ClickHouse.Configuration;

Serilog.Debugging.SelfLog.Enable(Console.Error);

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Debug()
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "DemoApi")
        .Enrich.WithMachineName()
        .WriteTo.Console()
        .WriteTo.ClickHouse(
            connectionString: context.Configuration["ClickHouse:ConnectionString"]!,
            configureSchema: schema => schema
                .WithDatabase("logs")
                .WithTableName("app_logs")
                .AddTimestampColumn()
                .AddLevelColumn()
                .AddMessageColumn()
                .AddMessageTemplateColumn()
                .AddExceptionColumn()
                .AddPropertiesColumn("properties", "JSON(ServiceName String, RequestPath String, Elapsed Float64, UserId String, OrderId String, OrderStatus String)")
                .AddPropertyColumn("CorrelationId", "Nullable(String)")
                .AddPropertyColumn("RequestPath", "Nullable(String)")
                .AddPropertyColumn("StatusCode", "Nullable(Int32)", writeMethod: PropertyWriteMethod.Raw)
                .AddPropertyColumn("ServiceName", "LowCardinality(String)")
                .AddIndex("INDEX idx_message message TYPE text(tokenizer = splitByNonAlpha, preprocessor = lowerUTF8(message)) GRANULARITY 8")
                .WithEngine(@"ENGINE = MergeTree
                              ORDER BY (level, timestamp)
                              TTL timestamp + toIntervalDay(30)"),
            flushInterval: TimeSpan.FromSeconds(2));
});

// ── Health checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<ClickHouseHealthCheck>("clickhouse");

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ProductService>();
builder.Services.AddTransient<OrderService>();

var app = builder.Build();

// ── Middleware ──────────────────────────────────────────────────────────────
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

// ── Endpoints ──────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapEndpoints();

try
{
    app.Run();
}
finally
{
    await Log.CloseAndFlushAsync();
}
