using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Payments.Application.Services;

/// <summary>Casos de uso de CRUD sobre as assinaturas das clinicas.</summary>
public interface IClinicSubscriptionService
{
    Task<IReadOnlyList<ClinicSubscriptionResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ClinicSubscriptionResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ClinicSubscriptionResponse> CreateAsync(ClinicSubscriptionRequest request, CancellationToken ct = default);
    Task<ClinicSubscriptionResponse> UpdateAsync(Guid id, ClinicSubscriptionUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class ClinicSubscriptionService : IClinicSubscriptionService
{
    private readonly IClinicSubscriptionRepository _subscriptions;
    private readonly IClinicRepository _clinics;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly ILogger<ClinicSubscriptionService> _logger;

    public ClinicSubscriptionService(
        IClinicSubscriptionRepository subscriptions,
        IClinicRepository clinics,
        ISubscriptionPlanRepository plans,
        ILogger<ClinicSubscriptionService> logger)
    {
        _subscriptions = subscriptions;
        _clinics = clinics;
        _plans = plans;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ClinicSubscriptionResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var subscriptions = await _subscriptions.ListAsync(ct);
        return subscriptions.Select(ClinicSubscriptionResponse.FromEntity).ToList();
    }

    public async Task<ClinicSubscriptionResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Assinatura {id} nao encontrada.");

        return ClinicSubscriptionResponse.FromEntity(subscription);
    }

    public async Task<ClinicSubscriptionResponse> CreateAsync(ClinicSubscriptionRequest request, CancellationToken ct = default)
    {
        // As FKs do Oracle ja garantem, mas validar aqui devolve 404 em vez de 500.
        var clinic = await _clinics.GetByIdAsync(request.ClinicId, ct)
            ?? throw new ResourceNotFoundException($"Clinica {request.ClinicId} nao encontrada.");

        _ = await _plans.GetByIdAsync(request.PlanId, ct)
            ?? throw new ResourceNotFoundException($"Plano {request.PlanId} nao encontrado.");

        if (!clinic.Active)
            throw new BusinessRuleException($"A clinica {clinic.Id} esta desativada e nao pode contratar planos.");

        var subscription = new ClinicSubscription(
            request.ClinicId, request.PlanId, request.StripeCustomerId, request.StripeSubscriptionId);

        await _subscriptions.AddAsync(subscription, ct);
        await _subscriptions.SaveChangesAsync(ct);

        _logger.LogInformation("Assinatura {SubscriptionId} criada para a clinica {ClinicId} no plano {PlanId}",
            subscription.Id, subscription.ClinicId, subscription.PlanId);

        return ClinicSubscriptionResponse.FromEntity(subscription);
    }

    public async Task<ClinicSubscriptionResponse> UpdateAsync(Guid id, ClinicSubscriptionUpdateRequest request, CancellationToken ct = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Assinatura {id} nao encontrada.");

        if (request.PlanId != subscription.PlanId)
        {
            _ = await _plans.GetByIdAsync(request.PlanId, ct)
                ?? throw new ResourceNotFoundException($"Plano {request.PlanId} nao encontrado.");

            subscription.ChangePlan(request.PlanId);
        }

        try
        {
            subscription.UpdateStatus(request.Status, request.CurrentPeriodEnd);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        await _subscriptions.SaveChangesAsync(ct);

        _logger.LogInformation("Assinatura {SubscriptionId} atualizada para o status {Status}", subscription.Id, subscription.Status);
        return ClinicSubscriptionResponse.FromEntity(subscription);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Assinatura {id} nao encontrada.");

        await _subscriptions.RemoveAsync(subscription, ct);
        await _subscriptions.SaveChangesAsync(ct);

        _logger.LogWarning("Assinatura {SubscriptionId} excluida", id);
    }
}
