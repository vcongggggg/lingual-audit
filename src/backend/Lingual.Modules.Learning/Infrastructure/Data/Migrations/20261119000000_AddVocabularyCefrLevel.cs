using Lingual.Modules.Learning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingual.Modules.Learning.Infrastructure.Data.Migrations;

[DbContext(typeof(LearningDbContext))]
[Migration("20261119000000_AddVocabularyCefrLevel")]
public partial class AddVocabularyCefrLevel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        "ALTER TABLE vocabulary_items ADD COLUMN IF NOT EXISTS cefr_level character varying(2) NOT NULL DEFAULT 'A1';");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "cefr_level", table: "vocabulary_items");
}
