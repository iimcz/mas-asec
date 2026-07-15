using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asec.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDigitalObjectModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Paratexts_DigitalObjects_DigitalObjectId",
                table: "Paratexts");

            migrationBuilder.DropForeignKey(
                name: "FK_Paratexts_PhysicalObjects_PhysicalObjectId",
                table: "Paratexts");

            migrationBuilder.DropIndex(
                name: "IX_Paratexts_DigitalObjectId",
                table: "Paratexts");

            migrationBuilder.DropIndex(
                name: "IX_Paratexts_PhysicalObjectId",
                table: "Paratexts");

            migrationBuilder.DropColumn(
                name: "DigitalObjectId",
                table: "Paratexts");

            migrationBuilder.DropColumn(
                name: "PhysicalObjectId",
                table: "Paratexts");

            migrationBuilder.RenameColumn(
                name: "WebsiteUrl",
                table: "DigitalObjects",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "Quality",
                table: "DigitalObjects",
                newName: "RepoUrl");

            migrationBuilder.RenameColumn(
                name: "FedoraUrl",
                table: "DigitalObjects",
                newName: "MediaInfoReport");

            migrationBuilder.AlterColumn<int>(
                name: "DigitalObjectType",
                table: "DigitalObjects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DigitalObjectParatext",
                columns: table => new
                {
                    DigitalObjectsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParatextsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalObjectParatext", x => new { x.DigitalObjectsId, x.ParatextsId });
                    table.ForeignKey(
                        name: "FK_DigitalObjectParatext_DigitalObjects_DigitalObjectsId",
                        column: x => x.DigitalObjectsId,
                        principalTable: "DigitalObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DigitalObjectParatext_Paratexts_ParatextsId",
                        column: x => x.ParatextsId,
                        principalTable: "Paratexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParatextPhysicalObject",
                columns: table => new
                {
                    ParatextsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PhysicalObjectsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParatextPhysicalObject", x => new { x.ParatextsId, x.PhysicalObjectsId });
                    table.ForeignKey(
                        name: "FK_ParatextPhysicalObject_Paratexts_ParatextsId",
                        column: x => x.ParatextsId,
                        principalTable: "Paratexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParatextPhysicalObject_PhysicalObjects_PhysicalObjectsId",
                        column: x => x.PhysicalObjectsId,
                        principalTable: "PhysicalObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjectParatext_ParatextsId",
                table: "DigitalObjectParatext",
                column: "ParatextsId");

            migrationBuilder.CreateIndex(
                name: "IX_ParatextPhysicalObject_PhysicalObjectsId",
                table: "ParatextPhysicalObject",
                column: "PhysicalObjectsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigitalObjectParatext");

            migrationBuilder.DropTable(
                name: "ParatextPhysicalObject");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "DigitalObjects",
                newName: "WebsiteUrl");

            migrationBuilder.RenameColumn(
                name: "RepoUrl",
                table: "DigitalObjects",
                newName: "Quality");

            migrationBuilder.RenameColumn(
                name: "MediaInfoReport",
                table: "DigitalObjects",
                newName: "FedoraUrl");

            migrationBuilder.AddColumn<Guid>(
                name: "DigitalObjectId",
                table: "Paratexts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PhysicalObjectId",
                table: "Paratexts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DigitalObjectType",
                table: "DigitalObjects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Paratexts_DigitalObjectId",
                table: "Paratexts",
                column: "DigitalObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Paratexts_PhysicalObjectId",
                table: "Paratexts",
                column: "PhysicalObjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Paratexts_DigitalObjects_DigitalObjectId",
                table: "Paratexts",
                column: "DigitalObjectId",
                principalTable: "DigitalObjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Paratexts_PhysicalObjects_PhysicalObjectId",
                table: "Paratexts",
                column: "PhysicalObjectId",
                principalTable: "PhysicalObjects",
                principalColumn: "Id");
        }
    }
}
