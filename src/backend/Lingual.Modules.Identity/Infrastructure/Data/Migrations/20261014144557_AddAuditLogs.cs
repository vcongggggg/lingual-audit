using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.Identity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 405, DateTimeKind.Utc).AddTicks(5709),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(8917));

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 405, DateTimeKind.Utc).AddTicks(5426),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(8684));

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "learner_profiles",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 404, DateTimeKind.Utc).AddTicks(9538),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(2408));

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "learner_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 404, DateTimeKind.Utc).AddTicks(9101),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(1991));

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_id_created_at",
                table: "audit_logs",
                columns: new[] { "actor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id_created_at",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(8917),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 405, DateTimeKind.Utc).AddTicks(5709));

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(8684),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 405, DateTimeKind.Utc).AddTicks(5426));

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "learner_profiles",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(2408),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 404, DateTimeKind.Utc).AddTicks(9538));

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "learner_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 10, 14, 14, 2, 19, 877, DateTimeKind.Utc).AddTicks(1991),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 10, 14, 14, 45, 57, 404, DateTimeKind.Utc).AddTicks(9101));
        }
    }
}
