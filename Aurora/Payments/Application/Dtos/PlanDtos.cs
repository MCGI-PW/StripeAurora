using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Payments.Application.Dtos;

/// <summary>Payload de criacao de uma clinica.</summary>
public record ClinicRequest(string Name, string Cnpj, string Email, string FirebaseUid);

/// <summary>Payload de alteracao de uma clinica (o uid do Firebase e imutavel).</summary>
public record ClinicUpdateRequest(string Name, string Cnpj, string Email, bool Active);

/// <summary>Representacao de leitura de uma clinica.</summary>
public record ClinicResponse(Guid Id, string Name, string Cnpj, string Email, string FirebaseUid, bool Active)
{
    public static ClinicResponse FromEntity(Clinic clinic) =>
        new(clinic.Id, clinic.Name, clinic.Cnpj, clinic.Email, clinic.FirebaseUid, clinic.Active);
}

/// <summary>Payload de criacao/alteracao de um plano de assinatura.</summary>
public record SubscriptionPlanRequest(PlanTier Tier, string Name, decimal Price, string StripePriceId);

/// <summary>Representacao de leitura de um plano de assinatura.</summary>
public record SubscriptionPlanResponse(Guid Id, PlanTier Tier, string Name, decimal Price, string StripePriceId, bool Active)
{
    public static SubscriptionPlanResponse FromEntity(SubscriptionPlan plan) =>
        new(plan.Id, plan.Tier, plan.Name, plan.Price, plan.StripePriceId, plan.Active);
}

/// <summary>Payload de criacao de uma assinatura de clinica.</summary>
public record ClinicSubscriptionRequest(Guid ClinicId, Guid PlanId, string StripeCustomerId, string StripeSubscriptionId);

/// <summary>Payload de alteracao de status/vigencia/plano de uma assinatura.</summary>
public record ClinicSubscriptionUpdateRequest(SubscriptionStatus Status, DateTime CurrentPeriodEnd, Guid PlanId);

/// <summary>Representacao de leitura de uma assinatura de clinica.</summary>
public record ClinicSubscriptionResponse(
    Guid Id,
    Guid ClinicId,
    Guid PlanId,
    string StripeCustomerId,
    string StripeSubscriptionId,
    SubscriptionStatus Status,
    DateTime CurrentPeriodEnd)
{
    public static ClinicSubscriptionResponse FromEntity(ClinicSubscription subscription) =>
        new(subscription.Id, subscription.ClinicId, subscription.PlanId, subscription.StripeCustomerId,
            subscription.StripeSubscriptionId, subscription.Status, subscription.CurrentPeriodEnd);
}
