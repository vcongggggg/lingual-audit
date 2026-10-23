using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.Gamification.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialGamification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "duel_matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    challenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "pending"),
                    question_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)5),
                    question_ids = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    challenger_answers = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    opponent_answers = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    challenger_score = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    opponent_score = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    challenger_correct = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    opponent_correct = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duel_matches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_streaks",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    best_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_active_date = table.Column<DateOnly>(type: "date", nullable: true),
                    freeze_balance = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    freeze_history = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    daily_activity = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_streaks", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "weekly_leaderboards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false),
                    week_end = table.Column<DateOnly>(type: "date", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xp_total = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rank = table.Column<short>(type: "smallint", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "open"),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_leaderboards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "xp_ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xp_delta = table.Column<int>(type: "integer", nullable: false),
                    awarded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xp_ledger", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_duel_matches_challenger_id_created_at",
                table: "duel_matches",
                columns: new[] { "challenger_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_duel_matches_opponent_id_created_at",
                table: "duel_matches",
                columns: new[] { "opponent_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_duel_matches_winner_id",
                table: "duel_matches",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "ix_weekly_leaderboards_week_start_rank",
                table: "weekly_leaderboards",
                columns: new[] { "week_start", "rank" });

            migrationBuilder.CreateIndex(
                name: "ix_weekly_leaderboards_week_start_user_id",
                table: "weekly_leaderboards",
                columns: new[] { "week_start", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weekly_leaderboards_week_start_xp_total",
                table: "weekly_leaderboards",
                columns: new[] { "week_start", "xp_total" });

            migrationBuilder.CreateIndex(
                name: "ix_xp_ledger_idempotency_key",
                table: "xp_ledger",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_xp_ledger_source_type_source_id",
                table: "xp_ledger",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_xp_ledger_user_id_awarded_at",
                table: "xp_ledger",
                columns: new[] { "user_id", "awarded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duel_matches");

            migrationBuilder.DropTable(
                name: "user_streaks");

            migrationBuilder.DropTable(
                name: "weekly_leaderboards");

            migrationBuilder.DropTable(
                name: "xp_ledger");
        }
    }
}
