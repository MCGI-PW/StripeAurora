using System.Security.Claims;
using System.Text;
using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Auth.Application.Options;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuroraPet.Infrastructure.Security;

/// <summary>Nomes das claims emitidas no token (MapInboundClaims = false: chegam com esses nomes).</summary>
public static class AuroraClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}

/// <summary>Hash de senha com PBKDF2 (HMAC-SHA512, salt aleatorio) do ASP.NET Core Identity.</summary>
public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(string.Empty, password);

    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(password))
            return false;

        try
        {
            return _hasher.VerifyHashedPassword(string.Empty, passwordHash, password)
                is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false; // hash corrompido no banco
        }
    }
}

/// <summary>Emite JWT assinado com HMAC-SHA256 contendo id, e-mail, nome e perfil do usuario.</summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessToken Generate(User user)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(AuroraClaims.Subject, user.Id.ToString()),
                new Claim(AuroraClaims.Email, user.Email),
                new Claim(AuroraClaims.Name, user.Name),
                new Claim(AuroraClaims.Role, user.Role),
                new Claim("jti", Guid.NewGuid().ToString("n"))
            }),
            SigningCredentials = new SigningCredentials(CreateSigningKey(_options.Key), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }

    public static SymmetricSecurityKey CreateSigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}
