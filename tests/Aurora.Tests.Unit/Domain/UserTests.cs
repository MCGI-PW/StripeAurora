using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Unit.Domain;

public class UserTests
{
    [Fact]
    public void Construtor_ComDadosValidos_CriaUsuarioAtivoComEmailNormalizado()
    {
        // Arrange & Act
        var user = new User("Maria Recepcao", "  Maria@VidaAnimal.com.BR ", "hash", UserRoles.Clinic);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("maria@vidaanimal.com.br", user.Email);
        Assert.Equal(UserRoles.Clinic, user.Role);
        Assert.True(user.Active);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sem-arroba.com.br")]
    public void Construtor_ComEmailInvalido_LancaArgumentException(string email)
    {
        // Arrange & Act
        var excecao = Record.Exception(() => new User("Maria", email, "hash", UserRoles.Clinic));

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void Construtor_SemHashDeSenha_LancaArgumentException()
    {
        // Arrange & Act
        var excecao = Record.Exception(() => new User("Maria", "maria@x.com", " ", UserRoles.Clinic));

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void Construtor_ComPerfilInexistente_LancaArgumentException()
    {
        // Arrange & Act
        var excecao = Record.Exception(() => new User("Maria", "maria@x.com", "hash", "SuperUsuario"));

        // Assert
        Assert.IsType<ArgumentException>(excecao);
    }

    [Fact]
    public void ChangeRole_ParaAdmin_AlteraOPerfil()
    {
        // Arrange
        var user = new User("Maria", "maria@x.com", "hash", UserRoles.Clinic);

        // Act
        user.ChangeRole(UserRoles.Admin);

        // Assert
        Assert.Equal(UserRoles.Admin, user.Role);
    }

    [Fact]
    public void Deactivate_ComUsuarioAtivo_DesativaOUsuario()
    {
        // Arrange
        var user = new User("Maria", "maria@x.com", "hash", UserRoles.Clinic);

        // Act
        user.Deactivate();

        // Assert
        Assert.False(user.Active);
    }
}
