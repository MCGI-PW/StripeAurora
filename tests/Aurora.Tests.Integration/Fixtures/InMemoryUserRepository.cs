using System.Collections.Concurrent;
using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Integration.Fixtures;

/// <summary>Substitui o repositorio MongoDB de usuarios nos testes de integracao.</summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_users.TryGetValue(id, out var user) ? user : null);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = User.NormalizeEmail(email);
        return Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == normalized));
    }

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
