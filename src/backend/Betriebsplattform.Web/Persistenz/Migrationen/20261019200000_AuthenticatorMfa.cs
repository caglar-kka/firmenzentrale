using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Betriebsplattform.Web.Persistenz.Migrationen;
public partial class AuthenticatorMfa : Migration
{
    protected override void Up(MigrationBuilder b)
    {
        b.AddColumn<string>(name:"MfaGeheimnis",table:"Benutzer",type:"character varying(2000)",maxLength:2000,nullable:true);
        b.AddColumn<string>(name:"MfaVorbereitung",table:"Benutzer",type:"character varying(2000)",maxLength:2000,nullable:true);
        b.AddColumn<DateTimeOffset>(name:"MfaVorbereitungBis",table:"Benutzer",type:"timestamp with time zone",nullable:true);
        b.AddColumn<string>(name:"MfaRecoveryHashes",table:"Benutzer",type:"character varying(600)",maxLength:600,nullable:true);
        b.AddColumn<long>(name:"MfaLetztePeriode",table:"Benutzer",type:"bigint",nullable:false,defaultValue:-1L);
        b.AddColumn<int>(name:"MfaFehlversuche",table:"Benutzer",type:"integer",nullable:false,defaultValue:0);
        b.AddColumn<string>(name:"MfaChallengeHash",table:"Benutzer",type:"character varying(64)",maxLength:64,nullable:true);
        b.AddColumn<DateTimeOffset>(name:"MfaChallengeBis",table:"Benutzer",type:"timestamp with time zone",nullable:true);
        b.AddColumn<string>(name:"MfaStepupHash",table:"Benutzer",type:"character varying(64)",maxLength:64,nullable:true);
        b.AddColumn<DateTimeOffset>(name:"MfaStepupBis",table:"Benutzer",type:"timestamp with time zone",nullable:true);
    }
    protected override void Down(MigrationBuilder b)
    {
        // A rollback must not silently turn MFA accounts into password-only accounts.
        b.Sql("UPDATE \"Benutzer\" SET \"Aktiv\"=FALSE, \"Sicherheitskennung\"=md5(random()::text || clock_timestamp()::text) WHERE \"MfaGeheimnis\" IS NOT NULL;");
        foreach(var name in new[]{"MfaGeheimnis","MfaVorbereitung","MfaVorbereitungBis","MfaRecoveryHashes","MfaLetztePeriode","MfaFehlversuche","MfaChallengeHash","MfaChallengeBis","MfaStepupHash","MfaStepupBis"})b.DropColumn(name:name,table:"Benutzer");
    }
}
