using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shikayat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportanceAndSender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "Complaints",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "ComplaintLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ComplaintLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintLogs_SenderId",
                table: "ComplaintLogs",
                column: "SenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComplaintLogs_AspNetUsers_SenderId",
                table: "ComplaintLogs",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComplaintLogs_AspNetUsers_SenderId",
                table: "ComplaintLogs");

            migrationBuilder.DropIndex(
                name: "IX_ComplaintLogs_SenderId",
                table: "ComplaintLogs");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ComplaintLogs");

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "ComplaintLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
