namespace AuroraPet.Auth.Application.Options;

/// <summary>Configuracao do JWT (secao "Jwt" do appsettings / variaveis Jwt__*).</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "aurora-api";
    public string Audience { get; set; } = "aurora-app";

    /// <summary>Chave simetrica HMAC-SHA256: no minimo 32 caracteres. Nunca versionar.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>Administrador criado automaticamente na subida (secao "Auth").</summary>
public class AdminSeedOptions
{
    public const string SectionName = "Auth";

    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
