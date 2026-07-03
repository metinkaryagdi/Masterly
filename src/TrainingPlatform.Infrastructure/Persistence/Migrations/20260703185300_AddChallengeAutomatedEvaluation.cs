using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeAutomatedEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "scenario_submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "coding_submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TestsPassed",
                table: "coding_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestsTotal",
                table: "coding_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestCode",
                table: "coding_challenges",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "scenario_submissions");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "coding_submissions");

            migrationBuilder.DropColumn(
                name: "TestsPassed",
                table: "coding_submissions");

            migrationBuilder.DropColumn(
                name: "TestsTotal",
                table: "coding_submissions");

            migrationBuilder.DropColumn(
                name: "TestCode",
                table: "coding_challenges");
        }
    }
}
