using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class Leistungskatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ObjektId",
                table: "Leistungsaktionen",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateTable(
                name: "Leistungskatalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Beschreibung = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Kategorie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reihenfolge = table.Column<int>(type: "integer", nullable: false),
                    Aktiv = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leistungskatalog", x => x.Id);
                    table.CheckConstraint("Leistungskatalog_Version", "\"Version\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leistungskatalog_Reihenfolge_Name",
                table: "Leistungskatalog",
                columns: new[] { "Reihenfolge", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Leistungskatalog und Audit enthalten Nutzerdaten; kein automatischer destruktiver Rückbau.");
        }
    }
}
