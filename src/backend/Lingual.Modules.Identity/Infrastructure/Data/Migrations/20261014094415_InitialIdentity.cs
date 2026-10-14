using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.Identity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mezon_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    preferred_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "vi-VN"),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Ho_Chi_Minh"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "learner"),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 9, 44, 14, 427, DateTimeKind.Utc).AddTicks(5998)),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 9, 44, 14, 427, DateTimeKind.Utc).AddTicks(6931)),
                    version = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("6915426e-6aef-4ffc-ba03-b9af591414a5"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "learner_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_goal = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    daily_commitment_minutes = table.Column<short>(type: "smallint", nullable: true),
                    proficiency_level = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    placement_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "pending"),
                    onboarding_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 9, 44, 14, 423, DateTimeKind.Utc).AddTicks(7913)),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 9, 44, 14, 424, DateTimeKind.Utc).AddTicks(501)),
                    version = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("7fe91903-926d-4a91-af7c-8433234cb522"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learner_profiles", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_learner_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "placement_tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "in_progress"),
                    question_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)10),
                    correct_count = table.Column<short>(type: "smallint", nullable: true),
                    assessed_level = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    answers_detail = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2026, 10, 14, 9, 44, 14, 426, DateTimeKind.Utc).AddTicks(3381)),
                    update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValue: new DateTime(2026, 10, 14, 9, 44, 14, 426, DateTimeKind.Utc).AddTicks(5925)),
                    version = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("cdf8f7c7-e44d-42f2-9636-16b69c9ecdc4"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_placement_tests", x => x.id);
                    table.ForeignKey(
                        name: "fk_placement_tests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_placement_tests_user_id_started_at",
                table: "placement_tests",
                columns: new[] { "user_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_users_mezon_user_id",
                table: "users",
                column: "mezon_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learner_profiles");

            migrationBuilder.DropTable(
                name: "placement_tests");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
