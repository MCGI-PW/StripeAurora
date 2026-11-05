using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuroraPet.Infrastructure.Data.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly AuroraDbContext _db;

    public SubscriptionPlanRepository(AuroraDbContext db) => _db = db;

    public async Task<IReadOnlyList<SubscriptionPlan>> ListAsync(CancellationToken ct = default) =>
        await _db.SubscriptionPlans.AsNoTracking().OrderBy(p => p.Tier).ToListAsync(ct);

    public Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default) =>
        await _db.SubscriptionPlans.AddAsync(plan, ct);

    public Task RemoveAsync(SubscriptionPlan plan, CancellationToken ct = default)
    {
        _db.SubscriptionPlans.Remove(plan);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
