using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Auth.Application.Dtos;

/// <summary>Payload de cadastro de um usuario (sempre criado com o perfil Clinic).</summary>
public record RegisterRequest(string Name, string Email, string Password);

/// <summary>Payload de login.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Representacao publica de um usuario (sem hash de senha).</summary>
public record UserResponse(Guid Id, string Name, string Email, string Role)
{
    public static UserResponse FromEntity(User user) => new(user.Id, user.Name, user.Email, user.Role);
}

/// <summary>Resposta de login/cadastro: o token vai no header Authorization: Bearer {accessToken}.</summary>
public record AuthResponse(string AccessToken, string TokenType, DateTime ExpiresAt, UserResponse User);
