using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewsAssistant.Infrastructure.Data.Migrations;

[DbContext(typeof(ReviewsDbContext))]
[Migration("20260928120000_AddAiDraftResponseMetadata")]
public partial class AddAiDraftResponseMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AiDraftResponseModel",
            table: "Reviews",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AiDraftResponseProvider",
            table: "Reviews",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AiDraftResponseModel", table: "Reviews");
        migrationBuilder.DropColumn(name: "AiDraftResponseProvider", table: "Reviews");
    }
}
