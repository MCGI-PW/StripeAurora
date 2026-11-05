using Aurora.Tests.Unit.Fixtures;
using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Unit.Domain;

[Collection(PlanCollection.Name)]
public class SubscriptionPlanTests
{
    private readonly PlanFixture _fixture;

    public SubscriptionPlanTests(PlanFixture fixture) => _fixture = fixture;

    [Fact]
    public void Constructor_ComDadosValidos_CriaPlanoAtivoComIdGerado()
    {
        // Arrange
        const string nome = "Aurora Basico";
        const decimal preco = 29.90m;

        // Act
        var plano = new SubscriptionPlan(PlanTier.Basic, nome, preco, "price_basic_123");

        // Assert
        Assert.NotEqual(Guid.Empty, plano.Id);
        Assert.Equal(PlanTier.Basic, plano.Tier);
        Assert.Equal(nome, plano.Name);
        Assert.Equal(preco, plano.Price);
        Assert.True(plano.Active);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ComNomeVazio_LancaArgumentException(string nomeInvalido)
    {
        // Arrange
        var criar = () => new SubscriptionPlan(PlanTier.Basic, nomeInvalido, 29.90m, "price_123");

        // Act
        var excecao = Record.Exception(criar);

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void Constructor_ComPrecoNegativo_LancaArgumentOutOfRangeException()
    {
        // Arrange
        var criar = () => new SubscriptionPlan(PlanTier.Premium, "Aurora Premium", -1m, "price_123");

        // Act
        var excecao = Record.Exception(criar);

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(excecao);
    }

    [Fact]
    public void Update_ComDadosValidos_AlteraOsCamposPreservandoOId()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();
        var idOriginal = plano.Id;

        // Act
        plano.Update(PlanTier.Premium, "Aurora Premium", 59.90m, "price_premium_456");

        // Assert
        Assert.Equal(idOriginal, plano.Id);
        Assert.Equal(PlanTier.Premium, plano.Tier);
        Assert.Equal("Aurora Premium", plano.Name);
        Assert.Equal(59.90m, plano.Price);
    }

    [Fact]
    public void Deactivate_EmPlanoAtivo_MarcaPlanoComoInativo()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();

        // Act
        plano.Deactivate();

        // Assert
        Assert.False(plano.Active);
    }

    [Fact]
    public void Activate_EmPlanoInativo_ReativaOPlano()
    {
        // Arrange
        var plano = _fixture.PlanoBasico();
        plano.Deactivate();

        // Act
        plano.Activate();

        // Assert
        Assert.True(plano.Active);
    }
}
