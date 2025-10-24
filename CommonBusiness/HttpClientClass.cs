using System.Net.Http.Headers;
using Tool;
using Tool.Utils;

namespace OxalisApi.CommonBusiness
{
    public class HttpClientClass
    {
        public static async Task<JsonVar> GetAsync(string url)
        {
            return await SendAsync(HttpMethod.Get, url, "");
        }
        public static async Task<JsonVar> PostAsync(string url, object json)
        {
            return await SendAsync(HttpMethod.Post, url, json);
        }
        public static async Task<JsonVar> SendAsync(HttpMethod HttpMethod, string url, object json)
        {
            using var request = HttpHelpers.CreateHttpRequestMessage(HttpMethod, url);
            if (HttpMethod == HttpMethod.Post) { request.Content = new StringContent(json.ToJson(), new MediaTypeHeaderValue("application/json")); }
            using var response = await HttpHelpers.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var jsons = body.JsonVar();
            return jsons;
        }
        public static async Task<Stream> StreamAsync(string url)
        {
            var requestMessage = HttpHelpers.CreateHttpRequestMessage(HttpMethod.Get, url);
            var response = await HttpHelpers.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
