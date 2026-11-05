using Aurora.Tests.Unit.Fixtures;
using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Unit.Domain;

[Collection(PlanCollection.Name)]
public class ClinicTests
{
    private readonly PlanFixture _fixture;

    public ClinicTests(PlanFixture fixture) => _fixture = fixture;

    [Fact]
    public void Constructor_ComDadosValidos_CriaClinicaAtivaComIdGerado()
    {
        // Arrange
        const string nome = "Clinica Vida Animal";

        // Act
        var clinica = new Clinic(nome, "12345678000190", "contato@vidaanimal.com.br", "uid-clinica-vida-animal");

        // Assert
        Assert.NotEqual(Guid.Empty, clinica.Id);
        Assert.Equal(nome, clinica.Name);
        Assert.True(clinica.Active);
    }

    [Fact]
    public void Constructor_ComCnpjFormatado_ArmazenaSomenteDigitos()
    {
        // Arrange & Act
        var clinica = _fixture.ClinicaVidaAnimal();

        // Assert
        Assert.Equal("12345678000190", clinica.Cnpj);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("1234567800019012345")]
    public void Constructor_ComCnpjDeTamanhoInvalido_LancaArgumentException(string cnpjInvalido)
    {
        // Arrange
        var criar = () => new Clinic("Clinica X", cnpjInvalido, "contato@x.com.br", "uid-x");

        // Act
        var excecao = Record.Exception(criar);

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void Constructor_ComEmailSemArroba_LancaArgumentException()
    {
        // Arrange
        var criar = () => new Clinic("Clinica X", "12345678000190", "contato-invalido", "uid-x");

        // Act
        var excecao = Record.Exception(criar);

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void Update_ComDadosValidos_AlteraCadastroPreservandoIdEFirebaseUid()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();
        var idOriginal = clinica.Id;
        var uidOriginal = clinica.FirebaseUid;

        // Act
        clinica.Update("Clinica Vida Animal 24h", "98765432000155", "novo@vidaanimal.com.br");

        // Assert
        Assert.Equal(idOriginal, clinica.Id);
        Assert.Equal(uidOriginal, clinica.FirebaseUid);
        Assert.Equal("Clinica Vida Animal 24h", clinica.Name);
        Assert.Equal("98765432000155", clinica.Cnpj);
    }

    [Fact]
    public void Deactivate_EmClinicaAtiva_MarcaComoInativa()
    {
        // Arrange
        var clinica = _fixture.ClinicaVidaAnimal();

        // Act
        clinica.Deactivate();

        // Assert
        Assert.False(clinica.Active);
    }
}
