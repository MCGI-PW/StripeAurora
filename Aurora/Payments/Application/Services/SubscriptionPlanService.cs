using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Payments.Application.Services;

/// <summary>Lancada quando o recurso solicitado nao existe. Traduzida em 404 pelo controller.</summary>
public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}

/// <summary>Lancada quando a operacao viola uma regra de negocio. Traduzida em 409 pelo controller.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Casos de uso de CRUD sobre os planos de assinatura.</summary>
public interface ISubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanResponse>> GetAllAsync(CancellationToken ct = default);
    Task<SubscriptionPlanResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SubscriptionPlanResponse> CreateAsync(SubscriptionPlanRequest request, CancellationToken ct = default);
    Task<SubscriptionPlanResponse> UpdateAsync(Guid id, SubscriptionPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _plans;
    private readonly IClinicSubscriptionRepository _subscriptions;
    private readonly ILogger<SubscriptionPlanService> _logger;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository plans,
        IClinicSubscriptionRepository subscriptions,
        ILogger<SubscriptionPlanService> logger)
    {
        _plans = plans;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SubscriptionPlanResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var plans = await _plans.ListAsync(ct);
        return plans.Select(SubscriptionPlanResponse.FromEntity).ToList();
    }

    public async Task<SubscriptionPlanResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Plano {id} nao encontrado.");

        return SubscriptionPlanResponse.FromEntity(plan);
    }

    public async Task<SubscriptionPlanResponse> CreateAsync(SubscriptionPlanRequest request, CancellationToken ct = default)
    {
        var plan = new SubscriptionPlan(request.Tier, request.Name, request.Price, request.StripePriceId);

        await _plans.AddAsync(plan, ct);
        await _plans.SaveChangesAsync(ct);

        _logger.LogInformation("Plano {PlanId} criado no tier {Tier} por {Price:C}", plan.Id, plan.Tier, plan.Price);
        return SubscriptionPlanResponse.FromEntity(plan);
    }

    public async Task<SubscriptionPlanResponse> UpdateAsync(Guid id, SubscriptionPlanRequest request, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Plano {id} nao encontrado.");

        plan.Update(request.Tier, request.Name, request.Price, request.StripePriceId);
        await _plans.SaveChangesAsync(ct);

        _logger.LogInformation("Plano {PlanId} atualizado para {Name} por {Price:C}", plan.Id, plan.Name, plan.Price);
        return SubscriptionPlanResponse.FromEntity(plan);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Plano {id} nao encontrado.");

        var vinculadas = await _subscriptions.ListByPlanAsync(id, ct);
        if (vinculadas.Count > 0)
            throw new BusinessRuleException($"O plano {id} possui {vinculadas.Count} assinatura(s) vinculada(s) e nao pode ser excluido.");

        await _plans.RemoveAsync(plan, ct);
        await _plans.SaveChangesAsync(ct);

        _logger.LogWarning("Plano {PlanId} excluido", id);
    }
}
