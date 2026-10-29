using Microsoft.EntityFrameworkCore.Migrations;
namespace Betriebsplattform.Web.Persistenz.Migrationen;
public partial class Dokumentanalyse : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.AddColumn<string>(name:"AnalyseJson",table:"Dokumente",type:"character varying(500000)",maxLength:500000,nullable:true);
    protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.DropColumn(name:"AnalyseJson",table:"Dokumente");
}
