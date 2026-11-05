using AuroraPet.Infrastructure.Mongo;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AuroraPet.Infrastructure.HealthChecks;

/// <summary>Verifica a conectividade com o MongoDB executando o comando "ping".</summary>
public class MongoHealthCheck : IHealthCheck
{
    private readonly MongoContext _context;

    public MongoHealthCheck(MongoContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            await _context.Database.RunCommandAsync((Command<BsonDocument>)"{ ping: 1 }", cancellationToken: timeout.Token);

            return HealthCheckResult.Healthy(
                $"MongoDB respondeu ao ping (banco '{_context.Database.DatabaseNamespace.DatabaseName}').");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao contatar o MongoDB.", ex);
        }
    }
}
