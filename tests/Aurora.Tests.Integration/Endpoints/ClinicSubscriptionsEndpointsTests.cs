using System.Net;
using System.Net.Http.Json;
using Aurora.Tests.Integration.Fixtures;
using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Integration.Endpoints;

[Collection(ApiCollection.Name)]
public class ClinicSubscriptionsEndpointsTests
{
    private readonly HttpClient _client;

    public ClinicSubscriptionsEndpointsTests(AuroraWebApplicationFactory factory) => _client = factory.CreateAuthenticatedClient();

    private async Task<ClinicResponse> CriarClinicaAsync()
    {
        var cnpj = Random.Shared.NextInt64(10000000000000, 99999999999999).ToString();
        var response = await _client.PostAsJsonAsync("/api/clinics",
            new ClinicRequest($"Clinica {cnpj[..6]}", cnpj, $"contato{cnpj[..6]}@clinica.com.br", $"uid-{cnpj}"));

        return (await response.Content.ReadFromJsonAsync<ClinicResponse>())!;
    }

    private async Task<SubscriptionPlanResponse> CriarPlanoAsync()
    {
        var sufixo = Guid.NewGuid().ToString("n")[..8];
        var response = await _client.PostAsJsonAsync("/api/plans",
            new SubscriptionPlanRequest(PlanTier.Basic, $"Plano {sufixo}", 29.90m, $"price_{sufixo}"));

        return (await response.Content.ReadFromJsonAsync<SubscriptionPlanResponse>())!;
    }

    [Fact]
    public async Task PostSubscription_ComClinicaEPlanoExistentes_Retorna201LigandoOsDois()
    {
        // Arrange
        var clinica = await CriarClinicaAsync();
        var plano = await CriarPlanoAsync();
        var request = new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_1", $"sub_{Guid.NewGuid():n}");

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);
        var criada = await response.Content.ReadFromJsonAsync<ClinicSubscriptionResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(clinica.Id, criada!.ClinicId);
        Assert.Equal(plano.Id, criada.PlanId);
        Assert.Equal(SubscriptionStatus.Incomplete, criada.Status);
    }

    [Fact]
    public async Task PostSubscription_ComClinicaInexistente_Retorna404()
    {
        // Arrange
        var plano = await CriarPlanoAsync();
        var request = new ClinicSubscriptionRequest(Guid.NewGuid(), plano.Id, "cus_2", $"sub_{Guid.NewGuid():n}");

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSubscription_ComPlanoInexistente_Retorna404()
    {
        // Arrange
        var clinica = await CriarClinicaAsync();
        var request = new ClinicSubscriptionRequest(clinica.Id, Guid.NewGuid(), "cus_3", $"sub_{Guid.NewGuid():n}");

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSubscription_ComClinicaDesativada_Retorna409()
    {
        // Arrange
        var clinica = await CriarClinicaAsync();
        var plano = await CriarPlanoAsync();
        await _client.PutAsJsonAsync($"/api/clinics/{clinica.Id}",
            new ClinicUpdateRequest(clinica.Name, clinica.Cnpj, clinica.Email, false));

        var request = new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_4", $"sub_{Guid.NewGuid():n}");

        // Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PutSubscription_AtivandoAAssinatura_Retorna200ComStatusActive()
    {
        // Arrange
        var clinica = await CriarClinicaAsync();
        var plano = await CriarPlanoAsync();
        var criacao = await _client.PostAsJsonAsync("/api/subscriptions",
            new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_5", $"sub_{Guid.NewGuid():n}"));
        var assinatura = (await criacao.Content.ReadFromJsonAsync<ClinicSubscriptionResponse>())!;

        var fimDoPeriodo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/subscriptions/{assinatura.Id}",
            new ClinicSubscriptionUpdateRequest(SubscriptionStatus.Active, fimDoPeriodo, plano.Id));
        var atualizada = await response.Content.ReadFromJsonAsync<ClinicSubscriptionResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SubscriptionStatus.Active, atualizada!.Status);
        Assert.Equal(fimDoPeriodo, atualizada.CurrentPeriodEnd);
    }

    [Fact]
    public async Task DeleteClinic_ComAssinaturaVinculada_Retorna409EPreservaAClinica()
    {
        // Arrange
        var clinica = await CriarClinicaAsync();
        var plano = await CriarPlanoAsync();
        await _client.PostAsJsonAsync("/api/subscriptions",
            new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_6", $"sub_{Guid.NewGuid():n}"));

        // Act
        var response = await _client.DeleteAsync($"/api/clinics/{clinica.Id}");
        var clinicaAindaExiste = await _client.GetAsync($"/api/clinics/{clinica.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, clinicaAindaExiste.StatusCode);
    }

    [Fact]
    public async Task DeletePlan_ComAssinaturaVinculada_Retorna409EPreservaOPlano()
    {
        // Arrange
        var clinica = await CriarClinicaAsync();
        var plano = await CriarPlanoAsync();
        await _client.PostAsJsonAsync("/api/subscriptions",
            new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_7", $"sub_{Guid.NewGuid():n}"));

        // Act
        var response = await _client.DeleteAsync($"/api/plans/{plano.Id}");
        var planoAindaExiste = await _client.GetAsync($"/api/plans/{plano.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, planoAindaExiste.StatusCode);
    }

    [Fact]
    public async Task DeleteSubscription_ComAssinaturaExistente_Retorna204EDepois404()
    {
        // Arrange
        var clinica = await CriarClinicaAsync();
        var plano = await CriarPlanoAsync();
        var criacao = await _client.PostAsJsonAsync("/api/subscriptions",
            new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_8", $"sub_{Guid.NewGuid():n}"));
        var assinatura = (await criacao.Content.ReadFromJsonAsync<ClinicSubscriptionResponse>())!;

        // Act
        var exclusao = await _client.DeleteAsync($"/api/subscriptions/{assinatura.Id}");
        var consultaPosterior = await _client.GetAsync($"/api/subscriptions/{assinatura.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consultaPosterior.StatusCode);
    }

    [Fact]
    public async Task DeleteSubscription_ComIdInexistente_Retorna404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/subscriptions/{idInexistente}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
