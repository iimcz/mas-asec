using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asec.Migrations
{
    /// <inheritdoc />
    public partial class MinorModelUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmulatorVersion",
                table: "Environments",
                newName: "Version");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Environments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Environments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Environments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Environments");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Environments",
                newName: "EmulatorVersion");
        }
    }
}
