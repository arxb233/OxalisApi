using Azure;
using Azure.Core;
using Quartz.Util;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text;
using Tool;
using Tool.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OxalisApi.CommonBusiness
{
    public class AutoDLClass
    {
        public static async Task<bool> Open(string Authorization, string instance_uuid,string payload)
        {
            if (Authorization.IsNullOrWhiteSpace() || instance_uuid.IsNullOrWhiteSpace()) { return false; }
            string url = "https://www.autodl.com/api/v1/instance/power_on";
            var body = new { instance_uuid,payload};
            var response = await PostAsync(url, Authorization, body);
            Ext.Info($"uuid: {instance_uuid}, open 机器开机");
            if (response.TryGet(out var Code, "code") && Code == "Success")
            {
                Ext.Info($"uuid: {instance_uuid}, 机器开机成功");
                return true;
            }
            return false;
        }
        public static async Task<bool> Close(string Authorization, string instance_uuid)
        {
            if (Authorization.IsNullOrWhiteSpace() || instance_uuid.IsNullOrWhiteSpace()) { return false; }
            string url = "https://www.autodl.com/api/v1/instance/power_off";
            var body = new { instance_uuid };
            var response = await PostAsync(url, Authorization, body);
            Ext.Info($"uuid: {instance_uuid},close 机器关机");
            if (response.TryGet(out var Code, "code") && Code == "Success")
            {
                Ext.Info($"uuid: {instance_uuid}, 机器关机成功");
                return true;
            }
            return false;
        }
        public static async Task<int> Check(string Authorization, string instance_uuid)
        {
            if (Authorization.IsNullOrWhiteSpace()) { return 0; }
            string url = "https://www.autodl.com/api/v1/instance";
            var body = new
            {
                date_from = "",
                date_to = "",
                page_index = 1,
                page_size = 10,
                status = Array.Empty<string>(),
                charge_type = Array.Empty<string>()
            };
            var response = await PostAsync(url, Authorization, body);
            if (response.TryGet(out var Code, "code") && Code == "Success" && response.TryGet(out var list, "data", "list") && list.Count > 0)
            {
                var matchedMessages = list.Where(x => x["uuid"].ToString() == instance_uuid).Select(x => x["gpu_idle_num"].ToString()).FirstOrDefault();
                var statusMessages = list.Where(x => x["uuid"].ToString() == instance_uuid).Select(x => x["status"].ToString()).FirstOrDefault();
                if (statusMessages == "running") { return -1; }
                return matchedMessages.IsNullOrWhiteSpace() ? 0 : Convert.ToInt32(matchedMessages);
            } 
            return 0;
        }
        public static async Task<double> Wallet(string Authorization)
        {
            if (Authorization.IsNullOrWhiteSpace()) { return default; }
            string url = "https://www.autodl.com/api/v1/wallet";
            var response = await GetAsync(url, Authorization);
            if (response.TryGet(out var Code, "code") && Code == "Success" && response.TryGet(out var assets, "data", "assets"))
            {
                var intassets = Convert.ToInt32(assets.ToString());
                return intassets / 1000.0;
            }
            return 0;
        }
        private static async Task<JsonVar> GetAsync(string url, string Authorization)
        {
            return await SendAsync(HttpMethod.Get, url, Authorization, "");
        }
        private static async Task<JsonVar> PostAsync(string url, string Authorization, object json)
        {
            return await SendAsync(HttpMethod.Post, url, Authorization, json);
        }
        private static async Task<JsonVar> SendAsync(HttpMethod HttpMethod, string url, string Authorization, object json)
        {
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod,
                RequestUri = new Uri(url),
                Headers =
                {
                    { "Authorization", Authorization },
                    { "User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36" },
                }
            };
            if (HttpMethod == HttpMethod.Post) { request.Content = new StringContent(json.ToJson(), new MediaTypeHeaderValue("application/json")); }
            using var response = await HttpHelpers.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var jsons = body.JsonVar();
            return jsons;
        }
    }
}
