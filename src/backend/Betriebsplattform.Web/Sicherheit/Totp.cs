using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Betriebsplattform.Web.Sicherheit;

// RFC 4226 dynamic truncation / RFC 6238: SHA-1, 30 seconds, six decimal digits.
public static class Totp
{
    public static string Code(byte[] key,long period,int digits=6)
    {
        if(period<0||digits is not (6 or 8))throw new ArgumentOutOfRangeException(nameof(period));
        Span<byte> counter=stackalloc byte[8];BinaryPrimitives.WriteInt64BigEndian(counter,period);
        var mac=HMACSHA1.HashData(key,counter);var offset=mac[^1]&15;
        var value=BinaryPrimitives.ReadUInt32BigEndian(mac.AsSpan(offset,4))&0x7fffffff;
        return (value%(digits==6?1000000u:100000000u)).ToString(digits==6?"D6":"D8",System.Globalization.CultureInfo.InvariantCulture);
    }
    public static string Base32(byte[] key)
    {
        const string alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";var result=new System.Text.StringBuilder();int buffer=0,bits=0;
        foreach(var b in key){buffer=(buffer<<8)|b;bits+=8;while(bits>=5){bits-=5;result.Append(alphabet[(buffer>>bits)&31]);}}
        if(bits>0)result.Append(alphabet[(buffer<<(5-bits))&31]);return result.ToString();
    }
}
