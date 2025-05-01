using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QL_Lichcanhan.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Appointments");
        }
    }
}
