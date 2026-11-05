using System;
using System.Linq;

namespace AuroraPet.Payments.Domain.Entities;

/// <summary>Perfis de acesso aceitos pela API (claim "role" do JWT).</summary>
public static class UserRoles
{
    /// <summary>Administrador da plataforma: gerencia o catalogo de planos.</summary>
    public const string Admin = "Admin";

    /// <summary>Usuario de uma clinica: consulta e contrata planos.</summary>
    public const string Clinic = "Clinic";

    public static readonly string[] All = [Admin, Clinic];

    public static bool IsValid(string role) => All.Contains(role);
}

/// <summary>
/// Usuario que se autentica na API e recebe um token JWT. A senha nunca e guardada:
/// a entidade so conhece o hash, gerado pela camada de infraestrutura.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = UserRoles.Clinic;
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // MongoDB

    public User(string name, string email, string passwordHash, string role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do usuario e obrigatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("E-mail do usuario invalido.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("O hash da senha e obrigatorio.", nameof(passwordHash));

        EnsureValidRole(role);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(string role)
    {
        EnsureValidRole(role);
        Role = role;
    }

    public void Activate() => Active = true;

    public void Deactivate() => Active = false;

    /// <summary>E-mail e a chave de login: comparado sempre sem espacos e em minusculas.</summary>
    public static string NormalizeEmail(string email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static void EnsureValidRole(string role)
    {
        if (!UserRoles.IsValid(role))
            throw new ArgumentException($"Perfil '{role}' invalido. Use: {string.Join(", ", UserRoles.All)}.", nameof(role));
    }
}
