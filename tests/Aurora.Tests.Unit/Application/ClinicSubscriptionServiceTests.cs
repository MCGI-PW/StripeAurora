using Aurora.Tests.Unit.Fixtures;
using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Application.Services;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aurora.Tests.Unit.Application;

[Collection(PlanCollection.Name)]
public class ClinicSubscriptionServiceTests
{
    private readonly PlanFixture _fixture;
    private readonly IClinicSubscriptionRepository _subscriptions = Substitute.For<IClinicSubscriptionRepository>();
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly ClinicSubscriptionService _service;

    public ClinicSubscriptionServiceTests(PlanFixture fixture)
    {
        _fixture = fixture;
        _service = new ClinicSubscriptionService(_subscriptions, _clinics, _plans,
            Substitute.For<ILogger<ClinicSubscriptionService>>());
    }

    [Fact]
    public async Task CreateAsync_ComClinicaEPlanoExistentes_CriaAssinaturaIncompleta()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        var plano = _fixture.PlanoBasico();
        _clinics.GetByIdAsync(clinica.Id, Arg.Any<CancellationToken>()).Returns(clinica);
        _plans.GetByIdAsync(plano.Id, Arg.Any<CancellationToken>()).Returns(plano);
        var request = new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_1", "sub_1");

        // Act
        var resultado = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(clinica.Id, resultado.ClinicId);
        Assert.Equal(plano.Id, resultado.PlanId);
        Assert.Equal(SubscriptionStatus.Incomplete, resultado.Status);
        await _subscriptions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ComClinicaInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        var clinicaInexistente = Guid.NewGuid();
        _clinics.GetByIdAsync(clinicaInexistente, Arg.Any<CancellationToken>()).Returns((Clinic?)null);
        var request = new ClinicSubscriptionRequest(clinicaInexistente, Guid.NewGuid(), "cus_1", "sub_1");

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.CreateAsync(request));

        // Assert
        Assert.IsType<ResourceNotFoundException>(excecao);
        await _subscriptions.DidNotReceive().AddAsync(Arg.Any<ClinicSubscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ComPlanoInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        var planoInexistente = Guid.NewGuid();
        _clinics.GetByIdAsync(clinica.Id, Arg.Any<CancellationToken>()).Returns(clinica);
        _plans.GetByIdAsync(planoInexistente, Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);
        var request = new ClinicSubscriptionRequest(clinica.Id, planoInexistente, "cus_1", "sub_1");

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.CreateAsync(request));

        // Assert
        Assert.IsType<ResourceNotFoundException>(excecao);
    }

    [Fact]
    public async Task CreateAsync_ComClinicaDesativada_LancaBusinessRuleException()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        clinica.Deactivate();
        var plano = _fixture.PlanoBasico();
        _clinics.GetByIdAsync(clinica.Id, Arg.Any<CancellationToken>()).Returns(clinica);
        _plans.GetByIdAsync(plano.Id, Arg.Any<CancellationToken>()).Returns(plano);
        var request = new ClinicSubscriptionRequest(clinica.Id, plano.Id, "cus_1", "sub_1");

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.CreateAsync(request));

        // Assert
        Assert.IsType<BusinessRuleException>(excecao);
        await _subscriptions.DidNotReceive().AddAsync(Arg.Any<ClinicSubscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_TrocandoDePlano_ValidaONovoPlanoEMantemAClinica()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        var premium = _fixture.PlanoPremium();
        var assinatura = _fixture.AssinaturaDe(clinica, _fixture.PlanoBasico());

        _subscriptions.GetByIdAsync(assinatura.Id, Arg.Any<CancellationToken>()).Returns(assinatura);
        _plans.GetByIdAsync(premium.Id, Arg.Any<CancellationToken>()).Returns(premium);

        var fimDoPeriodo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new ClinicSubscriptionUpdateRequest(SubscriptionStatus.Active, fimDoPeriodo, premium.Id);

        // Act
        var resultado = await _service.UpdateAsync(assinatura.Id, request);

        // Assert
        Assert.Equal(premium.Id, resultado.PlanId);
        Assert.Equal(clinica.Id, resultado.ClinicId);
        Assert.Equal(SubscriptionStatus.Active, resultado.Status);
    }

    [Fact]
    public async Task UpdateAsync_EmAssinaturaCancelada_LancaBusinessRuleException()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();
        var assinatura = _fixture.AssinaturaDe(_fixture.ClinicaVidaAnimal(), plano);
        assinatura.Cancel();
        _subscriptions.GetByIdAsync(assinatura.Id, Arg.Any<CancellationToken>()).Returns(assinatura);

        var request = new ClinicSubscriptionUpdateRequest(SubscriptionStatus.Active, DateTime.UtcNow, plano.Id);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.UpdateAsync(assinatura.Id, request));

        // Assert
        Assert.IsType<BusinessRuleException>(excecao);
    }

    [Fact]
    public async Task DeleteAsync_ComAssinaturaExistente_RemoveESalva()
    {
        // Arrange
        var assinatura = _fixture.AssinaturaDe(_fixture.ClinicaVidaAnimal(), _fixture.PlanoBasico());
        _subscriptions.GetByIdAsync(assinatura.Id, Arg.Any<CancellationToken>()).Returns(assinatura);

        // Act
        await _service.DeleteAsync(assinatura.Id);

        // Assert
        await _subscriptions.Received(1).RemoveAsync(assinatura, Arg.Any<CancellationToken>());
        await _subscriptions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
