using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewsAssistant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiReviewAnalysisMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiAnalysisModel",
                table: "Reviews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiAnalysisProvider",
                table: "Reviews",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAnalysisModel",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "AiAnalysisProvider",
                table: "Reviews");
        }
    }
}
