using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asec.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Converters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Environment = table.Column<string>(type: "TEXT", nullable: true),
                    SupportedArtefactTypes = table.Column<string>(type: "TEXT", nullable: true),
                    Configuration = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Converters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalizationTools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Environment = table.Column<string>(type: "TEXT", nullable: true),
                    PhysicalMedia = table.Column<string>(type: "TEXT", nullable: true),
                    Hash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalizationTools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Emulators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Homepage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emulators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    InternalNote = table.Column<string>(type: "TEXT", nullable: true),
                    FilledOutBy = table.Column<string>(type: "TEXT", nullable: true),
                    PhysicalObjectType = table.Column<string>(type: "TEXT", nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    EAN = table.Column<string>(type: "TEXT", nullable: true),
                    ISBN = table.Column<string>(type: "TEXT", nullable: true),
                    Condition = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    Owner = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalObjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    MediaTypes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Works",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    TypeOfWork = table.Column<string>(type: "TEXT", nullable: true),
                    CuratorialDescription = table.Column<string>(type: "TEXT", nullable: true),
                    InternalNote = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Works", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Environments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmulatorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EmulatorVersion = table.Column<string>(type: "TEXT", nullable: true),
                    EaasId = table.Column<string>(type: "TEXT", nullable: true),
                    InternetConnected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Environments_Emulators_EmulatorId",
                        column: x => x.EmulatorId,
                        principalTable: "Emulators",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmulatorPlatform",
                columns: table => new
                {
                    EmulatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlatformsName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmulatorPlatform", x => new { x.EmulatorId, x.PlatformsName });
                    table.ForeignKey(
                        name: "FK_EmulatorPlatform_Emulators_EmulatorId",
                        column: x => x.EmulatorId,
                        principalTable: "Emulators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmulatorPlatform_Platforms_PlatformsName",
                        column: x => x.PlatformsName,
                        principalTable: "Platforms",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: true),
                    System = table.Column<string>(type: "TEXT", nullable: true),
                    CopyProtection = table.Column<string>(type: "TEXT", nullable: true),
                    CuratorialDescription = table.Column<string>(type: "TEXT", nullable: true),
                    InternalNote = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    WorkId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkVersions_Works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "Works",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConverterEmulationEnvironment",
                columns: table => new
                {
                    ConvertersId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmulationEnvironmentId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConverterEmulationEnvironment", x => new { x.ConvertersId, x.EmulationEnvironmentId });
                    table.ForeignKey(
                        name: "FK_ConverterEmulationEnvironment_Converters_ConvertersId",
                        column: x => x.ConvertersId,
                        principalTable: "Converters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConverterEmulationEnvironment_Environments_EmulationEnvironmentId",
                        column: x => x.EmulationEnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DigitalObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    InternalNote = table.Column<string>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DigitalObjectType = table.Column<string>(type: "TEXT", nullable: true),
                    Format = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Quality = table.Column<string>(type: "TEXT", nullable: true),
                    FedoraUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PhysicalObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    GamePackageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ArchivationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: true),
                    DigitalizationToolId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PhysicalMediaType = table.Column<int>(type: "INTEGER", nullable: true),
                    GamePackage_ObjectId = table.Column<string>(type: "TEXT", nullable: true),
                    IsDiskImage = table.Column<bool>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    VersionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EnvironmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ConverterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ConversionDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalObjects_Converters_ConverterId",
                        column: x => x.ConverterId,
                        principalTable: "Converters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DigitalObjects_DigitalObjects_GamePackageId",
                        column: x => x.GamePackageId,
                        principalTable: "DigitalObjects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DigitalObjects_DigitalizationTools_DigitalizationToolId",
                        column: x => x.DigitalizationToolId,
                        principalTable: "DigitalizationTools",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DigitalObjects_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DigitalObjects_PhysicalObjects_PhysicalObjectId",
                        column: x => x.PhysicalObjectId,
                        principalTable: "PhysicalObjects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DigitalObjects_WorkVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "WorkVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhysicalObjectWorkVersion",
                columns: table => new
                {
                    PhysicalObjectsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkVersionsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalObjectWorkVersion", x => new { x.PhysicalObjectsId, x.WorkVersionsId });
                    table.ForeignKey(
                        name: "FK_PhysicalObjectWorkVersion_PhysicalObjects_PhysicalObjectsId",
                        column: x => x.PhysicalObjectsId,
                        principalTable: "PhysicalObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhysicalObjectWorkVersion_WorkVersions_WorkVersionsId",
                        column: x => x.WorkVersionsId,
                        principalTable: "WorkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DigitalObjectWorkVersion",
                columns: table => new
                {
                    DigitalObjectsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkVersionsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalObjectWorkVersion", x => new { x.DigitalObjectsId, x.WorkVersionsId });
                    table.ForeignKey(
                        name: "FK_DigitalObjectWorkVersion_DigitalObjects_DigitalObjectsId",
                        column: x => x.DigitalObjectsId,
                        principalTable: "DigitalObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DigitalObjectWorkVersion_WorkVersions_WorkVersionsId",
                        column: x => x.WorkVersionsId,
                        principalTable: "WorkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Paratexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    Language = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    InternalNote = table.Column<string>(type: "TEXT", nullable: true),
                    FilledOutBy = table.Column<string>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    EmissionSize = table.Column<uint>(type: "INTEGER", nullable: false),
                    IdentificationNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ParatextType = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CanExport = table.Column<bool>(type: "INTEGER", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DigitalObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PhysicalObjectId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paratexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paratexts_DigitalObjects_DigitalObjectId",
                        column: x => x.DigitalObjectId,
                        principalTable: "DigitalObjects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Paratexts_PhysicalObjects_PhysicalObjectId",
                        column: x => x.PhysicalObjectId,
                        principalTable: "PhysicalObjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParatextWorkVersion",
                columns: table => new
                {
                    ParatextsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkVersionsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParatextWorkVersion", x => new { x.ParatextsId, x.WorkVersionsId });
                    table.ForeignKey(
                        name: "FK_ParatextWorkVersion_Paratexts_ParatextsId",
                        column: x => x.ParatextsId,
                        principalTable: "Paratexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParatextWorkVersion_WorkVersions_WorkVersionsId",
                        column: x => x.WorkVersionsId,
                        principalTable: "WorkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConverterEmulationEnvironment_EmulationEnvironmentId",
                table: "ConverterEmulationEnvironment",
                column: "EmulationEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalizationTools_Hash",
                table: "DigitalizationTools",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_ConverterId",
                table: "DigitalObjects",
                column: "ConverterId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_DigitalizationToolId",
                table: "DigitalObjects",
                column: "DigitalizationToolId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_EnvironmentId",
                table: "DigitalObjects",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_GamePackageId",
                table: "DigitalObjects",
                column: "GamePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_PhysicalObjectId",
                table: "DigitalObjects",
                column: "PhysicalObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjects_VersionId",
                table: "DigitalObjects",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalObjectWorkVersion_WorkVersionsId",
                table: "DigitalObjectWorkVersion",
                column: "WorkVersionsId");

            migrationBuilder.CreateIndex(
                name: "IX_EmulatorPlatform_PlatformsName",
                table: "EmulatorPlatform",
                column: "PlatformsName");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_EmulatorId",
                table: "Environments",
                column: "EmulatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Paratexts_DigitalObjectId",
                table: "Paratexts",
                column: "DigitalObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Paratexts_PhysicalObjectId",
                table: "Paratexts",
                column: "PhysicalObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ParatextWorkVersion_WorkVersionsId",
                table: "ParatextWorkVersion",
                column: "WorkVersionsId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalObjectWorkVersion_WorkVersionsId",
                table: "PhysicalObjectWorkVersion",
                column: "WorkVersionsId");

            migrationBuilder.CreateIndex(
                name: "IX_Works_RemoteId",
                table: "Works",
                column: "RemoteId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkVersions_RemoteId",
                table: "WorkVersions",
                column: "RemoteId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkVersions_WorkId",
                table: "WorkVersions",
                column: "WorkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConverterEmulationEnvironment");

            migrationBuilder.DropTable(
                name: "DigitalObjectWorkVersion");

            migrationBuilder.DropTable(
                name: "EmulatorPlatform");

            migrationBuilder.DropTable(
                name: "ParatextWorkVersion");

            migrationBuilder.DropTable(
                name: "PhysicalObjectWorkVersion");

            migrationBuilder.DropTable(
                name: "Platforms");

            migrationBuilder.DropTable(
                name: "Paratexts");

            migrationBuilder.DropTable(
                name: "DigitalObjects");

            migrationBuilder.DropTable(
                name: "Converters");

            migrationBuilder.DropTable(
                name: "DigitalizationTools");

            migrationBuilder.DropTable(
                name: "Environments");

            migrationBuilder.DropTable(
                name: "PhysicalObjects");

            migrationBuilder.DropTable(
                name: "WorkVersions");

            migrationBuilder.DropTable(
                name: "Emulators");

            migrationBuilder.DropTable(
                name: "Works");
        }
    }
}
