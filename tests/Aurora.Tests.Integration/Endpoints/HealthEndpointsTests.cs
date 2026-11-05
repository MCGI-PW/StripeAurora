using System.Net;
using System.Text.Json;
using Aurora.Tests.Integration.Fixtures;

namespace Aurora.Tests.Integration.Endpoints;

[Collection(ApiCollection.Name)]
public class HealthEndpointsTests
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(AuroraWebApplicationFactory factory) => _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task GetHealthLive_ComAplicacaoNoAr_RetornaHealthy()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health/live");
        var corpo = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", corpo);
    }

    [Fact]
    public async Task GetHealth_ComTodasAsDependenciasVerificadas_ListaCadaCheckIndividualmente()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");
        using var documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Assert
        var checks = documento.RootElement.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("self", checks);
        Assert.Contains("oracle", checks);
        Assert.DoesNotContain("mongodb", checks); // removido no WebApplicationFactory: os testes nao dependem de MongoDB
        Assert.Contains("stripe", checks);
    }

    [Fact]
    public async Task GetMetrics_AposRequisicoesAtendidas_ExpoeMetricasNoFormatoPrometheus()
    {
        // Arrange
        await _client.GetAsync("/api/plans");

        // Act
        var response = await _client.GetAsync("/metrics");
        var corpo = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("http_server_request_duration_seconds", corpo);
    }

    [Fact]
    public async Task GetPlans_ComHeaderDeCorrelacao_DevolveOMesmoCorrelationIdNaResposta()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString("n");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/plans");
        request.Headers.Add("X-Correlation-Id", correlationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var valores));
        Assert.Equal(correlationId, valores!.Single());
    }
}
