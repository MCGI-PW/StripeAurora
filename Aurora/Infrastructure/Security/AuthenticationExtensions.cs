using System.Security.Cryptography;
using System.Text;
using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Auth.Application.Options;
using AuroraPet.Auth.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AuroraPet.Infrastructure.Security;

public static class AuthenticationExtensions
{
    private const int MinKeyBytes = 32; // HMAC-SHA256 exige chave de pelo menos 256 bits

    /// <summary>
    /// Autenticacao por JWT Bearer. A chave vem de Jwt:Key (user-secrets / variavel Jwt__Key).
    /// Fora de Production, sem chave configurada, gera uma chave temporaria (tokens deixam de valer
    /// ao reiniciar a API); em Production a ausencia da chave impede a subida.
    /// </summary>
    public static void AddAuroraJwtAuthentication(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(JwtOptions.SectionName);
        var jwt = section.Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key) && !builder.Environment.IsProduction())
        {
            jwt.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            Console.WriteLine("[AVISO] Jwt:Key nao configurada: usando chave temporaria de desenvolvimento (tokens expiram ao reiniciar). Configure via user-secrets.");
        }

        if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < MinKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:Key nao configurada ou com menos de {MinKeyBytes} caracteres. Defina a variavel de ambiente " +
                "Jwt__Key ou rode 'dotnet user-secrets set \"Jwt:Key\" \"...\"'. Veja o README.");
        }

        // Os servicos leem as mesmas opcoes que validam o token (inclusive a chave temporaria).
        builder.Services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwt.Issuer;
            options.Audience = jwt.Audience;
            options.Key = jwt.Key;
            options.ExpirationMinutes = jwt.ExpirationMinutes;
        });
        builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection(AdminSeedOptions.SectionName));

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
        builder.Services.AddSingleton<ITokenService, JwtTokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.Key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AuroraClaims.Name,
                    RoleClaimType = AuroraClaims.Role
                };

                // 401/403 no mesmo formato ProblemDetails do resto da API.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            "Nao autenticado",
                            "Envie um token valido no header 'Authorization: Bearer {token}'. Obtenha-o em POST /api/auth/login.");
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "Acesso negado", "O seu perfil nao tem permissao para esta operacao.")
                };
            });

        builder.Services.AddAuthorization();
    }

    private static Task WriteProblemAsync(HttpContext httpContext, int status, string title, string detail)
    {
        httpContext.Response.StatusCode = status;
        return httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = detail },
            options: null,
            contentType: "application/problem+json");
    }
}
