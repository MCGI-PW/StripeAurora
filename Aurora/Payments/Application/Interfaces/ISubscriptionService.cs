namespace AuroraPet.Payments.Application.Interfaces;

public interface ISubscriptionService
{
    Task<(string subscriptionId, string clientSecret)> CreateSubscriptionAsync(
        string firebaseUid, string customerEmail, string planTier, CancellationToken ct = default);

    Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken ct = default);
}