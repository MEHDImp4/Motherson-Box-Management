using System.Security.Cryptography;
using System.Text;

namespace MothersonBoxManagement.Printing;

public static class PrintAgentSecurity
{
    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string CreateToken(int bytes = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

    public static string CreatePairingCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(12);
        return string.Create(12, bytes, (span, state) =>
        {
            for (var index = 0; index < span.Length; index++)
                span[index] = alphabet[state[index] % alphabet.Length];
        });
    }
}
