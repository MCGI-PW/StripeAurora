using System.Net;
using System.Net.Http.Json;
using Aurora.Tests.Integration.Fixtures;
using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Integration.Endpoints;

[Collection(ApiCollection.Name)]
public class SubscriptionPlansEndpointsTests
{
    private readonly HttpClient _client;

    public SubscriptionPlansEndpointsTests(AuroraWebApplicationFactory factory) => _client = factory.CreateAuthenticatedClient();

    private static SubscriptionPlanRequest NovoPlano(string sufixo) =>
        new(PlanTier.Basic, $"Plano {sufixo}", 29.90m, $"price_{sufixo}");

    [Fact]
    public async Task PostPlan_ComPayloadValido_Retorna201ComLocationHeader()
    {
        // Arrange
        var request = NovoPlano(Guid.NewGuid().ToString("n")[..8]);

        // Act
        var response = await _client.PostAsJsonAsync("/api/plans", request);
        var criado = await response.Content.ReadFromJsonAsync<SubscriptionPlanResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(criado);
        Assert.Equal(request.Name, criado!.Name);
        Assert.True(criado.Active);
    }

    [Fact]
    public async Task PostPlan_ComNomeVazio_Retorna400()
    {
        // Arrange
        var request = new SubscriptionPlanRequest(PlanTier.Basic, "", 29.90m, "price_invalido");

        // Act
        var response = await _client.PostAsJsonAsync("/api/plans", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPlan_ComPrecoNegativo_Retorna400()
    {
        // Arrange
        var request = new SubscriptionPlanRequest(PlanTier.Premium, "Plano invalido", -10m, "price_negativo");

        // Act
        var response = await _client.PostAsJsonAsync("/api/plans", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPlanById_ComIdInexistente_Retorna404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/plans/{idInexistente}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutPlan_ComPlanoExistente_Retorna200ComOsDadosAtualizados()
    {
        // Arrange
        var criacao = await _client.PostAsJsonAsync("/api/plans", NovoPlano(Guid.NewGuid().ToString("n")[..8]));
        var plano = (await criacao.Content.ReadFromJsonAsync<SubscriptionPlanResponse>())!;
        var alteracao = new SubscriptionPlanRequest(PlanTier.Premium, "Plano renomeado", 99.90m, plano.StripePriceId);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/plans/{plano.Id}", alteracao);
        var atualizado = await response.Content.ReadFromJsonAsync<SubscriptionPlanResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Plano renomeado", atualizado!.Name);
        Assert.Equal(99.90m, atualizado.Price);
        Assert.Equal(PlanTier.Premium, atualizado.Tier);
    }

    [Fact]
    public async Task DeletePlan_ComPlanoSemAssinaturas_Retorna204ERemoveDoBanco()
    {
        // Arrange
        var criacao = await _client.PostAsJsonAsync("/api/plans", NovoPlano(Guid.NewGuid().ToString("n")[..8]));
        var plano = (await criacao.Content.ReadFromJsonAsync<SubscriptionPlanResponse>())!;

        // Act
        var response = await _client.DeleteAsync($"/api/plans/{plano.Id}");
        var consultaPosterior = await _client.GetAsync($"/api/plans/{plano.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consultaPosterior.StatusCode);
    }

    [Fact]
    public async Task GetPlans_AposCriarUmPlano_RetornaOPlanoNaListagem()
    {
        // Arrange
        var sufixo = Guid.NewGuid().ToString("n")[..8];
        await _client.PostAsJsonAsync("/api/plans", NovoPlano(sufixo));

        // Act
        var planos = await _client.GetFromJsonAsync<List<SubscriptionPlanResponse>>("/api/plans");

        // Assert
        Assert.NotNull(planos);
        Assert.Contains(planos!, p => p.Name == $"Plano {sufixo}");
    }
}
