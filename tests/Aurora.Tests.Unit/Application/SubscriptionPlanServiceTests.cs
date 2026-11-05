using Aurora.Tests.Unit.Fixtures;
using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Application.Services;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aurora.Tests.Unit.Application;

[Collection(PlanCollection.Name)]
public class SubscriptionPlanServiceTests
{
    private readonly PlanFixture _fixture;
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IClinicSubscriptionRepository _subscriptions = Substitute.For<IClinicSubscriptionRepository>();
    private readonly SubscriptionPlanService _service;

    public SubscriptionPlanServiceTests(PlanFixture fixture)
    {
        _fixture = fixture;
        _service = new SubscriptionPlanService(_plans, _subscriptions, Substitute.For<ILogger<SubscriptionPlanService>>());
    }

    [Fact]
    public async Task GetAllAsync_ComDoisPlanosCadastrados_RetornaOsDoisPlanos()
    {
        // Arrange
        var basico = _fixture.PlanoBasico();
        var premium = _fixture.PlanoPremium();
        _plans.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<SubscriptionPlan> { basico, premium });

        // Act
        var resultado = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, p => p.Name == "Aurora Basico");
        Assert.Contains(resultado, p => p.Name == "Aurora Premium");
    }

    [Fact]
    public async Task GetByIdAsync_ComPlanoInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        _plans.GetByIdAsync(idInexistente, Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.GetByIdAsync(idInexistente));

        // Assert
        Assert.IsType<ResourceNotFoundException>(excecao);
    }

    [Fact]
    public async Task CreateAsync_ComRequisicaoValida_PersisteOPlanoESalva()
    {
        // Arrange
        var request = new SubscriptionPlanRequest(PlanTier.Premium, "Aurora Premium", 59.90m, "price_premium_456");

        // Act
        var resultado = await _service.CreateAsync(request);

        // Assert
        Assert.Equal("Aurora Premium", resultado.Name);
        Assert.True(resultado.Active);
        await _plans.Received(1).AddAsync(Arg.Is<SubscriptionPlan>(p => p.Name == "Aurora Premium"), Arg.Any<CancellationToken>());
        await _plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ComPlanoExistente_AtualizaOsDadosESalva()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();
        _plans.GetByIdAsync(plano.Id, Arg.Any<CancellationToken>()).Returns(plano);
        var request = new SubscriptionPlanRequest(PlanTier.Premium, "Aurora Premium", 79.90m, "price_novo_789");

        // Act
        var resultado = await _service.UpdateAsync(plano.Id, request);

        // Assert
        Assert.Equal("Aurora Premium", resultado.Name);
        Assert.Equal(79.90m, resultado.Price);
        await _plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ComPlanoSemAssinaturas_RemoveOPlano()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();
        _plans.GetByIdAsync(plano.Id, Arg.Any<CancellationToken>()).Returns(plano);
        _subscriptions.ListByPlanAsync(plano.Id, Arg.Any<CancellationToken>()).Returns(new List<ClinicSubscription>());

        // Act
        await _service.DeleteAsync(plano.Id);

        // Assert
        await _plans.Received(1).RemoveAsync(plano, Arg.Any<CancellationToken>());
        await _plans.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ComPlanoQuePossuiAssinaturas_LancaBusinessRuleExceptionENaoRemove()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();
        _plans.GetByIdAsync(plano.Id, Arg.Any<CancellationToken>()).Returns(plano);
        _subscriptions.ListByPlanAsync(plano.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ClinicSubscription> { _fixture.AssinaturaDe(_fixture.ClinicaVidaAnimal(), plano) });

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.DeleteAsync(plano.Id));

        // Assert
        Assert.IsType<BusinessRuleException>(excecao);
        await _plans.DidNotReceive().RemoveAsync(Arg.Any<SubscriptionPlan>(), Arg.Any<CancellationToken>());
    }
}
