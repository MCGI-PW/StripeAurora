using AuroraPet.Auth.Application.Interfaces;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Domain.Entities;
using MongoDB.Driver;

namespace AuroraPet.Infrastructure.Mongo;

/// <summary>Clinicas na colecao "clinics".</summary>
public class MongoClinicRepository : MongoRepositoryBase<Clinic>, IClinicRepository
{
    public MongoClinicRepository(MongoContext context) : base(context.Clinics) { }

    protected override Guid GetId(Clinic entity) => entity.Id;

    public async Task<IReadOnlyList<Clinic>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<Clinic>.Empty).SortBy(c => c.Name).ToListAsync(ct);

    public Task<Clinic?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        FindTrackedAsync(ById(id), ct);

    public async Task<Clinic?> GetByCnpjAsync(string cnpj, CancellationToken ct = default) =>
        await Collection.Find(c => c.Cnpj == cnpj).FirstOrDefaultAsync(ct);
}

/// <summary>Planos na colecao "subscription_plans".</summary>
public class MongoSubscriptionPlanRepository : MongoRepositoryBase<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public MongoSubscriptionPlanRepository(MongoContext context) : base(context.SubscriptionPlans) { }

    protected override Guid GetId(SubscriptionPlan entity) => entity.Id;

    public async Task<IReadOnlyList<SubscriptionPlan>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<SubscriptionPlan>.Empty).SortBy(p => p.Price).ToListAsync(ct);

    public Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        FindTrackedAsync(ById(id), ct);
}

/// <summary>Assinaturas na colecao "clinic_subscriptions" (referenciam clinicId e planId).</summary>
public class MongoClinicSubscriptionRepository : MongoRepositoryBase<ClinicSubscription>, IClinicSubscriptionRepository
{
    public MongoClinicSubscriptionRepository(MongoContext context) : base(context.ClinicSubscriptions) { }

    protected override Guid GetId(ClinicSubscription entity) => entity.Id;

    public async Task<IReadOnlyList<ClinicSubscription>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<ClinicSubscription>.Empty).ToListAsync(ct);

    public Task<ClinicSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        FindTrackedAsync(ById(id), ct);

    public async Task<IReadOnlyList<ClinicSubscription>> ListByPlanAsync(Guid planId, CancellationToken ct = default) =>
        await Collection.Find(s => s.PlanId == planId).ToListAsync(ct);

    public async Task<IReadOnlyList<ClinicSubscription>> ListByClinicAsync(Guid clinicId, CancellationToken ct = default) =>
        await Collection.Find(s => s.ClinicId == clinicId).ToListAsync(ct);
}

/// <summary>Usuarios da API (login JWT) na colecao "users".</summary>
public class MongoUserRepository : MongoRepositoryBase<User>, IUserRepository
{
    public MongoUserRepository(MongoContext context) : base(context.Users) { }

    protected override Guid GetId(User entity) => entity.Id;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        FindTrackedAsync(ById(id), ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = User.NormalizeEmail(email);
        return await Collection.Find(u => u.Email == normalized).FirstOrDefaultAsync(ct);
    }
}
