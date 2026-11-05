using AuroraPet.Payments.Domain.Entities;

namespace Aurora.Tests.Unit.Fixtures;

/// <summary>
/// Contexto compartilhado entre os testes: fabrica entidades validas para que cada
/// teste precise montar apenas o que e relevante para o seu cenario.
/// </summary>
public class PlanFixture
{
    public Clinic ClinicaVidaAnimal() =>
        new("Clinica Vida Animal", "12.345.678/0001-90", "contato@vidaanimal.com.br", "uid-clinica-vida-animal");

    public Clinic ClinicaPatasFelizes() =>
        new("Clinica Patas Felizes", "98765432000155", "financeiro@patasfelizes.com.br", "uid-clinica-patas-felizes");

    public SubscriptionPlan PlanoBasico() =>
        new(PlanTier.Basic, "Aurora Basico", 29.90m, "price_basic_123");

    public SubscriptionPlan PlanoPremium() =>
        new(PlanTier.Premium, "Aurora Premium", 59.90m, "price_premium_456");

    public ClinicSubscription AssinaturaDe(Clinic clinica, SubscriptionPlan plano) =>
        new(clinica.Id, plano.Id, "cus_123", "sub_123");
}

/// <summary>Collection fixture: compartilha a PlanFixture entre varias classes de teste.</summary>
[CollectionDefinition(PlanCollection.Name)]
public class PlanCollection : ICollectionFixture<PlanFixture>
{
    public const string Name = "Dominio de assinaturas";
}
