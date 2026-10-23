using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.MezonBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMezonBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bot_configuration",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    mezon_clan_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    clan_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    default_channel_mezon_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "active"),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Ho_Chi_Minh"),
                    installed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    installed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    uninstalled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    daily_word_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    quiz_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    quiz_duration_seconds = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)30),
                    schedules = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bot_configuration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clan_moderator_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clan_moderator_grants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quizzes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    quiz_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    level = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "published"),
                    time_limit_seconds = table.Column<int>(type: "integer", nullable: true),
                    total_questions = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 14, 46, 59, 201, DateTimeKind.Utc).AddTicks(522)),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 14, 46, 59, 201, DateTimeKind.Utc).AddTicks(792))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quizzes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quiz_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quiz_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "in_progress"),
                    score = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false, defaultValue: 0m),
                    max_score = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false, defaultValue: 0m),
                    correct_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    answers_detail = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_quiz_attempts_quizzes_quiz_id",
                        column: x => x.quiz_id,
                        principalTable: "quizzes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quiz_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quiz_id = table.Column<Guid>(type: "uuid", nullable: true),
                    question_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    prompt = table.Column<string>(type: "text", nullable: false),
                    prompt_audio_url = table.Column<string>(type: "text", nullable: true),
                    difficulty = table.Column<short>(type: "smallint", nullable: true),
                    level = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    options = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    explanation = table.Column<string>(type: "text", nullable: true),
                    points = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "published"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 14, 46, 59, 201, DateTimeKind.Utc).AddTicks(6969)),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 14, 46, 59, 201, DateTimeKind.Utc).AddTicks(7472))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quiz_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_quiz_questions_quizzes_quiz_id",
                        column: x => x.quiz_id,
                        principalTable: "quizzes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clan_quiz_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    channel_mezon_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "open"),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    closes_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winning_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winning_response_ms = table.Column<int>(type: "integer", nullable: true),
                    explanation_published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responses = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clan_quiz_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_clan_quiz_sessions_quiz_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "quiz_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bot_configuration_mezon_clan_id",
                table: "bot_configuration",
                column: "mezon_clan_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clan_moderator_grants_user_id",
                table: "clan_moderator_grants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_clan_quiz_sessions_channel_mezon_id_opened_at",
                table: "clan_quiz_sessions",
                columns: new[] { "channel_mezon_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_clan_quiz_sessions_question_id",
                table: "clan_quiz_sessions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_quiz_attempts_quiz_id_status",
                table: "quiz_attempts",
                columns: new[] { "quiz_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_quiz_attempts_user_id_started_at",
                table: "quiz_attempts",
                columns: new[] { "user_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_quiz_questions_quiz_id_sort_order",
                table: "quiz_questions",
                columns: new[] { "quiz_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_quiz_questions_status_level_question_type",
                table: "quiz_questions",
                columns: new[] { "status", "level", "question_type" });

            migrationBuilder.CreateIndex(
                name: "ix_quizzes_code",
                table: "quizzes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quizzes_quiz_type_level_status",
                table: "quizzes",
                columns: new[] { "quiz_type", "level", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_configuration");

            migrationBuilder.DropTable(
                name: "clan_moderator_grants");

            migrationBuilder.DropTable(
                name: "clan_quiz_sessions");

            migrationBuilder.DropTable(
                name: "quiz_attempts");

            migrationBuilder.DropTable(
                name: "quiz_questions");

            migrationBuilder.DropTable(
                name: "quizzes");
        }
    }
}
