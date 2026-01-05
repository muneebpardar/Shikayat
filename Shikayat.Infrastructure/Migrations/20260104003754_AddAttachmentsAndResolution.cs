using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shikayat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentsAndResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionAttachmentPath",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ResolutionAttachmentPath",
                table: "Complaints");
        }
    }
}

