using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmDB.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Character");

            migrationBuilder.DropTable(
                name: "Film");

            migrationBuilder.DropTable(
                name: "Film_Genre");

            migrationBuilder.DropTable(
                name: "Film_Person");

            migrationBuilder.DropTable(
                name: "Film_Person_Character");

            migrationBuilder.DropTable(
                name: "Genre");

            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropTable(
                name: "Person_Job_Summary");

            migrationBuilder.DropTable(
                name: "Person");
        }
    }
}
