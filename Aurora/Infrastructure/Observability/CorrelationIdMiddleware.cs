using Serilog.Context;

namespace AuroraPet.Infrastructure.Observability;

/// <summary>
/// Garante um identificador de correlacao por requisicao: le o header X-Correlation-Id
/// quando enviado, gera um novo quando ausente, devolve no response e injeta no
/// LogContext do Serilog para que todo log da requisicao carregue o mesmo valor.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : Guid.NewGuid().ToString("n");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString()))
        {
            await _next(context);
        }
    }
}
