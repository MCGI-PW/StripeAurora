using AuroraPet.Auth.Application.Dtos;
using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Auth.Application.Services;
using AuroraPet.Payments.Application.Services;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aurora.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHashService _hasher = Substitute.For<IPasswordHashService>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hash-gerado");
        _tokens.Generate(Arg.Any<User>()).Returns(new AccessToken("token-jwt", DateTime.UtcNow.AddHours(1)));
        _service = new AuthService(_users, _hasher, _tokens, Substitute.For<ILogger<AuthService>>());
    }

    [Fact]
    public async Task RegisterAsync_ComEmailInedito_SalvaUsuarioComHashEPerfilClinic()
    {
        // Arrange
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var request = new RegisterRequest("Joao", "joao@clinica.com.br", "Senha1234");

        // Act
        var resposta = await _service.RegisterAsync(request);

        // Assert
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash == "hash-gerado" && u.Role == UserRoles.Clinic),
            Arg.Any<CancellationToken>());
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("token-jwt", resposta.AccessToken);
        Assert.Equal(UserRoles.Clinic, resposta.User.Role);
    }

    [Fact]
    public async Task RegisterAsync_ComEmailJaCadastrado_LancaBusinessRuleExceptionENaoSalva()
    {
        // Arrange
        var existente = new User("Joao", "joao@clinica.com.br", "hash", UserRoles.Clinic);
        _users.GetByEmailAsync("joao@clinica.com.br", Arg.Any<CancellationToken>()).Returns(existente);

        // Act
        var excecao = await Record.ExceptionAsync(() =>
            _service.RegisterAsync(new RegisterRequest("Outro", "joao@clinica.com.br", "Senha1234")));

        // Assert
        Assert.IsType<BusinessRuleException>(excecao);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("curta1")]
    [InlineData("somenteletras")]
    [InlineData("1234567890")]
    public async Task RegisterAsync_ComSenhaFraca_LancaArgumentException(string senha)
    {
        // Arrange
        var request = new RegisterRequest("Joao", "joao@clinica.com.br", senha);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.RegisterAsync(request));

        // Assert
        Assert.IsType<ArgumentException>(excecao);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_ComSenhaCorreta_RetornaToken()
    {
        // Arrange
        var user = new User("Joao", "joao@clinica.com.br", "hash-salvo", UserRoles.Clinic);
        _users.GetByEmailAsync("joao@clinica.com.br", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("hash-salvo", "Senha1234").Returns(true);

        // Act
        var resposta = await _service.LoginAsync(new LoginRequest("joao@clinica.com.br", "Senha1234"));

        // Assert
        Assert.Equal("token-jwt", resposta.AccessToken);
        Assert.Equal(user.Id, resposta.User.Id);
    }

    [Fact]
    public async Task LoginAsync_ComSenhaErrada_LancaInvalidCredentialsException()
    {
        // Arrange
        var user = new User("Joao", "joao@clinica.com.br", "hash-salvo", UserRoles.Clinic);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.LoginAsync(new LoginRequest("joao@clinica.com.br", "errada123")));

        // Assert
        Assert.IsType<InvalidCredentialsException>(excecao);
        _tokens.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task LoginAsync_ComEmailInexistente_LancaInvalidCredentialsException()
    {
        // Arrange
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.LoginAsync(new LoginRequest("ninguem@x.com", "Senha1234")));

        // Assert
        Assert.IsType<InvalidCredentialsException>(excecao);
    }

    [Fact]
    public async Task LoginAsync_ComUsuarioDesativado_LancaInvalidCredentialsException()
    {
        // Arrange
        var user = new User("Joao", "joao@clinica.com.br", "hash-salvo", UserRoles.Clinic);
        user.Deactivate();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.LoginAsync(new LoginRequest("joao@clinica.com.br", "Senha1234")));

        // Assert
        Assert.IsType<InvalidCredentialsException>(excecao);
    }

    [Fact]
    public async Task GetProfileAsync_ComUsuarioInexistente_LancaResourceNotFoundException()
    {
        // Arrange
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var excecao = await Record.ExceptionAsync(() => _service.GetProfileAsync(Guid.NewGuid()));

        // Assert
        Assert.IsType<ResourceNotFoundException>(excecao);
    }
}
