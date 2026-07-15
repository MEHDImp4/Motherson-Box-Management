using System.Security.Cryptography;

namespace MothersonBoxManagement.Security;

public interface ICspNonceService
{
    string GetNonce(HttpContext context);
}

public sealed class CspNonceService : ICspNonceService
{
    private const string NonceKey = "CspNonce";

    public string GetNonce(HttpContext context)
    {
        if (context.Items.TryGetValue(NonceKey, out var existing) && existing is string nonce)
            return nonce;

        var newNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceKey] = newNonce;
        return newNonce;
    }
}
