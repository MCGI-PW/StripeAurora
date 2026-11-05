using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraPet.Infrastructure.Data.Configurations;

public class ClinicSubscriptionConfiguration : IEntityTypeConfiguration<ClinicSubscription>
{
    private static readonly Guid AssinaturaAtivaId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid AssinaturaIncompletaId = new("44444444-4444-4444-4444-444444444444");

    public void Configure(EntityTypeBuilder<ClinicSubscription> builder)
    {
        builder.ToTable("TB_CLINIC_SUBSCRIPTION", t =>
            t.HasComment("Assinaturas contratadas pelas clinicas: associa TB_CLINIC a TB_SUBSCRIPTION_PLAN e espelha a subscription da Stripe."));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("ID")
            .ValueGeneratedNever()
            .HasComment("Chave primaria da assinatura (GUID gerado pela aplicacao).");

        builder.Property(s => s.ClinicId)
            .HasColumnName("CLINIC_ID")
            .IsRequired()
            .HasComment("Chave estrangeira para TB_CLINIC.ID - a clinica que contratou o plano.");

        builder.Property(s => s.PlanId)
            .HasColumnName("PLAN_ID")
            .IsRequired()
            .HasComment("Chave estrangeira para TB_SUBSCRIPTION_PLAN.ID - o plano contratado.");

        builder.Property(s => s.StripeCustomerId)
            .HasColumnName("STRIPE_CUSTOMER_ID")
            .HasMaxLength(120)
            .IsRequired()
            .HasComment("Identificador do Customer correspondente na Stripe.");

        builder.Property(s => s.StripeSubscriptionId)
            .HasColumnName("STRIPE_SUBSCRIPTION_ID")
            .HasMaxLength(120)
            .IsRequired()
            .HasComment("Identificador da Subscription correspondente na Stripe.");

        builder.Property(s => s.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasComment("Ciclo de vida da assinatura: Incomplete, Active, PastDue ou Canceled.");

        builder.Property(s => s.CurrentPeriodEnd)
            .HasColumnName("CURRENT_PERIOD_END")
            .IsRequired()
            .HasComment("Data/hora em que o periodo pago corrente se encerra.");

        builder.HasIndex(s => s.ClinicId)
            .HasDatabaseName("IX_SUBSCRIPTION_CLINIC");

        builder.HasIndex(s => s.StripeSubscriptionId)
            .IsUnique()
            .HasDatabaseName("UX_SUBSCRIPTION_STRIPE_ID");

        builder.HasOne<Clinic>()
            .WithMany()
            .HasForeignKey(s => s.ClinicId)
            .HasConstraintName("FK_SUBSCRIPTION_CLINIC")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .HasConstraintName("FK_SUBSCRIPTION_PLAN")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new
            {
                Id = AssinaturaAtivaId,
                ClinicId = ClinicConfiguration.ClinicaVidaAnimalId,
                PlanId = SubscriptionPlanConfiguration.PlanoPremiumId,
                StripeCustomerId = "cus_aurora_vida_animal",
                StripeSubscriptionId = "sub_aurora_vida_animal",
                Status = SubscriptionStatus.Active,
                CurrentPeriodEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            },
            new
            {
                Id = AssinaturaIncompletaId,
                ClinicId = ClinicConfiguration.ClinicaPatasFelizesId,
                PlanId = SubscriptionPlanConfiguration.PlanoBasicoId,
                StripeCustomerId = "cus_aurora_patas_felizes",
                StripeSubscriptionId = "sub_aurora_patas_felizes",
                Status = SubscriptionStatus.Incomplete,
                CurrentPeriodEnd = new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc)
            });
    }
}
