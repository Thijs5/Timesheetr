using JasperFx.CodeGeneration.Model;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Timesheetr.Api;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Services;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Http;
using Wolverine.RabbitMQ;
using Wolverine.SignalR;
using Wolverine.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information)
    .AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
    .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);

var dataDir = builder.Configuration["DataPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data", "sqlite");
Directory.CreateDirectory(dataDir);
var sqliteConnectionString = $"Data Source={Path.Combine(dataDir, "settings.db")}";

builder.Services.AddDbContextFactory<TimesheetrDbContext>(options =>
    options.UseSqlite(sqliteConnectionString, o => o.MigrationsAssembly("Timesheetr.Api")));
builder.Services.AddDbContext<TimesheetrDbContext>(
    options => options.UseSqlite(sqliteConnectionString, o => o.MigrationsAssembly("Timesheetr.Api")),
    optionsLifetime: ServiceLifetime.Singleton);

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.Configure<Timesheetr.Api.Infrastructure.Messaging.Handlers.MessagingOptions>(
    builder.Configuration.GetSection("Messaging"));

builder.Host.UseWolverine(opts =>
{
    // Matches the OpenTelemetry resource service name below, so Wolverine's
    // "Wolverine:{ServiceName}" meter name is predictable for AddMeter(...).
    opts.ServiceName = "backend";

    // TogglService/TempoService/JiraService are registered via AddHttpClient<T>(),
    // a transient factory registration that requires service location in generated
    // handler code — safe here since they're stateless HTTP clients, not scoped state.
    opts.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;

    opts.PersistMessagesWithSqlite(sqliteConnectionString);
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();

    var rabbitConnectionString = builder.Configuration.GetConnectionString("messaging");
    if (rabbitConnectionString is not null)
        opts.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();

    opts.UseSignalR();
    opts.Publish(x =>
    {
        x.MessagesImplementing<WebSocketMessage>();
        x.ToSignalR();
    });
});

builder.Services.AddWolverineHttp();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("backend"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // Wolverine's own ActivitySource — traces command/event publishing,
        // scheduling, and handler execution so the bus is visible in Aspire.
        .AddSource("Wolverine")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics.AddMeter("Wolverine:backend"))
    .WithLogging(logging => logging.AddOtlpExporter());

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddHttpClient<TogglService>();
builder.Services.AddHttpClient<TempoService>();
builder.Services.AddHttpClient<JiraService>();

builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo<IEndpoint>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());

var app = builder.Build();

app.UseCors();

foreach (var endpoint in app.Services.GetServices<IEndpoint>())
    endpoint.Map(app);

app.MapWolverineEndpoints();
app.MapWolverineSignalRHub("/hubs/sync-status");

app.Run();
