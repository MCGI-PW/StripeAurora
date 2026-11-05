using Aurora.Tests.Unit.Fixtures;
using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Application.Services;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aurora.Tests.Unit.Application;

[Collection(PlanCollection.Name)]
public class ClinicServiceTests
{
    private readonly PlanFixture _fixture;
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly IClinicSubscriptionRepository _subscriptions = Substitute.For<IClinicSubscriptionRepository>();
    private readonly ClinicService _service;

    public ClinicServiceTests(PlanFixture fixture)
    {
        _fixture = fixture;
        _service = new ClinicService(_clinics, _subscriptions, Substitute.For<ILogger<ClinicService>>());
    }

    [Fact]
    public async Task GetAllAsync_ComDuasClinicasCadastradas_RetornaAsDuas()
    {
        // Arrange
        _clinics.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Clinic> { _fixture.ClinicaVidaAnimal(), _fixture.ClinicaPatasFelizes() });

        // Act
        var resultado = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, c => c.Name == "Clinica Vida Animal");
    }

    [Fact]
    public async Task GetByIdAsync_ComClinicaInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        _clinics.GetByIdAsync(idInexistente, Arg.Any<CancellationToken>()).Returns((Clinic?)null);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.GetByIdAsync(idInexistente));

        // Assert
        Assert.IsType<ResourceNotFoundException>(excecao);
    }

    [Fact]
    public async Task CreateAsync_ComCnpjInedito_PersisteAClinicaESalva()
    {
        // Arrange
        _clinics.GetByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Clinic?)null);
        var request = new ClinicRequest("Clinica Nova", "11222333000181", "contato@nova.com.br", "uid-nova");

        // Act
        var resultado = await _service.CreateAsync(request);

        // Assert
        Assert.Equal("Clinica Nova", resultado.Name);
        Assert.True(resultado.Active);
        await _clinics.Received(1).AddAsync(Arg.Any<Clinic>(), Arg.Any<CancellationToken>());
        await _clinics.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ComCnpjJaCadastrado_LancaBusinessRuleExceptionENaoPersiste()
    {
        // Arrange
        _clinics.GetByCnpjAsync("12345678000190", Arg.Any<CancellationToken>()).Returns(_fixture.ClinicaVidaAnimal());
        var request = new ClinicRequest("Outra Clinica", "12345678000190", "outra@clinica.com.br", "uid-outra");

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.CreateAsync(request));

        // Assert
        Assert.IsType<BusinessRuleException>(excecao);
        await _clinics.DidNotReceive().AddAsync(Arg.Any<Clinic>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DesativandoAClinica_MarcaComoInativaESalva()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        _clinics.GetByIdAsync(clinica.Id, Arg.Any<CancellationToken>()).Returns(clinica);
        var request = new ClinicUpdateRequest("Clinica Vida Animal", "12345678000190", "contato@vidaanimal.com.br", false);

        // Act
        var resultado = await _service.UpdateAsync(clinica.Id, request);

        // Assert
        Assert.False(resultado.Active);
        await _clinics.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ComClinicaSemAssinaturas_RemoveAClinica()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        _clinics.GetByIdAsync(clinica.Id, Arg.Any<CancellationToken>()).Returns(clinica);
        _subscriptions.ListByClinicAsync(clinica.Id, Arg.Any<CancellationToken>()).Returns(new List<ClinicSubscription>());

        // Act
        await _service.DeleteAsync(clinica.Id);

        // Assert
        await _clinics.Received(1).RemoveAsync(clinica, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ComClinicaQuePossuiAssinaturas_LancaBusinessRuleExceptionENaoRemove()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        _clinics.GetByIdAsync(clinica.Id, Arg.Any<CancellationToken>()).Returns(clinica);
        _subscriptions.ListByClinicAsync(clinica.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ClinicSubscription> { _fixture.AssinaturaDe(clinica, _fixture.PlanoBasico()) });

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.DeleteAsync(clinica.Id));

        // Assert
        Assert.IsType<BusinessRuleException>(excecao);
        await _clinics.DidNotReceive().RemoveAsync(Arg.Any<Clinic>(), Arg.Any<CancellationToken>());
    }
}
