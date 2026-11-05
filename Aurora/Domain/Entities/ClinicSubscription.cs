using System;

namespace AuroraPet.Payments.Domain.Entities;

public enum SubscriptionStatus
{
    Incomplete,
    Active,
    PastDue,
    Canceled
}

/// <summary>
/// Assinatura de uma clinica em um plano. E a tabela associativa entre TB_CLINIC e
/// TB_SUBSCRIPTION_PLAN, e guarda o espelho da Subscription correspondente na Stripe.
/// </summary>
public class ClinicSubscription
{
    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid PlanId { get; private set; }
    public string StripeCustomerId { get; private set; } = string.Empty;
    public string StripeSubscriptionId { get; private set; } = string.Empty;
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }

    private ClinicSubscription() { } // EF Core

    public ClinicSubscription(Guid clinicId, Guid planId, string stripeCustomerId, string stripeSubscriptionId)
    {
        if (clinicId == Guid.Empty)
            throw new ArgumentException("A assinatura precisa referenciar uma clinica valida.", nameof(clinicId));
        if (planId == Guid.Empty)
            throw new ArgumentException("A assinatura precisa referenciar um plano valido.", nameof(planId));

        Id = Guid.NewGuid();
        ClinicId = clinicId;
        PlanId = planId;
        StripeCustomerId = stripeCustomerId ?? string.Empty;
        StripeSubscriptionId = stripeSubscriptionId ?? string.Empty;
        Status = SubscriptionStatus.Incomplete;
    }

    public void UpdateStatus(SubscriptionStatus status, DateTime currentPeriodEnd)
    {
        if (Status == SubscriptionStatus.Canceled)
            throw new InvalidOperationException("Uma assinatura cancelada nao pode mudar de status.");

        Status = status;
        CurrentPeriodEnd = currentPeriodEnd;
    }

    public void ChangePlan(Guid planId)
    {
        if (planId == Guid.Empty)
            throw new ArgumentException("A assinatura precisa referenciar um plano valido.", nameof(planId));

        PlanId = planId;
    }

    public void Cancel() => Status = SubscriptionStatus.Canceled;
}
