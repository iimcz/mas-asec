using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asec.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantWorkVersionFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DigitalObjects_WorkVersions_VersionId",
                table: "DigitalObjects");

            migrationBuilder.DropIndex(
                name: "IX_DigitalObjects_VersionId",
                table: "DigitalObjects");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "DigitalObjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VersionId",
                table: "DigitalObjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_VersionId",
                table: "DigitalObjects",
                column: "VersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalObjects_WorkVersions_VersionId",
                table: "DigitalObjects",
                column: "VersionId",
                principalTable: "WorkVersions",
                principalColumn: "Id");
        }
    }
}
