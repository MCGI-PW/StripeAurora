using AuroraPet.Auth.Application.Dtos;
using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Payments.Application.Services;
using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Auth.Application.Services;

/// <summary>E-mail inexistente, senha errada ou usuario desativado. Traduzida em 401 pelo controller.</summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("E-mail ou senha invalidos.") { }
}

/// <summary>Casos de uso de autenticacao: cadastro, login e perfil do usuario logado.</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserResponse> GetProfileAsync(Guid userId, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    public const int MinPasswordLength = 8;

    private readonly IUserRepository _users;
    private readonly IPasswordHashService _passwordHasher;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordHashService passwordHasher,
        ITokenService tokens,
        ILogger<AuthService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ValidatePassword(request.Password);

        if (await _users.GetByEmailAsync(request.Email, ct) is not null)
            throw new BusinessRuleException($"Ja existe um usuario cadastrado com o e-mail {User.NormalizeEmail(request.Email)}.");

        // Cadastro publico nunca cria administrador: o Admin vem do seed (Auth:AdminEmail).
        var user = new User(request.Name, request.Email, _passwordHasher.Hash(request.Password), UserRoles.Clinic);

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        _logger.LogInformation("Usuario {UserId} ({Email}) cadastrado com o perfil {Role}", user.Id, user.Email, user.Role);
        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            throw new InvalidCredentialsException();

        var user = await _users.GetByEmailAsync(request.Email, ct);

        // Mesma excecao para e-mail inexistente e senha errada: nao revela quais e-mails existem.
        if (user is null || !user.Active || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogWarning("Tentativa de login recusada para {Email}", User.NormalizeEmail(request.Email));
            throw new InvalidCredentialsException();
        }

        _logger.LogInformation("Usuario {UserId} autenticado", user.Id);
        return BuildResponse(user);
    }

    public async Task<UserResponse> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new ResourceNotFoundException($"Usuario {userId} nao encontrado.");

        return UserResponse.FromEntity(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = _tokens.Generate(user);
        return new AuthResponse(token.Token, "Bearer", token.ExpiresAt, UserResponse.FromEntity(user));
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength
            || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new ArgumentException(
                $"A senha deve ter no minimo {MinPasswordLength} caracteres, com letras e numeros.", nameof(password));
        }
    }
}
