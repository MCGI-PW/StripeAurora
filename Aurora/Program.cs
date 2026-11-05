using AuroraPet.Infrastructure.Mongo;
using AuroraPet.Infrastructure.Observability;
using AuroraPet.Infrastructure.Security;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Application.Options;
using AuroraPet.Payments.Application.Services;
using AuroraPet.Payments.Infrastructure.Payments;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Observabilidade ----------
builder.AddAuroraSerilog();
builder.AddAuroraOpenTelemetry();

// ---------- Persistencia: MongoDB (padrao) com Oracle/EF Core opcional ----------
// Database:Provider = "MongoDB" -> clinicas, planos, assinaturas e usuarios no MongoDB.
// Database:Provider = "Oracle"  -> clinicas, planos e assinaturas no Oracle; usuarios no MongoDB.
var oracleRegistrado = builder.AddAuroraPersistence();

// ---------- Autenticacao e autorizacao (JWT Bearer) ----------
builder.AddAuroraJwtAuthentication();

// ---------- Pagamentos ----------
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddScoped<ISubscriptionService, StripeSubscriptionService>();

// ---------- Casos de uso ----------
builder.Services.AddScoped<IClinicService, ClinicService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<IClinicSubscriptionService, ClinicSubscriptionService>();

// ---------- Health checks ----------
builder.AddAuroraHealthChecks(oracleRegistrado);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// A correlacao vem primeiro: assim o log de conclusao da requisicao, emitido pelo
// UseSerilogRequestLogging, ja enxerga o CorrelationId no LogContext.
app.UseMiddleware<CorrelationIdMiddleware>();

// Log de uma linha por requisicao, com rota, status e tempo de resposta.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Autenticacao antes da autorizacao: primeiro identifica o usuario pelo token, depois aplica [Authorize].
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---------- Endpoints de monitoramento (publicos, sem token) ----------
// /health          -> visao completa (processo + MongoDB + Oracle + Stripe)
// /health/live     -> o processo esta de pe? (liveness probe)
// /health/ready    -> as dependencias respondem? (readiness probe)
// /metrics         -> metricas de desempenho no formato Prometheus
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = ObservabilityExtensions.WriteHealthResponse
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = ObservabilityExtensions.WriteHealthResponse
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = ObservabilityExtensions.WriteHealthResponse
});
app.MapPrometheusScrapingEndpoint("/metrics");

try
{
    Log.Information("Iniciando {Service} no ambiente {Environment}",
        AuroraTelemetry.ServiceName, app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicacao terminou de forma inesperada");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposto para que os testes de integracao possam usar WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program { }
