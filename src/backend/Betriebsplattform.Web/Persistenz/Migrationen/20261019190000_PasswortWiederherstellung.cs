using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Betriebsplattform.Web.Persistenz.Migrationen;

public partial class PasswortWiederherstellung : Migration
{
    protected override void Up(MigrationBuilder b)
    {
        b.AddColumn<string>(name:"WiederherstellungHash",table:"Benutzer",type:"character varying(64)",maxLength:64,nullable:true);
        b.AddColumn<DateTimeOffset>(name:"WiederherstellungBis",table:"Benutzer",type:"timestamp with time zone",nullable:true);
    }
    protected override void Down(MigrationBuilder b)
    {
        b.DropColumn(name:"WiederherstellungHash",table:"Benutzer");
        b.DropColumn(name:"WiederherstellungBis",table:"Benutzer");
    }
}
