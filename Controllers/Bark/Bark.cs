using Microsoft.AspNetCore.DataProtection;
using OxalisApi.CommonBusiness;
using System.Security.Cryptography;
using System.Text;
using Tool.Web.Api;

namespace OxalisApi.Controllers.Bark
{
    public class Bark : MinApi
    {
        private const string Token = "d017f6c2-a107-4337-8837-c25d70bd99da";
        [Ashx(State = AshxState.Post)]
        public async Task<IApiOut> GiteeWebHook(HttpContext context)
        {
            var token = context.Request.Headers["X-Gitee-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(token) || token != Token)
            {
                return new JsonOut(new { message = $"签名验证失败" }) { StatusCode = 500 };
            }
            var eventType = context.Request.Headers["X-Gitee-Event"].FirstOrDefault();
            string body;
            using (var reader = new StreamReader(context.Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }
            if (eventType == "push_hooks") { GiteeClass.HandlePush(body); }
            return ApiOut.Json(new { message = $"成功" });
        }
    }
}
