using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraPet.Infrastructure.Data.Configurations;

public class TutorSubscriptionConfiguration : IEntityTypeConfiguration<TutorSubscription>
{
    public void Configure(EntityTypeBuilder<TutorSubscription> builder)
    {
        builder.ToTable("TB_TUTOR_SUBSCRIPTION");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("ID")
            .ValueGeneratedNever();

        builder.Property(s => s.FirebaseUid)
            .HasColumnName("FIREBASE_UID")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(s => s.StripeCustomerId)
            .HasColumnName("STRIPE_CUSTOMER_ID")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(s => s.StripeSubscriptionId)
            .HasColumnName("STRIPE_SUBSCRIPTION_ID")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(s => s.PlanId)
            .HasColumnName("PLAN_ID")
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.CurrentPeriodEnd)
            .HasColumnName("CURRENT_PERIOD_END")
            .IsRequired();

        builder.HasIndex(s => s.FirebaseUid)
            .HasDatabaseName("IX_SUBSCRIPTION_FIREBASE_UID");

        builder.HasIndex(s => s.StripeSubscriptionId)
            .IsUnique()
            .HasDatabaseName("UX_SUBSCRIPTION_STRIPE_ID");

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .HasConstraintName("FK_SUBSCRIPTION_PLAN")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
