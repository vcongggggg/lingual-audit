using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledQuizRunsAndParticipations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slot_key",
                table: "clan_quiz_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_asked_at",
                table: "quiz_questions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "scheduled_quiz_runs",
                columns: table => new
                {
                    slot_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pinned_message_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    warned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_quiz_runs", x => x.slot_key);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_quiz_participations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    slot_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mezon_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    clan_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    xp_awarded = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_quiz_participations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_quiz_runs_channel_id",
                table: "scheduled_quiz_runs",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_quiz_participations_slot_key_user_id",
                table: "scheduled_quiz_participations",
                columns: new[] { "slot_key", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_quiz_participations_session_id_user_id",
                table: "scheduled_quiz_participations",
                columns: new[] { "session_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_quiz_participations");

            migrationBuilder.DropTable(
                name: "scheduled_quiz_runs");

            migrationBuilder.DropColumn(
                name: "slot_key",
                table: "clan_quiz_sessions");

            migrationBuilder.DropColumn(
                name: "last_asked_at",
                table: "quiz_questions");
        }
    }
}
