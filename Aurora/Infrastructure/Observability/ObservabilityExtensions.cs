using System.Text.Json;
using AuroraPet.Infrastructure.Data;
using AuroraPet.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace AuroraPet.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Logging estruturado com Serilog: console (formato legivel em Development) e
    /// arquivo rolante em JSON em logs/aurora-*.log, com enriquecimento por requisicao.
    /// </summary>
    public static void AddAuroraSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", AuroraTelemetry.ServiceName)
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                formatter: new Serilog.Formatting.Compact.CompactJsonFormatter(),
                path: Path.Combine("logs", "aurora-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7));
    }

    /// <summary>
    /// Distributed tracing e metricas com OpenTelemetry. Traces cobrem ASP.NET Core,
    /// HttpClient (chamadas a Stripe) e EF Core (consultas no Oracle). As metricas de
    /// desempenho sao expostas no formato Prometheus em /metrics.
    /// </summary>
    public static void AddAuroraOpenTelemetry(this WebApplicationBuilder builder)
    {
        var resource = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: AuroraTelemetry.ServiceName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0")
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", builder.Environment.EnvironmentName)
            });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resource)
                    .AddSource(AuroraTelemetry.ServiceName)
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        // /health e /metrics sao ruido de scraping, nao trafego de negocio.
                        o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health")
                                       && !ctx.Request.Path.StartsWithSegments("/metrics");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (builder.Environment.IsDevelopment())
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resource)
                    .AddMeter(AuroraTelemetry.ServiceName)
                    .AddAspNetCoreInstrumentation()   // http.server.request.duration + status code
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
            });
    }

    /// <summary>
    /// Health checks: processo, MongoDB, banco Oracle (quando configurado) e servico externo de pagamentos.
    /// </summary>
    public static void AddAuroraHealthChecks(this WebApplicationBuilder builder, bool includeOracle)
    {
        builder.Services.AddHttpClient();

        var checks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API no ar."), tags: ["live"])
            .AddCheck<MongoHealthCheck>("mongodb", tags: ["ready", "db"])
            .AddCheck<StripeHealthCheck>("stripe", tags: ["ready", "external"]);

        if (includeOracle)
            checks.AddDbContextCheck<AuroraDbContext>("oracle", tags: ["ready", "db"]);
    }

    /// <summary>Escreve o resultado dos health checks como JSON detalhado por dependencia.</summary>
    public static Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                error = e.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
