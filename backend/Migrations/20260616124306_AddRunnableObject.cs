using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asec.Migrations
{
    /// <inheritdoc />
    public partial class AddRunnableObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DigitalObjects_Converters_ConverterId",
                table: "DigitalObjects");

            migrationBuilder.DropForeignKey(
                name: "FK_DigitalObjects_DigitalObjects_GamePackageId",
                table: "DigitalObjects");

            migrationBuilder.DropIndex(
                name: "IX_DigitalObjects_ConverterId",
                table: "DigitalObjects");

            migrationBuilder.DropIndex(
                name: "IX_DigitalObjects_GamePackageId",
                table: "DigitalObjects");

            migrationBuilder.DropColumn(
                name: "ConversionDate",
                table: "DigitalObjects");

            migrationBuilder.DropColumn(
                name: "ConverterId",
                table: "DigitalObjects");

            migrationBuilder.DropColumn(
                name: "IsDiskImage",
                table: "DigitalObjects");

            migrationBuilder.RenameColumn(
                name: "GamePackage_ObjectId",
                table: "DigitalObjects",
                newName: "PlayableObject_ObjectId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "DigitalObjects",
                newName: "PlayableObjectId");

            migrationBuilder.RenameColumn(
                name: "GamePackageId",
                table: "DigitalObjects",
                newName: "CreationDate");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Environments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_PlayableObjectId",
                table: "DigitalObjects",
                column: "PlayableObjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalObjects_DigitalObjects_PlayableObjectId",
                table: "DigitalObjects",
                column: "PlayableObjectId",
                principalTable: "DigitalObjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DigitalObjects_DigitalObjects_PlayableObjectId",
                table: "DigitalObjects");

            migrationBuilder.DropIndex(
                name: "IX_DigitalObjects_PlayableObjectId",
                table: "DigitalObjects");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Environments");

            migrationBuilder.RenameColumn(
                name: "PlayableObject_ObjectId",
                table: "DigitalObjects",
                newName: "GamePackage_ObjectId");

            migrationBuilder.RenameColumn(
                name: "PlayableObjectId",
                table: "DigitalObjects",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CreationDate",
                table: "DigitalObjects",
                newName: "GamePackageId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConversionDate",
                table: "DigitalObjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConverterId",
                table: "DigitalObjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDiskImage",
                table: "DigitalObjects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_ConverterId",
                table: "DigitalObjects",
                column: "ConverterId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_GamePackageId",
                table: "DigitalObjects",
                column: "GamePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalObjects_Converters_ConverterId",
                table: "DigitalObjects",
                column: "ConverterId",
                principalTable: "Converters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalObjects_DigitalObjects_GamePackageId",
                table: "DigitalObjects",
                column: "GamePackageId",
                principalTable: "DigitalObjects",
                principalColumn: "Id");
        }
    }
}
