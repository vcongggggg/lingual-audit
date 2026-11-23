using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.AITutor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCreditQuotaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "ai_scenarios",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: new DateTime(2026, 11, 23, 3, 43, 37, 999, DateTimeKind.Utc).AddTicks(1925),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 47, 19, 916, DateTimeKind.Utc).AddTicks(5729));

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_scenarios",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 11, 23, 3, 43, 37, 999, DateTimeKind.Utc).AddTicks(1420),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 47, 19, 916, DateTimeKind.Utc).AddTicks(5490));

            migrationBuilder.CreateTable(
                name: "ai_credit_wallets",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    daily_free_remaining = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    last_reset_date = table.Column<DateOnly>(type: "date", nullable: false),
                    daily_bonus_earned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_credit_wallets", x => x.user_id);
                    table.CheckConstraint("ck_ai_credit_wallets_balance_non_negative", "balance >= 0");
                    table.CheckConstraint("ck_ai_credit_wallets_daily_bonus_non_negative", "daily_bonus_earned >= 0");
                    table.CheckConstraint("ck_ai_credit_wallets_daily_free_non_negative", "daily_free_remaining >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ai_credit_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_credit_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_credit_transactions_ai_credit_wallets_user_id",
                        column: x => x.user_id,
                        principalTable: "ai_credit_wallets",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_credit_transactions_idempotency_key",
                table: "ai_credit_transactions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_credit_transactions_user_id_created_at",
                table: "ai_credit_transactions",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_credit_transactions");

            migrationBuilder.DropTable(
                name: "ai_credit_wallets");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "ai_scenarios",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: new DateTime(2026, 10, 14, 14, 47, 19, 916, DateTimeKind.Utc).AddTicks(5729),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValue: new DateTime(2026, 11, 23, 3, 43, 37, 999, DateTimeKind.Utc).AddTicks(1925));

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_scenarios",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 10, 14, 14, 47, 19, 916, DateTimeKind.Utc).AddTicks(5490),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 11, 23, 3, 43, 37, 999, DateTimeKind.Utc).AddTicks(1420));
        }
    }
}
