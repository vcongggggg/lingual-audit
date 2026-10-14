using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.Learning.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    source_language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "en"),
                    target_language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "vi"),
                    level = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "published"),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 518, DateTimeKind.Utc).AddTicks(5971)),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 518, DateTimeKind.Utc).AddTicks(6430))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vocabulary_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    term = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    normalized_term = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    source_language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "en"),
                    target_language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "vi"),
                    ipa = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    part_of_speech = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    meaning = table.Column<string>(type: "text", nullable: false),
                    pronunciation_audio_url = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    examples = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "published"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 522, DateTimeKind.Utc).AddTicks(3690)),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 522, DateTimeKind.Utc).AddTicks(4681))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vocabulary_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    topic = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "published")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_units_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srs_cards",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vocabulary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repetitions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    interval_days = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    ease_factor = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 2.50m),
                    next_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lapses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_history = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 520, DateTimeKind.Utc).AddTicks(640)),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 520, DateTimeKind.Utc).AddTicks(915))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_srs_cards", x => new { x.user_id, x.vocabulary_id });
                    table.ForeignKey(
                        name: "fk_srs_cards_vocabulary_items_vocabulary_id",
                        column: x => x.vocabulary_id,
                        principalTable: "vocabulary_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    estimated_minutes = table.Column<short>(type: "smallint", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "published"),
                    vocabulary_ids = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 519, DateTimeKind.Utc).AddTicks(332)),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 14, 46, 22, 519, DateTimeKind.Utc).AddTicks(651))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.ForeignKey(
                        name: "fk_lessons_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_progress",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "not_started"),
                    first_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    best_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    session_history = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_progress", x => new { x.user_id, x.lesson_id });
                    table.ForeignKey(
                        name: "fk_lesson_progress_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_courses_code",
                table: "courses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lesson_progress_lesson_id",
                table: "lesson_progress",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_unit_id_code",
                table: "lessons",
                columns: new[] { "unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lessons_unit_id_sort_order",
                table: "lessons",
                columns: new[] { "unit_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_srs_cards_user_id_next_due_at",
                table: "srs_cards",
                columns: new[] { "user_id", "next_due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_srs_cards_vocabulary_id",
                table: "srs_cards",
                column: "vocabulary_id");

            migrationBuilder.CreateIndex(
                name: "ix_units_course_id_code",
                table: "units",
                columns: new[] { "course_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_units_course_id_sort_order",
                table: "units",
                columns: new[] { "course_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vocabulary_items_normalized_term_source_language_target_lan",
                table: "vocabulary_items",
                columns: new[] { "normalized_term", "source_language", "target_language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lesson_progress");

            migrationBuilder.DropTable(
                name: "srs_cards");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "vocabulary_items");

            migrationBuilder.DropTable(
                name: "units");

            migrationBuilder.DropTable(
                name: "courses");
        }
    }
}
