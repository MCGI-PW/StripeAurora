using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Aurora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_CLINIC",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false, comment: "Chave primaria da clinica (GUID gerado pela aplicacao)."),
                    NAME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false, comment: "Razao social ou nome fantasia da clinica."),
                    CNPJ = table.Column<string>(type: "NVARCHAR2(14)", maxLength: 14, nullable: false, comment: "CNPJ da clinica, somente digitos."),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false, comment: "E-mail administrativo usado na cobranca."),
                    FIREBASE_UID = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false, comment: "UID da conta da clinica no Firebase Authentication."),
                    ACTIVE = table.Column<bool>(type: "NUMBER(1)", nullable: false, comment: "1 = clinica ativa na plataforma, 0 = desativada.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_CLINIC", x => x.ID);
                },
                comment: "Clinicas veterinarias cadastradas no Aurora. E a entidade que contrata os planos de assinatura.");

            migrationBuilder.CreateTable(
                name: "TB_SUBSCRIPTION_PLAN",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false, comment: "Chave primaria do plano (GUID gerado pela aplicacao)."),
                    TIER = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false, comment: "Faixa do plano: Basic ou Premium."),
                    NAME = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false, comment: "Nome comercial do plano exibido na contratacao."),
                    PRICE = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false, comment: "Preco mensal do plano em reais."),
                    STRIPE_PRICE_ID = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false, comment: "Identificador do Price correspondente na Stripe."),
                    ACTIVE = table.Column<bool>(type: "NUMBER(1)", nullable: false, comment: "1 = plano disponivel para novas assinaturas, 0 = descontinuado.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_SUBSCRIPTION_PLAN", x => x.ID);
                },
                comment: "Catalogo de planos de assinatura do Aurora, espelhando os precos cadastrados na Stripe.");

            migrationBuilder.CreateTable(
                name: "TB_CLINIC_SUBSCRIPTION",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false, comment: "Chave primaria da assinatura (GUID gerado pela aplicacao)."),
                    CLINIC_ID = table.Column<Guid>(type: "RAW(16)", nullable: false, comment: "Chave estrangeira para TB_CLINIC.ID - a clinica que contratou o plano."),
                    PLAN_ID = table.Column<Guid>(type: "RAW(16)", nullable: false, comment: "Chave estrangeira para TB_SUBSCRIPTION_PLAN.ID - o plano contratado."),
                    STRIPE_CUSTOMER_ID = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false, comment: "Identificador do Customer correspondente na Stripe."),
                    STRIPE_SUBSCRIPTION_ID = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false, comment: "Identificador da Subscription correspondente na Stripe."),
                    STATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false, comment: "Ciclo de vida da assinatura: Incomplete, Active, PastDue ou Canceled."),
                    CURRENT_PERIOD_END = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, comment: "Data/hora em que o periodo pago corrente se encerra.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_CLINIC_SUBSCRIPTION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SUBSCRIPTION_CLINIC",
                        column: x => x.CLINIC_ID,
                        principalTable: "TB_CLINIC",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SUBSCRIPTION_PLAN",
                        column: x => x.PLAN_ID,
                        principalTable: "TB_SUBSCRIPTION_PLAN",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Assinaturas contratadas pelas clinicas: associa TB_CLINIC a TB_SUBSCRIPTION_PLAN e espelha a subscription da Stripe.");

            migrationBuilder.InsertData(
                table: "TB_CLINIC",
                columns: new[] { "ID", "ACTIVE", "CNPJ", "EMAIL", "FIREBASE_UID", "NAME" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), true, "12345678000190", "contato@vidaanimal.com.br", "uid-clinica-vida-animal", "Clinica Vida Animal" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, "98765432000155", "financeiro@patasfelizes.com.br", "uid-clinica-patas-felizes", "Clinica Patas Felizes" }
                });

            migrationBuilder.InsertData(
                table: "TB_SUBSCRIPTION_PLAN",
                columns: new[] { "ID", "ACTIVE", "NAME", "PRICE", "STRIPE_PRICE_ID", "TIER" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, "Aurora Basico", 29.90m, "price_aurora_basico", "Basic" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), true, "Aurora Premium", 59.90m, "price_aurora_premium", "Premium" }
                });

            migrationBuilder.InsertData(
                table: "TB_CLINIC_SUBSCRIPTION",
                columns: new[] { "ID", "CLINIC_ID", "CURRENT_PERIOD_END", "PLAN_ID", "STATUS", "STRIPE_CUSTOMER_ID", "STRIPE_SUBSCRIPTION_ID" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222222"), "Active", "cus_aurora_vida_animal", "sub_aurora_vida_animal" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2026, 10, 31, 23, 59, 59, 0, DateTimeKind.Utc), new Guid("11111111-1111-1111-1111-111111111111"), "Incomplete", "cus_aurora_patas_felizes", "sub_aurora_patas_felizes" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_CLINIC_CNPJ",
                table: "TB_CLINIC",
                column: "CNPJ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CLINIC_FIREBASE_UID",
                table: "TB_CLINIC",
                column: "FIREBASE_UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTION_CLINIC",
                table: "TB_CLINIC_SUBSCRIPTION",
                column: "CLINIC_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TB_CLINIC_SUBSCRIPTION_PLAN_ID",
                table: "TB_CLINIC_SUBSCRIPTION",
                column: "PLAN_ID");

            migrationBuilder.CreateIndex(
                name: "UX_SUBSCRIPTION_STRIPE_ID",
                table: "TB_CLINIC_SUBSCRIPTION",
                column: "STRIPE_SUBSCRIPTION_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PLAN_STRIPE_PRICE",
                table: "TB_SUBSCRIPTION_PLAN",
                column: "STRIPE_PRICE_ID",
                unique: true);

            // O provider Oracle emite os comentarios das migrations como literais N'...',
            // que o banco da FIAP grava byte a byte, deixando o texto ilegivel em
            // USER_TAB_COMMENTS / USER_COL_COMMENTS. Reaplica tudo em ASCII puro.
            migrationBuilder.Sql("COMMENT ON TABLE TB_CLINIC IS 'Clinicas veterinarias cadastradas no Aurora. E a entidade que contrata os planos de assinatura.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC.ID IS 'Chave primaria da clinica (GUID gerado pela aplicacao).'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC.NAME IS 'Razao social ou nome fantasia da clinica.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC.CNPJ IS 'CNPJ da clinica, somente digitos.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC.EMAIL IS 'E-mail administrativo usado na cobranca.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC.FIREBASE_UID IS 'UID da conta da clinica no Firebase Authentication.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC.ACTIVE IS '1 = clinica ativa na plataforma, 0 = desativada.'");
            migrationBuilder.Sql("COMMENT ON TABLE TB_SUBSCRIPTION_PLAN IS 'Catalogo de planos de assinatura do Aurora, espelhando os precos cadastrados na Stripe.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.ID IS 'Chave primaria do plano (GUID gerado pela aplicacao).'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.TIER IS 'Faixa do plano: Basic ou Premium.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.NAME IS 'Nome comercial do plano exibido na contratacao.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.PRICE IS 'Preco mensal do plano em reais.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.STRIPE_PRICE_ID IS 'Identificador do Price correspondente na Stripe.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_SUBSCRIPTION_PLAN.ACTIVE IS '1 = plano disponivel para novas assinaturas, 0 = descontinuado.'");
            migrationBuilder.Sql("COMMENT ON TABLE TB_CLINIC_SUBSCRIPTION IS 'Assinaturas contratadas pelas clinicas: associa TB_CLINIC a TB_SUBSCRIPTION_PLAN e espelha a subscription da Stripe.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.ID IS 'Chave primaria da assinatura (GUID gerado pela aplicacao).'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.CLINIC_ID IS 'Chave estrangeira para TB_CLINIC.ID - a clinica que contratou o plano.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.PLAN_ID IS 'Chave estrangeira para TB_SUBSCRIPTION_PLAN.ID - o plano contratado.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.STRIPE_CUSTOMER_ID IS 'Identificador do Customer correspondente na Stripe.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.STRIPE_SUBSCRIPTION_ID IS 'Identificador da Subscription correspondente na Stripe.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.STATUS IS 'Ciclo de vida da assinatura: Incomplete, Active, PastDue ou Canceled.'");
            migrationBuilder.Sql("COMMENT ON COLUMN TB_CLINIC_SUBSCRIPTION.CURRENT_PERIOD_END IS 'Data/hora em que o periodo pago corrente se encerra.'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_CLINIC_SUBSCRIPTION");

            migrationBuilder.DropTable(
                name: "TB_CLINIC");

            migrationBuilder.DropTable(
                name: "TB_SUBSCRIPTION_PLAN");
        }
    }
}
