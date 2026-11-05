using AuroraPet.Auth.Application.Options;
using AuroraPet.Infrastructure.Security;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aurora.Tests.Unit.Infrastructure;

public class SecurityServicesTests
{
    private static readonly JwtOptions Opcoes = new()
    {
        Issuer = "aurora-api",
        Audience = "aurora-app",
        Key = "chave-de-testes-unitarios-com-mais-de-32-caracteres",
        ExpirationMinutes = 30
    };

    private static TokenValidationParameters ParametrosDeValidacao(string chave) => new()
    {
        ValidIssuer = Opcoes.Issuer,
        ValidAudience = Opcoes.Audience,
        IssuerSigningKey = JwtTokenService.CreateSigningKey(chave),
        ValidateLifetime = true
    };

    [Fact]
    public async Task Generate_ComUsuarioValido_EmiteTokenAssinadoComIdEPerfil()
    {
        // Arrange
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Opcoes), TimeProvider.System);
        var user = new User("Admin", "admin@aurora.com", "hash", UserRoles.Admin);

        // Act
        var token = service.Generate(user);
        var validacao = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ParametrosDeValidacao(Opcoes.Key));

        // Assert
        Assert.True(validacao.IsValid);
        Assert.Equal(user.Id.ToString(), validacao.Claims[AuroraClaims.Subject]);
        Assert.Equal(UserRoles.Admin, validacao.Claims[AuroraClaims.Role]);
        Assert.True(token.ExpiresAt > DateTime.UtcNow.AddMinutes(29));
    }

    [Fact]
    public async Task Generate_ValidadoComOutraChave_TokenEhRejeitado()
    {
        // Arrange
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Opcoes), TimeProvider.System);
        var token = service.Generate(new User("Admin", "admin@aurora.com", "hash", UserRoles.Admin));

        // Act
        var validacao = await new JsonWebTokenHandler().ValidateTokenAsync(
            token.Token, ParametrosDeValidacao("uma-chave-completamente-diferente-de-32-chars"));

        // Assert
        Assert.False(validacao.IsValid);
    }

    [Fact]
    public void PasswordHash_ComSenhaCorreta_VerificaComSucessoESemGuardarTextoPuro()
    {
        // Arrange
        var hasher = new PasswordHashService();

        // Act
        var hash = hasher.Hash("Senha@2026");

        // Assert
        Assert.NotEqual("Senha@2026", hash);
        Assert.True(hasher.Verify(hash, "Senha@2026"));
        Assert.False(hasher.Verify(hash, "senha@2026"));
    }

    [Fact]
    public void PasswordHash_ComHashCorrompido_RetornaFalse()
    {
        // Arrange
        var hasher = new PasswordHashService();

        // Act
        var resultado = hasher.Verify("isto-nao-e-um-hash", "Senha@2026");

        // Assert
        Assert.False(resultado);
    }
}
