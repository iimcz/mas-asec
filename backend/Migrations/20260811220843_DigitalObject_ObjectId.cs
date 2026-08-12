using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asec.Migrations
{
    /// <inheritdoc />
    public partial class DigitalObject_ObjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE DigitalObjects SET ObjectId = PlayableObject_ObjectId WHERE DigitalObjectType == 1;");

            migrationBuilder.DropColumn(
                name: "PlayableObject_ObjectId",
                table: "DigitalObjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayableObject_ObjectId",
                table: "DigitalObjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE DigitalObjects SET PlayableObject_ObjectId = ObjectId WHERE DigitalObjectType == 1;");
        }
    }
}
