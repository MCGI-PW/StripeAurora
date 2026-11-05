using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Auth.Application.Options;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AuroraPet.Infrastructure.Mongo;

/// <summary>
/// Executado uma vez na subida da API: cria os indices (o equivalente as constraints
/// UNIQUE do Oracle) e insere a carga inicial e o usuario administrador quando ainda
/// nao existem. Falhas sao logadas sem derrubar a API - o /health/ready acusa o problema.
/// </summary>
public class MongoDbInitializer : IHostedService
{
    // Mesmos ids fixos do seed do Oracle, para que as duas bases tenham os mesmos dados.
    public static readonly Guid ClinicaVidaAnimalId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ClinicaPatasFelizesId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid PlanoBasicoId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PlanoPremiumId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AssinaturaAtivaId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AssinaturaIncompletaId = new("44444444-4444-4444-4444-444444444444");

    private readonly MongoContext _context;
    private readonly MongoDbOptions _mongoOptions;
    private readonly AdminSeedOptions _adminOptions;
    private readonly IPasswordHashService _passwordHasher;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(
        MongoContext context,
        IOptions<MongoDbOptions> mongoOptions,
        IOptions<AdminSeedOptions> adminOptions,
        IPasswordHashService passwordHasher,
        ILogger<MongoDbInitializer> logger)
    {
        _context = context;
        _mongoOptions = mongoOptions.Value;
        _adminOptions = adminOptions.Value;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CreateIndexesAsync(cancellationToken);

            if (_mongoOptions.SeedData)
                await SeedBusinessDataAsync(cancellationToken);

            await SeedAdminAsync(cancellationToken);

            _logger.LogInformation("MongoDB inicializado no banco {Database}", _mongoOptions.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nao foi possivel inicializar o MongoDB em {Database}. Verifique MongoDb:ConnectionString.",
                _mongoOptions.DatabaseName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateIndexesAsync(CancellationToken ct)
    {
        await _context.Clinics.Indexes.CreateManyAsync(
        new[]
        {
            new CreateIndexModel<Clinic>(Builders<Clinic>.IndexKeys.Ascending(c => c.Cnpj),
                new CreateIndexOptions { Unique = true, Name = "ux_clinic_cnpj" }),
            new CreateIndexModel<Clinic>(Builders<Clinic>.IndexKeys.Ascending(c => c.FirebaseUid),
                new CreateIndexOptions { Unique = true, Name = "ux_clinic_firebase_uid" })
        }, cancellationToken: ct);

        await _context.SubscriptionPlans.Indexes.CreateOneAsync(
            new CreateIndexModel<SubscriptionPlan>(Builders<SubscriptionPlan>.IndexKeys.Ascending(p => p.StripePriceId),
                new CreateIndexOptions { Unique = true, Name = "ux_plan_stripe_price" }),
            cancellationToken: ct);

        await _context.ClinicSubscriptions.Indexes.CreateManyAsync(
        new[]
        {
            new CreateIndexModel<ClinicSubscription>(Builders<ClinicSubscription>.IndexKeys.Ascending(s => s.ClinicId),
                new CreateIndexOptions { Name = "ix_subscription_clinic" }),
            new CreateIndexModel<ClinicSubscription>(Builders<ClinicSubscription>.IndexKeys.Ascending(s => s.PlanId),
                new CreateIndexOptions { Name = "ix_subscription_plan" }),
            new CreateIndexModel<ClinicSubscription>(Builders<ClinicSubscription>.IndexKeys.Ascending(s => s.StripeSubscriptionId),
                new CreateIndexOptions { Unique = true, Name = "ux_subscription_stripe_id" })
        }, cancellationToken: ct);

        await _context.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_user_email" }),
            cancellationToken: ct);
    }

    private async Task SeedBusinessDataAsync(CancellationToken ct)
    {
        if (await _context.SubscriptionPlans.CountDocumentsAsync(FilterDefinition<SubscriptionPlan>.Empty, cancellationToken: ct) == 0)
        {
            await _context.SubscriptionPlans.InsertManyAsync(
            new[]
            {
                WithId(new SubscriptionPlan(PlanTier.Basic, "Aurora Basico", 29.90m, "price_aurora_basico"), PlanoBasicoId),
                WithId(new SubscriptionPlan(PlanTier.Premium, "Aurora Premium", 59.90m, "price_aurora_premium"), PlanoPremiumId)
            }, cancellationToken: ct);
            _logger.LogInformation("Carga inicial de planos inserida no MongoDB");
        }

        if (await _context.Clinics.CountDocumentsAsync(FilterDefinition<Clinic>.Empty, cancellationToken: ct) == 0)
        {
            await _context.Clinics.InsertManyAsync(
            new[]
            {
                WithId(new Clinic("Clinica Vida Animal", "12345678000190", "contato@vidaanimal.com.br", "uid-clinica-vida-animal"), ClinicaVidaAnimalId),
                WithId(new Clinic("Clinica Patas Felizes", "98765432000155", "financeiro@patasfelizes.com.br", "uid-clinica-patas-felizes"), ClinicaPatasFelizesId)
            }, cancellationToken: ct);
            _logger.LogInformation("Carga inicial de clinicas inserida no MongoDB");
        }

        if (await _context.ClinicSubscriptions.CountDocumentsAsync(FilterDefinition<ClinicSubscription>.Empty, cancellationToken: ct) == 0)
        {
            var ativa = WithId(new ClinicSubscription(ClinicaVidaAnimalId, PlanoPremiumId, "cus_aurora_vida_animal", "sub_aurora_vida_animal"), AssinaturaAtivaId);
            ativa.UpdateStatus(SubscriptionStatus.Active, new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));

            var incompleta = WithId(new ClinicSubscription(ClinicaPatasFelizesId, PlanoBasicoId, "cus_aurora_patas_felizes", "sub_aurora_patas_felizes"), AssinaturaIncompletaId);
            incompleta.UpdateStatus(SubscriptionStatus.Incomplete, new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc));

            await _context.ClinicSubscriptions.InsertManyAsync(new[] { ativa, incompleta }, cancellationToken: ct);
            _logger.LogInformation("Carga inicial de assinaturas inserida no MongoDB");
        }
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_adminOptions.AdminEmail) || string.IsNullOrWhiteSpace(_adminOptions.AdminPassword))
        {
            _logger.LogInformation("Auth:AdminEmail/AdminPassword nao configurados: nenhum administrador criado automaticamente");
            return;
        }

        var email = User.NormalizeEmail(_adminOptions.AdminEmail);
        if (await _context.Users.Find(u => u.Email == email).AnyAsync(ct))
            return;

        var admin = new User("Administrador Aurora", email, _passwordHasher.Hash(_adminOptions.AdminPassword), UserRoles.Admin);
        await _context.Users.InsertOneAsync(admin, cancellationToken: ct);
        _logger.LogInformation("Usuario administrador {Email} criado no MongoDB", email);
    }

    /// <summary>Os construtores geram um Guid novo; o seed precisa de ids fixos para ser idempotente.</summary>
    private static T WithId<T>(T entity, Guid id) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}
