using Microsoft.Extensions.Logging;
using Quartz;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Tool;
using Tool.Utils;

namespace OxalisApi.Job
{
    public class HayFrpJob : IJob
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public HayFrpJob(IHttpClientFactory clientFactory, ILogger<HayFrpJob> logger)
        {
            _httpClient = clientFactory.CreateClient("HayFrp");
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            //{"type":"login","user":"","passwd":""} 登录
            //{"type":"sign","csrf":""} 签到
            //{"type":"csrf","csrf":""} 不掉线
            //{"type":"info","csrf":""} 获取用户信息
            List<(string, string)> result = [("nixue", "123456"), ("sign", "95Y8fe8DPSS622MOMJ2p")];
            foreach (var item in result)
            {
                var login = await LoginAsync(item.Item1, item.Item2);
                if (login["status"] == 200)
                {
                    await SignAsync(login["token"]);
                }
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        private async Task<JsonVar> LoginAsync(string user, string passwd)
        {
            return await PostAsync(new { type = "login", user, passwd });
        }

        /// <summary>
        /// 签到
        /// </summary>
        private async Task<JsonVar> SignAsync(string csrf)
        {
            return await PostAsync(new { type = "sign", csrf });
        }


        private async Task<JsonVar> PostAsync(object json)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.hayfrp.com/user"),
                Headers =
                {
                    { "waf", "off" },
                    { "Accept", "*/*" },
                    { "User-Agent", "PostmanRuntime-ApipostRuntime/1.1.0" },
                    { "Connection", "keep-alive" },
                },
                Content = new StringContent(json.ToJson(), new MediaTypeHeaderValue("application/json"))
            };
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            StringBuilder builder = new();
            var jsons = body.JsonVar();
            foreach (var item in jsons)
            {
                builder.AppendLine($"{item.Key}: {item.Current.Data}");
            }

            _logger.LogInformation("{builder}", builder.ToString());

            return jsons;
        }
    }
}

