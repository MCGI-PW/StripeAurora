using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Infrastructure.Data;
using AuroraPet.Infrastructure.Data.Repositories;
using AuroraPet.Payments.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AuroraPet.Infrastructure.Mongo;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registra a persistencia. O MongoDB e o banco principal: guarda os usuarios do login
    /// JWT e, com Database:Provider = "MongoDB" (padrao), tambem clinicas, planos e assinaturas.
    /// Com Database:Provider = "Oracle", esses tres repositorios voltam para o EF Core/Oracle,
    /// mantendo as migrations da entrega relacional.
    /// </summary>
    /// <returns>true quando o Oracle (EF Core) esta registrado.</returns>
    public static bool AddAuroraPersistence(this WebApplicationBuilder builder)
    {
        var provider = builder.Configuration[DatabaseProviders.SectionKey] ?? DatabaseProviders.MongoDb;
        var usarOracle = provider.Equals(DatabaseProviders.Oracle, StringComparison.OrdinalIgnoreCase);

        if (!usarOracle && !provider.Equals(DatabaseProviders.MongoDb, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database:Provider '{provider}' invalido. Use '{DatabaseProviders.MongoDb}' ou '{DatabaseProviders.Oracle}'.");
        }

        // ---------- MongoDB ----------
        MongoMappings.Register();
        builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(options.ServerSelectionTimeoutSeconds);
            settings.ApplicationName = "aurora-api";
            return new MongoClient(settings);
        });
        builder.Services.AddSingleton<MongoContext>();
        builder.Services.AddHostedService<MongoDbInitializer>();
        builder.Services.AddScoped<IUserRepository, MongoUserRepository>();

        // ---------- Oracle (EF Core) - opcional ----------
        // A connection string NAO fica no repositorio: user-secrets em desenvolvimento,
        // variavel ConnectionStrings__OracleConnection em container/nuvem.
        var oracleConnectionString = builder.Configuration.GetConnectionString("OracleConnection");

        if (usarOracle && string.IsNullOrWhiteSpace(oracleConnectionString))
        {
            throw new InvalidOperationException(
                "Database:Provider = Oracle, mas a connection string 'OracleConnection' nao foi configurada. " +
                "Defina ConnectionStrings__OracleConnection ou rode 'dotnet user-secrets set'. Veja o README.");
        }

        // Com a connection string presente, o DbContext fica disponivel mesmo no modo MongoDB,
        // para que 'dotnet ef database update' continue funcionando.
        var oracleRegistrado = !string.IsNullOrWhiteSpace(oracleConnectionString);
        if (oracleRegistrado)
        {
            builder.Services.AddDbContext<AuroraDbContext>(options => options.UseOracle(oracleConnectionString));
        }

        if (usarOracle)
        {
            builder.Services.AddScoped<IClinicRepository, ClinicRepository>();
            builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            builder.Services.AddScoped<IClinicSubscriptionRepository, ClinicSubscriptionRepository>();
        }
        else
        {
            builder.Services.AddScoped<IClinicRepository, MongoClinicRepository>();
            builder.Services.AddScoped<ISubscriptionPlanRepository, MongoSubscriptionPlanRepository>();
            builder.Services.AddScoped<IClinicSubscriptionRepository, MongoClinicSubscriptionRepository>();
        }

        return oracleRegistrado;
    }
}
