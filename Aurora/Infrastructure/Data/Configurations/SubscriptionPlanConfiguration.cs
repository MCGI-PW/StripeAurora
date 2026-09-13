using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AuroraPet.Infrastructure.Data.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("TB_SUBSCRIPTION_PLAN");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("ID")
            .ValueGeneratedNever();

        builder.Property(p => p.Tier)
            .HasColumnName("TIER")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("NAME")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasColumnName("PRICE")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.StripePriceId)
            .HasColumnName("STRIPE_PRICE_ID")
            .HasMaxLength(120)
            .IsRequired();

        // NUMBER(1) em vez de BOOLEAN: o tipo BOOLEAN nativo so existe no Oracle 23ai
        builder.Property(p => p.Active)
            .HasColumnName("ACTIVE")
            .HasConversion(new BoolToZeroOneConverter<int>())
            .HasColumnType("NUMBER(1)")
            .IsRequired();

        builder.HasIndex(p => p.StripePriceId)
            .IsUnique()
            .HasDatabaseName("UX_PLAN_STRIPE_PRICE");
    }
}
