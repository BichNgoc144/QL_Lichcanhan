using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QL_Lichcanhan.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "GroupParticipants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_GroupParticipants_UserId",
                table: "GroupParticipants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupParticipants_AspNetUsers_UserId",
                table: "GroupParticipants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupParticipants_AspNetUsers_UserId",
                table: "GroupParticipants");

            migrationBuilder.DropIndex(
                name: "IX_GroupParticipants_UserId",
                table: "GroupParticipants");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "GroupParticipants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
