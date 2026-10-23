using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.AITutor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAITutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    system_prompt_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "published"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 14, 47, 19, 916, DateTimeKind.Utc).AddTicks(5490)),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 14, 47, 19, 916, DateTimeKind.Utc).AddTicks(5729))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_scenarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "active"),
                    messages = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    model_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    total_tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_conversations_ai_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "ai_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversations_scenario_id",
                table: "ai_conversations",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversations_user_id_started_at",
                table: "ai_conversations",
                columns: new[] { "user_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_scenarios_code",
                table: "ai_scenarios",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_scenarios_level_status",
                table: "ai_scenarios",
                columns: new[] { "level", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_conversations");

            migrationBuilder.DropTable(
                name: "ai_scenarios");
        }
    }
}
