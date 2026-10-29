using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class PrivateNachweisablage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve test-era bytes instead of a destructive DROP. The runtime model never maps this legacy column.
            migrationBuilder.AlterColumn<byte[]>(name: "Inhalt",table: "Leistungsnachweise",type: "bytea",nullable: true,oldClrType: typeof(byte[]),oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Leistungsnachweise",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Keine automatische destruktive Rückmigration von Nachweisen. Separaten geprüften Rückfall verwenden.");
        }
    }
}
