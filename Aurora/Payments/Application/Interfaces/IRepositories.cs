using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Payments.Application.Interfaces;

/// <summary>Persistencia das clinicas (tabela TB_CLINIC).</summary>
public interface IClinicRepository
{
    Task<IReadOnlyList<Clinic>> ListAsync(CancellationToken ct = default);
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Clinic?> GetByCnpjAsync(string cnpj, CancellationToken ct = default);
    Task AddAsync(Clinic clinic, CancellationToken ct = default);
    Task RemoveAsync(Clinic clinic, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Persistencia dos planos de assinatura (tabela TB_SUBSCRIPTION_PLAN).</summary>
public interface ISubscriptionPlanRepository
{
    Task<IReadOnlyList<SubscriptionPlan>> ListAsync(CancellationToken ct = default);
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default);
    Task RemoveAsync(SubscriptionPlan plan, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Persistencia das assinaturas das clinicas (tabela TB_CLINIC_SUBSCRIPTION).</summary>
public interface IClinicSubscriptionRepository
{
    Task<IReadOnlyList<ClinicSubscription>> ListAsync(CancellationToken ct = default);
    Task<ClinicSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ClinicSubscription>> ListByPlanAsync(Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<ClinicSubscription>> ListByClinicAsync(Guid clinicId, CancellationToken ct = default);
    Task AddAsync(ClinicSubscription subscription, CancellationToken ct = default);
    Task RemoveAsync(ClinicSubscription subscription, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
