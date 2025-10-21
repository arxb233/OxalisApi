using Microsoft.Identity.Client;
using Quartz.Util;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Xml;
using Tool;
using Tool.Sockets.Kernels;
using Tool.Sockets.WebHelper;
using Tool.Utils;
using Tool.Utils.TaskHelper;

namespace OxalisApi.CommonBusiness
{
    public class ComfyUIClass : IDisposable
    {
        private readonly WebClientAsync webClientAsync;
        private readonly TaskWithTimeout taskWithTimeout;
        private readonly Dictionary<string, object> WaitDict;
        private readonly Func<string, Task> funcMsg;

        private readonly string ComfyUiUrl;
        private readonly string client_id;
        private readonly JsonVar PromptJsonVar;

        public Task Task => taskWithTimeout.Task;

        public ComfyUIClass(string ComfyUiUrl, string client_id, string PromptJson, Func<string, Task> funcMsg)
        {
            webClientAsync = new WebClientAsync();
            webClientAsync.SetReceived(Receive);
            this.ComfyUiUrl = ComfyUiUrl;
            this.client_id = client_id;
            PromptJsonVar = PromptJson.JsonVar();

            WaitDict = [];
            taskWithTimeout = new TaskWithTimeout(TimeSpan.FromHours(1));
            this.funcMsg = funcMsg;
        }

        public async Task<(bool, string)> Prompt()
        {
            try
            {
                var response = await PostAsync($"http://{ComfyUiUrl}/prompt", new { client_id, prompt = PromptJsonVar.Data });
                if (response.TryGet(out var message, "error", "message")) { return (false, message); }
                return (true, "任务运行成功！");
            }
            catch(Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public async Task<(bool,int)> GetPrompt()
        {
            try
            {
                var response = await GetAsync($"http://{ComfyUiUrl}/prompt");
                if (response.TryGet(out var message, "exec_info", "queue_remaining")) { return (true, message); }
                return (false, -1);
            }
            catch
            {
                return (false,-1);
            }
        }

        private async ValueTask Receive(ReceiveBytes<WebSocket> wsmsg)
        {
            using (wsmsg)
            {
                var Wsmsgutf = Encoding.UTF8.GetString(wsmsg.Span).JsonVar();
                if (Wsmsgutf.TryGet(out var Type, "type"))
                {
                    switch (Type.ToString())
                    {
                        case "execution_start":

                            if (Wsmsgutf.TryGet(out var StartTime, "data", "timestamp"))
                            {
                                var _StartTime = DateTimeExtension.ToLocalTime(StartTime, true);
                                await funcMsg($"任务已开始,时间:{_StartTime:yy-MM-dd HH:mm:ss}");
                                WaitDict.Add("StartTime", _StartTime);
                            }
                            break;
                        case "progress_state":
                            if (Wsmsgutf.TryGet(out var Data, "data", "nodes"))
                            {
                                double percent = PromptJsonVar.Count == 0 ? 0 : (double)Data.Count / PromptJsonVar.Count * 100;
                                await funcMsg($"任务完成百分比: {percent:F2}%,当前耗时{(DateTime.Now - (DateTime)WaitDict["StartTime"]):HH:mm;ss}");
                            }
                            break;
                        case "execution_success":
                            if (Wsmsgutf.TryGet(out var FinishTime, "data", "timestamp"))
                            {
                                var _FinishTime = DateTimeExtension.ToLocalTime(FinishTime, true);
                                await funcMsg($"任务已完成,时间:{_FinishTime:yy-MM-dd HH:mm:ss},当前耗时{(_FinishTime - (DateTime)WaitDict["StartTime"]):HH:mm;ss}");
                            }
                            taskWithTimeout.SetResult();
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public async Task Websocket()
        {
            await webClientAsync.ConnectAsync($"{ComfyUiUrl}/ws?clientId={client_id}&test=");
        }
        private static async Task<JsonVar> GetAsync(string url)
        {
            return await SendAsync(HttpMethod.Get, url,"");
        }
        private static async Task<JsonVar> PostAsync(string url, object json)
        {
            return await SendAsync(HttpMethod.Post, url, json);
        }
        private static async Task<JsonVar> SendAsync(HttpMethod HttpMethod, string url, object json)
        {
            using var request = HttpHelpers.CreateHttpRequestMessage(HttpMethod, url);
            if (HttpMethod == HttpMethod.Post) { request.Content = new StringContent(json.ToJson(), new MediaTypeHeaderValue("application/json")); }
            using var response = await HttpHelpers.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var jsons = body.JsonVar();
            return jsons;
        }

        public void Dispose()
        {
            //webClientAsync.Dispose();
            ((IDisposable)Task).Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
