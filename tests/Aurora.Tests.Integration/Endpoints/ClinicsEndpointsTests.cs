using System.Net;
using System.Net.Http.Json;
using Aurora.Tests.Integration.Fixtures;
using AuroraPet.Payments.Application.Dtos;

namespace Aurora.Tests.Integration.Endpoints;

[Collection(ApiCollection.Name)]
public class ClinicsEndpointsTests
{
    private readonly HttpClient _client;

    public ClinicsEndpointsTests(AuroraWebApplicationFactory factory) => _client = factory.CreateAuthenticatedClient();

    /// <summary>Gera um CNPJ de 14 digitos unico por teste, evitando colisao no indice unico.</summary>
    private static string CnpjUnico() => Random.Shared.NextInt64(10000000000000, 99999999999999).ToString();

    private static ClinicRequest NovaClinica(string cnpj) =>
        new($"Clinica {cnpj[..6]}", cnpj, $"contato{cnpj[..6]}@clinica.com.br", $"uid-{cnpj}");

    [Fact]
    public async Task PostClinic_ComPayloadValido_Retorna201ComLocationHeader()
    {
        // Arrange
        var request = NovaClinica(CnpjUnico());

        // Act
        var response = await _client.PostAsJsonAsync("/api/clinics", request);
        var criada = await response.Content.ReadFromJsonAsync<ClinicResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal(request.Name, criada!.Name);
        Assert.True(criada.Active);
    }

    [Fact]
    public async Task PostClinic_ComCnpjInvalido_Retorna400()
    {
        // Arrange
        var request = new ClinicRequest("Clinica X", "123", "contato@x.com.br", "uid-x-invalido");

        // Act
        var response = await _client.PostAsJsonAsync("/api/clinics", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostClinic_ComCnpjJaCadastrado_Retorna409()
    {
        // Arrange
        var cnpj = CnpjUnico();
        await _client.PostAsJsonAsync("/api/clinics", NovaClinica(cnpj));

        // Act
        var response = await _client.PostAsJsonAsync("/api/clinics",
            new ClinicRequest("Outra Clinica", cnpj, "outra@clinica.com.br", $"uid-outra-{cnpj}"));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetClinicById_ComIdInexistente_Retorna404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/clinics/{idInexistente}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutClinic_ComClinicaExistente_Retorna200ComOsDadosAtualizados()
    {
        // Arrange
        var criacao = await _client.PostAsJsonAsync("/api/clinics", NovaClinica(CnpjUnico()));
        var clinica = (await criacao.Content.ReadFromJsonAsync<ClinicResponse>())!;
        var alteracao = new ClinicUpdateRequest("Clinica Renomeada", clinica.Cnpj, "novo@clinica.com.br", true);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/clinics/{clinica.Id}", alteracao);
        var atualizada = await response.Content.ReadFromJsonAsync<ClinicResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Clinica Renomeada", atualizada!.Name);
        Assert.Equal("novo@clinica.com.br", atualizada.Email);
    }

    [Fact]
    public async Task DeleteClinic_SemAssinaturas_Retorna204EDepois404()
    {
        // Arrange
        var criacao = await _client.PostAsJsonAsync("/api/clinics", NovaClinica(CnpjUnico()));
        var clinica = (await criacao.Content.ReadFromJsonAsync<ClinicResponse>())!;

        // Act
        var exclusao = await _client.DeleteAsync($"/api/clinics/{clinica.Id}");
        var consultaPosterior = await _client.GetAsync($"/api/clinics/{clinica.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consultaPosterior.StatusCode);
    }

    [Fact]
    public async Task GetClinics_AposCadastrarUma_RetornaAClinicaNaListagem()
    {
        // Arrange
        var cnpj = CnpjUnico();
        await _client.PostAsJsonAsync("/api/clinics", NovaClinica(cnpj));

        // Act
        var clinicas = await _client.GetFromJsonAsync<List<ClinicResponse>>("/api/clinics");

        // Assert
        Assert.NotNull(clinicas);
        Assert.Contains(clinicas!, c => c.Cnpj == cnpj);
    }
}
