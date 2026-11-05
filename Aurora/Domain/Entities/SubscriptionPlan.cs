using System;

namespace AuroraPet.Payments.Domain.Entities;

public enum PlanTier
{
    Basic,
    Premium
}

public class SubscriptionPlan
{
    public Guid Id { get; private set; }
    public PlanTier Tier { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string StripePriceId { get; private set; } = string.Empty;
    public bool Active { get; private set; }

    private SubscriptionPlan() { } // EF Core

    public SubscriptionPlan(PlanTier tier, string name, decimal price, string stripePriceId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do plano e obrigatorio.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "O preco do plano nao pode ser negativo.");
        if (string.IsNullOrWhiteSpace(stripePriceId))
            throw new ArgumentException("O price id da Stripe e obrigatorio.", nameof(stripePriceId));

        Id = Guid.NewGuid();
        Tier = tier;
        Name = name;
        Price = price;
        StripePriceId = stripePriceId;
        Active = true;
    }

    public void Update(PlanTier tier, string name, decimal price, string stripePriceId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do plano e obrigatorio.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "O preco do plano nao pode ser negativo.");
        if (string.IsNullOrWhiteSpace(stripePriceId))
            throw new ArgumentException("O price id da Stripe e obrigatorio.", nameof(stripePriceId));

        Tier = tier;
        Name = name;
        Price = price;
        StripePriceId = stripePriceId;
    }

    public void Activate() => Active = true;

    public void Deactivate() => Active = false;
}
