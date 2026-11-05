using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Auth.Application.Interfaces;

/// <summary>Persistencia dos usuarios da API (colecao "users" no MongoDB).</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Gera e confere hashes de senha. A implementacao fica na infraestrutura.</summary>
public interface IPasswordHashService
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

/// <summary>Token de acesso emitido para um usuario autenticado.</summary>
public record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>Emite tokens JWT assinados para um usuario.</summary>
public interface ITokenService
{
    AccessToken Generate(User user);
}
