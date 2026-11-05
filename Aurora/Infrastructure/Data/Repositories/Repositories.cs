using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuroraPet.Infrastructure.Data.Repositories;

public class ClinicRepository : IClinicRepository
{
    private readonly AuroraDbContext _db;

    public ClinicRepository(AuroraDbContext db) => _db = db;

    public async Task<IReadOnlyList<Clinic>> ListAsync(CancellationToken ct = default) =>
        await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Clinic?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Clinics.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Clinic?> GetByCnpjAsync(string cnpj, CancellationToken ct = default) =>
        _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Cnpj == cnpj, ct);

    public async Task AddAsync(Clinic clinic, CancellationToken ct = default) =>
        await _db.Clinics.AddAsync(clinic, ct);

    public Task RemoveAsync(Clinic clinic, CancellationToken ct = default)
    {
        _db.Clinics.Remove(clinic);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

public class ClinicSubscriptionRepository : IClinicSubscriptionRepository
{
    private readonly AuroraDbContext _db;

    public ClinicSubscriptionRepository(AuroraDbContext db) => _db = db;

    public async Task<IReadOnlyList<ClinicSubscription>> ListAsync(CancellationToken ct = default) =>
        await _db.ClinicSubscriptions.AsNoTracking().ToListAsync(ct);

    public Task<ClinicSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.ClinicSubscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<ClinicSubscription>> ListByPlanAsync(Guid planId, CancellationToken ct = default) =>
        await _db.ClinicSubscriptions.AsNoTracking().Where(s => s.PlanId == planId).ToListAsync(ct);

    public async Task<IReadOnlyList<ClinicSubscription>> ListByClinicAsync(Guid clinicId, CancellationToken ct = default) =>
        await _db.ClinicSubscriptions.AsNoTracking().Where(s => s.ClinicId == clinicId).ToListAsync(ct);

    public async Task AddAsync(ClinicSubscription subscription, CancellationToken ct = default) =>
        await _db.ClinicSubscriptions.AddAsync(subscription, ct);

    public Task RemoveAsync(ClinicSubscription subscription, CancellationToken ct = default)
    {
        _db.ClinicSubscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
