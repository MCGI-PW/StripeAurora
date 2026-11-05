using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AuroraPet.Infrastructure.Observability;

/// <summary>
/// Fontes de telemetria proprias da aplicacao. As instrumentacoes automaticas do OpenTelemetry
/// cobrem HTTP/EF Core; estes instrumentos cobrem o dominio de assinaturas.
/// </summary>
public static class AuroraTelemetry
{
    public const string ServiceName = "AuroraPet.Api";

    /// <summary>Spans manuais: usados para rastrear casos de uso entre as camadas.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    /// <summary>Meter proprio da aplicacao, exposto junto das metricas do runtime em /metrics.</summary>
    public static readonly Meter Meter = new(ServiceName);

    /// <summary>Total de assinaturas criadas, particionado por plano.</summary>
    public static readonly Counter<long> SubscriptionsCreated =
        Meter.CreateCounter<long>("aurora.subscriptions.created", unit: "{assinatura}",
            description: "Quantidade de assinaturas de clinicas criadas.");

    /// <summary>Total de falhas de negocio, particionado por tipo de erro.</summary>
    public static readonly Counter<long> BusinessFailures =
        Meter.CreateCounter<long>("aurora.business.failures", unit: "{falha}",
            description: "Falhas de regra de negocio devolvidas pela API (4xx de dominio).");
}
