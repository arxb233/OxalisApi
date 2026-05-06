using Microsoft.AspNetCore.DataProtection;
using OxalisApi.CommonBusiness;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tool;
using Tool.Web.Api;

namespace OxalisApi.Controllers.Bark
{
    public class Bark : MinApi
    {
        private const string GiteeToken = "d017f6c2-a107-4337-8837-c25d70bd99da";
        private const string BarkServer = "http://192.168.20.22:8421/";
        private const string BarkToken = "sTT6ntci7uDc9nYqw2mTmU";
        [Ashx(State = AshxState.Post)]
        public async Task<IApiOut> GiteeWebHook(HttpContext context)
        {
            var token = context.Request.Headers["X-Gitee-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(token) || token != GiteeToken)
            {
                return new JsonOut(new { message = $"签名验证失败" }) { StatusCode = 500 };
            }
            var eventType = context.Request.Headers["X-Gitee-Event"].FirstOrDefault();
            string body;
            using (var reader = new StreamReader(context.Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }
            if (eventType == "push_hooks")
            {
                var Barkre = GiteeClass.HandlePush(body);
                var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                await HttpClientClass.PostAsync($"{BarkServer}{BarkToken}", Barkre, options);
            }
            return ApiOut.Json(new { message = $"成功" });
        }
    }
}
