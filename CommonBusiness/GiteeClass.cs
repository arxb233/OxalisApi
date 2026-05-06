using System.Security.Cryptography;
using System.Text;

namespace OxalisApi.CommonBusiness
{
    public class GiteeClass
    {
        public static bool VerifySignature(string secret, string body, string? signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computed = "sha256=" + Convert.ToHexStringLower(hash);

            return computed == signature;
        }
        public static void HandlePush(string body)
        {
        }
    }
}
