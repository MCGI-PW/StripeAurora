using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Aurora.Tests.Integration.Fixtures;
using AuroraPet.Auth.Application.Dtos;
using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Integration.Endpoints;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests
{
    private const string SenhaValida = "Senha@2026";

    private readonly AuroraWebApplicationFactory _factory;
    private readonly HttpClient _anonimo;

    public AuthEndpointsTests(AuroraWebApplicationFactory factory)
    {
        _factory = factory;
        _anonimo = factory.CreateClient();
    }

    private static string EmailUnico() => $"usuario-{Guid.NewGuid():n}@clinica.com.br";

    [Fact]
    public async Task PostRegister_ComDadosValidos_Retorna201ComTokenEPerfilClinic()
    {
        // Arrange
        var request = new RegisterRequest("Recepcao Vida Animal", EmailUnico(), SenhaValida);

        // Act
        var response = await _anonimo.PostAsJsonAsync("/api/auth/register", request);
        var corpo = await response.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(corpo!.AccessToken));
        Assert.Equal("Bearer", corpo.TokenType);
        Assert.Equal(UserRoles.Clinic, corpo.User.Role);
    }

    [Fact]
    public async Task PostRegister_ComEmailJaCadastrado_Retorna409()
    {
        // Arrange
        var email = EmailUnico();
        await _anonimo.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Primeiro", email, SenhaValida));

        // Act
        var response = await _anonimo.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Segundo", email, SenhaValida));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostRegister_ComSenhaFraca_Retorna400()
    {
        // Arrange
        var request = new RegisterRequest("Senha Fraca", EmailUnico(), "123");

        // Act
        var response = await _anonimo.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostLogin_ComCredenciaisCorretas_Retorna200ComTokenQueAbreRotaProtegida()
    {
        // Arrange
        var email = EmailUnico();
        await _anonimo.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Login Ok", email, SenhaValida));

        // Act
        var login = await _anonimo.PostAsJsonAsync("/api/auth/login", new LoginRequest(email.ToUpperInvariant(), SenhaValida));
        var token = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await _anonimo.SendAsync(request);
        var perfil = await me.Content.ReadFromJsonAsync<UserResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(email, perfil!.Email);
    }

    [Fact]
    public async Task PostLogin_ComSenhaErrada_Retorna401()
    {
        // Arrange
        var email = EmailUnico();
        await _anonimo.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Senha Errada", email, SenhaValida));

        // Act
        var response = await _anonimo.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "OutraSenha123"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetClinics_SemToken_Retorna401()
    {
        // Arrange & Act
        var response = await _anonimo.GetAsync("/api/clinics");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetClinics_ComTokenAdulterado_Retorna401()
    {
        // Arrange
        var token = _factory.GenerateToken() + "x";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/clinics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _anonimo.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPlan_ComPerfilClinic_Retorna403()
    {
        // Arrange
        var cliente = _factory.CreateAuthenticatedClient(UserRoles.Clinic);
        var request = new SubscriptionPlanRequest(PlanTier.Basic, "Plano Indevido", 10m, $"price_{Guid.NewGuid():n}");

        // Act
        var response = await cliente.PostAsJsonAsync("/api/plans", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPlans_ComPerfilClinic_Retorna200()
    {
        // Arrange
        var cliente = _factory.CreateAuthenticatedClient(UserRoles.Clinic);

        // Act
        var response = await cliente.GetAsync("/api/plans");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
