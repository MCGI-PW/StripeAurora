using System;

namespace AuroraPet.Payments.Domain.Entities;

public enum SubscriptionStatus
{
    Incomplete,
    Active,
    PastDue,
    Canceled
}

public class TutorSubscription
{
    public Guid Id { get; private set; }
    public string FirebaseUid { get; private set; } = string.Empty;
    public string StripeCustomerId { get; private set; } = string.Empty;
    public string StripeSubscriptionId { get; private set; } = string.Empty;
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }

    private TutorSubscription() { }

    public TutorSubscription(string firebaseUid, string stripeCustomerId, string stripeSubscriptionId, Guid planId)
    {
        Id = Guid.NewGuid();
        FirebaseUid = firebaseUid;
        StripeCustomerId = stripeCustomerId;
        StripeSubscriptionId = stripeSubscriptionId;
        PlanId = planId;
        Status = SubscriptionStatus.Incomplete;
    }

    public void UpdateStatus(SubscriptionStatus status, DateTime currentPeriodEnd)
    {
        Status = status;
        CurrentPeriodEnd = currentPeriodEnd;
    }
}
