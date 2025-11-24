using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmDB.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCastToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_cast",
                table: "Job",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE Job SET is_cast = 1 WHERE title IN ('Actor', 'Self')");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_cast",
                table: "Job");
        }
    }
}
