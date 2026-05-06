using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static OxalisApi.CommonBusiness.BarkClass;

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
        public static BarkRequest HandlePush(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var repo = root.GetProperty("repository").GetProperty("name").GetString();
                var refStr = root.GetProperty("ref").GetString();
                var branch = refStr?.Split('/').Last();
                var user = root.GetProperty("pusher").GetProperty("name").GetString();
                var commits = root.GetProperty("commits");
                string commitText = "无提交信息";
                if (commits.GetArrayLength() > 0)
                {
                    var first = commits[0];
                    var msg = first.GetProperty("message").GetString();
                    var author = first.GetProperty("author").GetProperty("name").GetString();
                    commitText = $"• {author}: {msg}";
                }
                var title = $"{repo} ({branch})";
                return new BarkRequest
                {
                    Title = title,
                    Body = commitText,
                    Group = "gitee",
                    Icon = "https://gitee.com/favicon.ico",
                    IsArchive = 1
                };
            }
            catch (Exception ex)
            {
                return new BarkRequest { Title = "❌ 解析失败", Body = ex.Message };
            }
        }
    }
}
