using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.Gamification.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameCenterTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "active_game_attempts",
                columns: table => new
                {
                    attempt_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    questions_payload_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_active_game_attempts", x => x.attempt_id);
                });

            migrationBuilder.CreateTable(
                name: "game_questions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    game_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    prompt = table.Column<string>(type: "text", nullable: false),
                    choices_json = table.Column<string>(type: "jsonb", nullable: true),
                    answer = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "A2"),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "script-derived"),
                    source_ref = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_questions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    attempt_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    accuracy = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    xp_earned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    combo_max = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_word_pools",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_text = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    translation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phonetic = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    topic = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    topic_vi = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    cefr_level = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "A1"),
                    example_sentence = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    example_translation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "ai-generated"),
                    verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_word_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_game_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    high_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_games_played = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_wins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_xp_earned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    avg_accuracy = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    last_played_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_game_stats", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_active_game_attempts_user_id",
                table: "active_game_attempts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_questions_game_type",
                table: "game_questions",
                column: "game_type");

            migrationBuilder.CreateIndex(
                name: "ix_game_questions_level",
                table: "game_questions",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_attempt_id",
                table: "game_sessions",
                column: "attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_game_type_score",
                table: "game_sessions",
                columns: new[] { "game_type", "score" });

            migrationBuilder.CreateIndex(
                name: "ix_game_sessions_user_id",
                table: "game_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_word_pools_cefr_level",
                table: "game_word_pools",
                column: "cefr_level");

            migrationBuilder.CreateIndex(
                name: "ix_game_word_pools_topic",
                table: "game_word_pools",
                column: "topic");

            migrationBuilder.CreateIndex(
                name: "ix_user_game_stats_game_type_high_score",
                table: "user_game_stats",
                columns: new[] { "game_type", "high_score" });

            migrationBuilder.CreateIndex(
                name: "ix_user_game_stats_user_id_game_type",
                table: "user_game_stats",
                columns: new[] { "user_id", "game_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "active_game_attempts");

            migrationBuilder.DropTable(
                name: "game_questions");

            migrationBuilder.DropTable(
                name: "game_sessions");

            migrationBuilder.DropTable(
                name: "game_word_pools");

            migrationBuilder.DropTable(
                name: "user_game_stats");
        }
    }
}
