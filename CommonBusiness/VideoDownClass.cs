using Azure;
using OxalisApi.Job;
using System;
using Tool;
using Tool.Utils;

namespace OxalisApi.CommonBusiness
{
    public class VideoDownClass()
    {
        public static async Task<Stream> DownLoad(string url)
        {
            var video = await GetStreamAsync(url);
            return video;
        }
        public static async Task<Stream> GetStreamAsync(string url)
        {
            var requestMessage = HttpHelpers.CreateHttpRequestMessage(HttpMethod.Get, url);
            var response = await HttpHelpers.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            else
            {
                Ext.Info($"请求失败，状态码：{response.StatusCode}");
                return Stream.Null;
            }
        }
    }
}
