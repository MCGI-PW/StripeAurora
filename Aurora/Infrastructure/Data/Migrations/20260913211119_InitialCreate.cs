using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_SUBSCRIPTION_PLAN",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TIER = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    PRICE = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    STRIPE_PRICE_ID = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    ACTIVE = table.Column<int>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_SUBSCRIPTION_PLAN", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TB_TUTOR_SUBSCRIPTION",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    FIREBASE_UID = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    STRIPE_CUSTOMER_ID = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    STRIPE_SUBSCRIPTION_ID = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    PLAN_ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    STATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CURRENT_PERIOD_END = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_TUTOR_SUBSCRIPTION", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SUBSCRIPTION_PLAN",
                        column: x => x.PLAN_ID,
                        principalTable: "TB_SUBSCRIPTION_PLAN",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PLAN_STRIPE_PRICE",
                table: "TB_SUBSCRIPTION_PLAN",
                column: "STRIPE_PRICE_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTION_FIREBASE_UID",
                table: "TB_TUTOR_SUBSCRIPTION",
                column: "FIREBASE_UID");

            migrationBuilder.CreateIndex(
                name: "IX_TB_TUTOR_SUBSCRIPTION_PLAN_ID",
                table: "TB_TUTOR_SUBSCRIPTION",
                column: "PLAN_ID");

            migrationBuilder.CreateIndex(
                name: "UX_SUBSCRIPTION_STRIPE_ID",
                table: "TB_TUTOR_SUBSCRIPTION",
                column: "STRIPE_SUBSCRIPTION_ID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_TUTOR_SUBSCRIPTION");

            migrationBuilder.DropTable(
                name: "TB_SUBSCRIPTION_PLAN");
        }
    }
}
