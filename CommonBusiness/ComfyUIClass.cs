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
using Tool.Utils.Data;
using Tool.Utils.TaskHelper;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            WaitDict.Add("StartTime", new TimestampedClock(DateTime.Now.Microsecond));
            taskWithTimeout = new TaskWithTimeout(TimeSpan.FromHours(1));
            this.funcMsg = funcMsg;
        }

        public async Task<(bool, string)> Prompt()
        {
            try
            {
                var response = await HttpClientClass.PostAsync($"http://{ComfyUiUrl}/prompt", new { client_id, prompt = PromptJsonVar.Data });
                if (response.TryGet(out var message, "error", "message")) { return (false, message); }
                return (true, "任务运行成功！");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public async Task<(bool, int)> GetPrompt()
        {
            try
            {
                var response = await HttpClientClass.GetAsync($"http://{ComfyUiUrl}/prompt");
                if (response.TryGet(out var message, "exec_info", "queue_remaining")) { return (true, message); }
                return (false, -1);
            }
            catch
            {
                return (false, -1);
            }
        }

        private async ValueTask Receive(ReceiveBytes<WebSocket> wsmsg)
        {
            using (wsmsg)
            {
                var Wsmsgutf = wsmsg.GetString().JsonVar();
                if (Wsmsgutf.TryGet(out var Type, "type"))
                {
                    switch (Type.ToString())
                    {
                        case "execution_start":

                            if (Wsmsgutf.TryGet(out var StartTime, "data", "timestamp"))
                            {
                                var _StartTime = WaitDict["StartTime"] as TimestampedClock;
                                await funcMsg($"任务已开始,时间:{_StartTime?.StartTime:yy-MM-dd HH:mm:ss}");
                            }
                            break;
                        case "progress_state":
                            if (Wsmsgutf.TryGet(out var progress_state, "data", "nodes"))
                            {
                                double percent = PromptJsonVar.Count == 0 ? 0 : (double)progress_state.Count / PromptJsonVar.Count * 100;
                                var _StartTime = WaitDict["StartTime"] as TimestampedClock;
                                await funcMsg($"任务进度:{percent:F2}%,耗时:{_StartTime?.ElapsedTime:hh\\:mm\\:ss}");
                            }
                            break;
                        case "progress":
                            if (Wsmsgutf.TryGet(out var progress, "data"))
                            {
                                if (progress.TryGet(out var value, "value") && progress.TryGet(out var max, "max")
                                    && progress.TryGet(out var node, "node") && PromptJsonVar.TryGet(out var NodeTitle, node.ToString(), "_meta", "title"))
                                {
                                    await funcMsg($"{NodeTitle}-进度:#{value}-#{max}");
                                }
                            }
                            break;
                        case "execution_success":
                            if (Wsmsgutf.TryGet(out var FinishTime, "data", "timestamp"))
                            {
                                var Time = WaitDict["StartTime"] as TimestampedClock;
                                await funcMsg($"任务已完成,时间:{Time?.Now:yy-MM-dd HH:mm:ss},当前耗时{Time?.ElapsedTime:HH:mm:ss}");
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

        public void Dispose()
        {
            webClientAsync.Dispose();
            ((IDisposable)Task).Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
