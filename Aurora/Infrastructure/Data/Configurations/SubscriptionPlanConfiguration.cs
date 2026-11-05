using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraPet.Infrastructure.Data.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    /// <summary>Ids fixos para que o seed seja idempotente entre execucoes das migrations.</summary>
    public static readonly Guid PlanoBasicoId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PlanoPremiumId = new("22222222-2222-2222-2222-222222222222");

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("TB_SUBSCRIPTION_PLAN", t =>
            t.HasComment("Catalogo de planos de assinatura do Aurora, espelhando os precos cadastrados na Stripe."));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("ID")
            .ValueGeneratedNever()
            .HasComment("Chave primaria do plano (GUID gerado pela aplicacao).");

        builder.Property(p => p.Tier)
            .HasColumnName("TIER")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasComment("Faixa do plano: Basic ou Premium.");

        builder.Property(p => p.Name)
            .HasColumnName("NAME")
            .HasMaxLength(120)
            .IsRequired()
            .HasComment("Nome comercial do plano exibido na contratacao.");

        builder.Property(p => p.Price)
            .HasColumnName("PRICE")
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Preco mensal do plano em reais.");

        builder.Property(p => p.StripePriceId)
            .HasColumnName("STRIPE_PRICE_ID")
            .HasMaxLength(120)
            .IsRequired()
            .HasComment("Identificador do Price correspondente na Stripe.");

        // NUMBER(1) em vez de BOOLEAN: o tipo BOOLEAN nativo so existe no Oracle 23ai.
        // O provider Oracle ja converte bool <-> NUMBER(1); um value converter explicito aqui
        // quebra a geracao do INSERT de seed (InsertData) das migrations.
        builder.Property(p => p.Active)
            .HasColumnName("ACTIVE")
            .HasColumnType("NUMBER(1)")
            .IsRequired()
            .HasComment("1 = plano disponivel para novas assinaturas, 0 = descontinuado.");

        builder.HasIndex(p => p.StripePriceId)
            .IsUnique()
            .HasDatabaseName("UX_PLAN_STRIPE_PRICE");

        builder.HasData(
            new
            {
                Id = PlanoBasicoId,
                Tier = PlanTier.Basic,
                Name = "Aurora Basico",
                Price = 29.90m,
                StripePriceId = "price_aurora_basico",
                Active = true
            },
            new
            {
                Id = PlanoPremiumId,
                Tier = PlanTier.Premium,
                Name = "Aurora Premium",
                Price = 59.90m,
                StripePriceId = "price_aurora_premium",
                Active = true
            });
    }
}
