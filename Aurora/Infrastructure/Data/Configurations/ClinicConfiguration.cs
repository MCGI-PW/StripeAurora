using AuroraPet.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraPet.Infrastructure.Data.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    /// <summary>Ids fixos para que a carga inicial seja idempotente entre execucoes das migrations.</summary>
    public static readonly Guid ClinicaVidaAnimalId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ClinicaPatasFelizesId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("TB_CLINIC", t =>
            t.HasComment("Clinicas veterinarias cadastradas no Aurora. E a entidade que contrata os planos de assinatura."));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("ID")
            .ValueGeneratedNever()
            .HasComment("Chave primaria da clinica (GUID gerado pela aplicacao).");

        builder.Property(c => c.Name)
            .HasColumnName("NAME")
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Razao social ou nome fantasia da clinica.");

        builder.Property(c => c.Cnpj)
            .HasColumnName("CNPJ")
            .HasMaxLength(14)
            .IsRequired()
            .HasComment("CNPJ da clinica, somente digitos.");

        builder.Property(c => c.Email)
            .HasColumnName("EMAIL")
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("E-mail administrativo usado na cobranca.");

        builder.Property(c => c.FirebaseUid)
            .HasColumnName("FIREBASE_UID")
            .HasMaxLength(128)
            .IsRequired()
            .HasComment("UID da conta da clinica no Firebase Authentication.");

        builder.Property(c => c.Active)
            .HasColumnName("ACTIVE")
            .HasColumnType("NUMBER(1)")
            .IsRequired()
            .HasComment("1 = clinica ativa na plataforma, 0 = desativada.");

        builder.HasIndex(c => c.Cnpj)
            .IsUnique()
            .HasDatabaseName("UX_CLINIC_CNPJ");

        builder.HasIndex(c => c.FirebaseUid)
            .IsUnique()
            .HasDatabaseName("UX_CLINIC_FIREBASE_UID");

        builder.HasData(
            new
            {
                Id = ClinicaVidaAnimalId,
                Name = "Clinica Vida Animal",
                Cnpj = "12345678000190",
                Email = "contato@vidaanimal.com.br",
                FirebaseUid = "uid-clinica-vida-animal",
                Active = true
            },
            new
            {
                Id = ClinicaPatasFelizesId,
                Name = "Clinica Patas Felizes",
                Cnpj = "98765432000155",
                Email = "financeiro@patasfelizes.com.br",
                FirebaseUid = "uid-clinica-patas-felizes",
                Active = true
            });
    }
}
