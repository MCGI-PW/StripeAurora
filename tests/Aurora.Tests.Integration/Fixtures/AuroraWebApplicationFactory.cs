using System.Net.Http.Headers;
using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Infrastructure.Data;
using AuroraPet.Infrastructure.Mongo;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Aurora.Tests.Integration.Fixtures;

/// <summary>
/// Sobe a API inteira em memoria (pipeline, controllers, DI, JWT, health checks) trocando
/// apenas a persistencia: os repositorios de negocio usam o EF Core InMemory e os usuarios
/// um repositorio em memoria. Assim a suite roda em qualquer maquina e no CI, sem depender
/// do MongoDB nem do Oracle da FIAP.
/// </summary>
public class AuroraWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Chave usada so nos testes (>= 32 caracteres, exigencia do HMAC-SHA256).</summary>
    public const string TestJwtKey = "chave-de-testes-aurora-com-mais-de-32-caracteres";

    private readonly string _databaseName = $"aurora-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // O Program.cs le estas configuracoes antes de registrar os servicos.
        // A connection string e apenas um placeholder: o provider e substituido logo abaixo.
        Environment.SetEnvironmentVariable("Database__Provider", DatabaseProviders.Oracle);
        Environment.SetEnvironmentVariable("ConnectionStrings__OracleConnection", "Data Source=in-memory-tests");
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var registrosDoEf = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AuroraDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(AuroraDbContext)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var registro in registrosDoEf)
                services.Remove(registro);

            services.AddDbContext<AuroraDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Sem MongoDB nos testes: nada de inicializador, health check do Mongo nem repositorio real de usuarios.
            var mongo = services
                .Where(d => (!d.IsKeyedService && d.ImplementationType == typeof(MongoDbInitializer))
                         || d.ServiceType == typeof(IUserRepository))
                .ToList();
            foreach (var registro in mongo)
                services.Remove(registro);

            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                var mongoCheck = options.Registrations.FirstOrDefault(r => r.Name == "mongodb");
                if (mongoCheck is not null)
                    options.Registrations.Remove(mongoCheck);
            });
        });
    }

    /// <summary>Cliente HTTP com um token JWT valido do perfil informado (Admin por padrao).</summary>
    public HttpClient CreateAuthenticatedClient(string role = UserRoles.Admin)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(role));
        return client;
    }

    /// <summary>Emite um token pelo mesmo ITokenService da API, para um usuario ficticio.</summary>
    public string GenerateToken(string role = UserRoles.Admin)
    {
        var tokens = Services.GetRequiredService<ITokenService>();
        var usuario = new User($"Usuario {role}", $"{role.ToLowerInvariant()}-{Guid.NewGuid():n}@aurora.test", "hash-de-teste", role);
        return tokens.Generate(usuario).Token;
    }

    /// <summary>Abre um escopo com o DbContext, para os testes conferirem a persistencia direto no banco.</summary>
    public IServiceScope CreateScopeWithDb(out AuroraDbContext db)
    {
        var scope = Services.CreateScope();
        db = scope.ServiceProvider.GetRequiredService<AuroraDbContext>();
        return scope;
    }
}

/// <summary>Collection fixture: a API sobe uma unica vez e e reaproveitada por todas as classes de teste.</summary>
[CollectionDefinition(ApiCollection.Name)]
public class ApiCollection : ICollectionFixture<AuroraWebApplicationFactory>
{
    public const string Name = "API Aurora";
}
