using Aurora.Tests.Unit.Fixtures;
using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Unit.Domain;

[Collection(PlanCollection.Name)]
public class ClinicSubscriptionTests
{
    private readonly PlanFixture _fixture;

    public ClinicSubscriptionTests(PlanFixture fixture) => _fixture = fixture;

    [Fact]
    public void Constructor_ComClinicaEPlanoValidos_CriaAssinaturaIncompleta()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        var plano = _fixture.PlanoBasico();

        // Act
        var assinatura = new ClinicSubscription(clinica.Id, plano.Id, "cus_1", "sub_1");

        // Assert
        Assert.NotEqual(Guid.Empty, assinatura.Id);
        Assert.Equal(clinica.Id, assinatura.ClinicId);
        Assert.Equal(plano.Id, assinatura.PlanId);
        Assert.Equal(SubscriptionStatus.Incomplete, assinatura.Status);
    }

    [Fact]
    public void Constructor_ComClinicaVazia_LancaArgumentException()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();
        var criar = () => new ClinicSubscription(Guid.Empty, plano.Id, "cus_1", "sub_1");

        // Act
        var excecao = Record.Exception(criar);

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void Constructor_ComPlanoVazio_LancaArgumentException()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        var criar = () => new ClinicSubscription(clinica.Id, Guid.Empty, "cus_1", "sub_1");

        // Act
        var excecao = Record.Exception(criar);

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void UpdateStatus_DeIncompleteParaActive_AtualizaStatusEVigencia()
    {
        // Arrange
        var assinatura = _fixture.AssinaturaDe(_fixture.ClinicaVidaAnimal(), _fixture.PlanoBasico());
        var fimDoPeriodo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        assinatura.UpdateStatus(SubscriptionStatus.Active, fimDoPeriodo);

        // Assert
        Assert.Equal(SubscriptionStatus.Active, assinatura.Status);
        Assert.Equal(fimDoPeriodo, assinatura.CurrentPeriodEnd);
    }

    [Fact]
    public void UpdateStatus_EmAssinaturaCancelada_LancaInvalidOperationException()
    {
        // Arrange
        var assinatura = _fixture.AssinaturaDe(_fixture.ClinicaVidaAnimal(), _fixture.PlanoBasico());
        assinatura.Cancel();

        // Act
        var excecao = Record.Exception(() => assinatura.UpdateStatus(SubscriptionStatus.Active, DateTime.UtcNow));

        // Assert
        Assert.IsType<InvalidOperationException>(excecao);
    }

    [Fact]
    public void ChangePlan_ComPlanoValido_TrocaOPlanoMantendoAClinica()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        var premium = _fixture.PlanoPremium();
        var assinatura = _fixture.AssinaturaDe(clinica, _fixture.PlanoBasico());

        // Act
        assinatura.ChangePlan(premium.Id);

        // Assert
        Assert.Equal(premium.Id, assinatura.PlanId);
        Assert.Equal(clinica.Id, assinatura.ClinicId);
    }

    [Fact]
    public void Cancel_EmAssinaturaAtiva_MarcaStatusComoCanceled()
    {
        // Arrange
        var assinatura = _fixture.AssinaturaDe(_fixture.ClinicaVidaAnimal(), _fixture.PlanoBasico());
        assinatura.UpdateStatus(SubscriptionStatus.Active, DateTime.UtcNow.AddDays(30));

        // Act
        assinatura.Cancel();

        // Assert
        Assert.Equal(SubscriptionStatus.Canceled, assinatura.Status);
    }
}
