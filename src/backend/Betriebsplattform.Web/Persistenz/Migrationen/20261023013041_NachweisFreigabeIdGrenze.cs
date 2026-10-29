using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class NachweisFreigabeIdGrenze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FreigegebenDurch",
                table: "Leistungsnachweise",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FreigegebenDurch",
                table: "Leistungsnachweise",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
