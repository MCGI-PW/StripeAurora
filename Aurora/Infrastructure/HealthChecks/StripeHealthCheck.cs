using AuroraPet.Payments.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AuroraPet.Infrastructure.HealthChecks;

/// <summary>
/// Verifica a disponibilidade do servico externo de pagamentos (Stripe).
/// Sem chave configurada o resultado e Degraded (a API continua servindo o CRUD);
/// com chave configurada, faz uma chamada real de baixo custo a API da Stripe.
/// </summary>
public class StripeHealthCheck : IHealthCheck
{
    private readonly StripeOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public StripeHealthCheck(IOptions<StripeOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return HealthCheckResult.Degraded(
                "Stripe nao configurado (Stripe:SecretKey vazio). O CRUD continua disponivel, o checkout nao.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("stripe");
            client.Timeout = TimeSpan.FromSeconds(5);

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.stripe.com/v1/balance");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);

            using var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"Stripe respondeu {(int)response.StatusCode}.");

            // 401/403 e chave invalida/revogada: problema de configuracao desta instalacao,
            // nao indisponibilidade da Stripe. Degrada o checkout sem derrubar a API inteira.
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                return HealthCheckResult.Degraded($"Credencial da Stripe recusada ({(int)response.StatusCode}). Revise Stripe:SecretKey.");

            return HealthCheckResult.Unhealthy($"Stripe respondeu {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao contatar a API da Stripe.", ex);
        }
    }
}
