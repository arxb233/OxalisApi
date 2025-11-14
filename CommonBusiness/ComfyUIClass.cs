using Microsoft.Identity.Client;
using Quartz.Util;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        private readonly LazyConcurrentDictionary<string, object> WaitDict;
        private readonly Func<string?, Task> funcMsg;

        private readonly string ComfyUiUrl;
        private readonly string client_id;
        private readonly JsonVar PromptJsonVar;
        private readonly KeepAlive _keep;
        private readonly TimeSpan _WaitTime;

        public Task Task => taskWithTimeout.Task;

        public ComfyUIClass(string ComfyUiUrl, string client_id, string PromptJson, TimeSpan WaitTime, Func<string?, Task> funcMsg)
        {
            _keep = new KeepAlive(1, Keep);
            webClientAsync = new WebClientAsync(NetBufferSize.Default, true);
            webClientAsync.SetReceived(Receive);
            webClientAsync.SetCompleted(async (a, b, c) =>
            {
                if (b == EnClient.Close)
                {
                    var prompt = await GetPrompt();
                    if (prompt.Item2 == 0) { taskWithTimeout?.TrySetResult(); }
                }
                if (b == EnClient.Reconnect)
                {
                    Ext.Info("websocket已重连");
                }
            });
            this.ComfyUiUrl = ComfyUiUrl;
            this.client_id = client_id;
            PromptJsonVar = PromptJson.JsonVar();
            WaitDict = [];
            taskWithTimeout = new TaskWithTimeout(WaitTime);
            this.funcMsg = funcMsg;
            _WaitTime = WaitTime;
        }
        private async Task Keep()
        {
            if (WaitDict.TryGetValue("MessageTitle", out var dictTitleKeep) && WaitDict.TryGetValue("Message", out var dictKeep))
            {
                await funcMsg(dictTitleKeep.ToString() + Environment.NewLine + dictKeep.ToString());
            }
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
            await using (wsmsg)
            {
                var Wsmsgutf = wsmsg.GetString().JsonVar();
                if (Wsmsgutf.TryGet(out var Type, "type"))
                {
                    switch (Type.ToString())
                    {
                        case "execution_start":
                        case "execution_cached":

                            if (Wsmsgutf.TryGet(out var StartTime, "data", "timestamp"))
                            {
                                WaitDict["MessageTitle"] = $"任务已开始,时间:{CreateTime(StartTime)?.StartTime:yy-MM-dd HH:mm:ss}";
                            }
                            break;
                        case "progress_state":
                            if (Wsmsgutf.TryGet(out var progress_state, "data", "nodes"))
                            {
                                double percent = PromptJsonVar.Count == 0 ? 0 : (double)progress_state.Count / PromptJsonVar.Count * 100;
                                WaitDict["MessageTitle"] = $"任务进度:{percent:F2}%,耗时:{CreateTime()?.ElapsedTime:hh\\:mm\\:ss}";
                            }
                            break;
                        case "progress":
                            if (Wsmsgutf.TryGet(out var progress, "data"))
                            {
                                if (progress.TryGet(out var value, "value") && progress.TryGet(out var max, "max")
                                    && progress.TryGet(out var node, "node") && PromptJsonVar.TryGet(out var NodeTitle, node.ToString(), "_meta", "title"))
                                {
                                    var info = 1;
                                    if (WaitDict.TryGetValue(node.ToString(), out var dict)) { info = Convert.ToInt32(dict.ToString()); }
                                    WaitDict["Message"] = $"{NodeTitle}#{info}-进度:#{value}-#{max}";
                                    if (value.ToString() == max.ToString()) { WaitDict[node.ToString()] = info + 1; }
                                }
                            }
                            break;
                        case "TyDev-Utils.ExecutionTime.execution_end":
                        case "execution_success":
                            if (Wsmsgutf.TryGet(out _, "data", "timestamp"))
                            {
                                WaitDict["Message"] = $"任务已完成,时间:{CreateTime()?.Now:yy-MM-dd HH:mm:ss},当前耗时{CreateTime()?.ElapsedTime:HH:mm:ss}";
                            }
                            taskWithTimeout.TrySetResult();
                            break;
                        default:
                            break;
                    }
                    //Ext.Info(Wsmsgutf.GetJson());
                }
            }
        }
        private TimestampedClock CreateTime(long time = 0)
        {
            if (WaitDict.TryGetValue("StartTime", out var StartTime) && StartTime is TimestampedClock StartTimeclock) { return StartTimeclock; }
            var NewTime = new TimestampedClock(time is 0 ? DateTime.Now.Microsecond : time);
            WaitDict["StartTime"] = NewTime;
            return NewTime;
        }
        public async Task Websocket()
        {
            await webClientAsync.ConnectAsync($"{ComfyUiUrl}/ws?clientId={client_id}");
            if (!SpinWait.SpinUntil(() => webClientAsync.Connected, _WaitTime))
            {
                throw new Exception("任务连接等待超时");
            }
        }
        public void Dispose()
        {
            webClientAsync.Dispose();
            _keep.Close();
            ((IDisposable)Task).Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
