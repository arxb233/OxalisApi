using Microsoft.Extensions.Logging;
using Quartz;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Tool;

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
            //{"type":"sign","csrf":""} 签到
            //{"type":"csrf","csrf":""} 不掉线
            //{"type":"info","csrf":""} 获取用户信息
            List<(string, string)> result = [("sign", "nRdyYRddPljIXSaUMynd"), ("sign", "95Y8fe8DPSS622MOMJ2p")];
            foreach (var item in result) 
            {
                await PostAsync(item.Item1, item.Item2);
            }
        }

        private async Task PostAsync(string type, string csrf)
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
                Content = new StringContent($"{{\"type\":\"{type}\",\"csrf\":\"{csrf}\"}}", new MediaTypeHeaderValue("application/json"))
            };
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            StringBuilder builder = new();
            foreach (var item in body.JsonVar())
            {
                builder.AppendLine($"{item.Key}: {item.Current.Data}");
            }

            _logger.LogInformation("{builder}", builder.ToString());
        }
    }
}

