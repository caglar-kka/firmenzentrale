using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen;

public partial class MitarbeiterErstzugang : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name:"Eingerichtet",table:"Benutzer",type:"boolean",nullable:false,defaultValue:true);
        migrationBuilder.AddColumn<string>(name:"EinladungHash",table:"Benutzer",type:"character varying(64)",maxLength:64,nullable:true);
        migrationBuilder.AddColumn<DateTimeOffset>(name:"EinladungBis",table:"Benutzer",type:"timestamp with time zone",nullable:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Pending accounts must remain unable to log in if the old application is restored.
        migrationBuilder.Sql("UPDATE \"Benutzer\" SET \"Aktiv\" = FALSE WHERE NOT \"Eingerichtet\";");
        migrationBuilder.DropColumn(name:"Eingerichtet",table:"Benutzer");
        migrationBuilder.DropColumn(name:"EinladungHash",table:"Benutzer");
        migrationBuilder.DropColumn(name:"EinladungBis",table:"Benutzer");
    }
}
