using Azure;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Quartz.Util;
using Renci.SshNet;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Tool;
using Tool.Sockets.Kernels;
using Tool.Utils.TaskHelper;
using static System.Net.Mime.MediaTypeNames;

namespace OxalisApi.CommonBusiness
{
    public partial class TgBotClass(TgBotClassRespose tb)
    {
        public TelegramBotClient _bot = new(tb.Token);
        public async Task<TelegramBotClient> Start()
        {
            var me = await _bot.GetMe();
            Ext.Info($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
            _bot.OnError += OnError;
            _bot.OnMessage += OnMessage;
            _bot.OnUpdate += OnUpdate;
            return _bot;
        }
        public async Task OnError(Exception exception, HandleErrorSource source)
        {
            await Console.Out.WriteLineAsync(exception.ToString());
        }
        public async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text != null && msg.Chat.Id == tb.ChatId && BotRegex().Match(msg.Text) is Match MatchBot && MatchBot.Success)
            {
                var detail = msg.Text.Replace(MatchBot.Value, "");
                StringBuilder stringBuilder = new(); stringBuilder.AppendLine("我已收到消息:");
                if (detail.IsNullOrEmpty()) { await _bot.SendMessage(msg.Chat.Id, $"消息内容不能为空！"); return; }
                var StartMessage = await _bot.SendMessage(msg.Chat.Id, stringBuilder.ToString());
                if (MatchUrlRegex().Match(detail) is Match MatchUrl && MatchUrl.Success)
                {
                    try
                    {
                        #region
                        await SendProcess($"任务开始执行....");
                        await SendProcess($"1.视频链接获取成功，正在下载视频，请耐心等待....");
                        using var video = await VideoDownClass.DownLoad(tb.DownApiUrl, MatchUrl.Value);
                        if (video == Stream.Null || video.Length <= 333) { await SendProcess("视频获取失败！"); return; }
                        #endregion

                        #region
                        await SendProcess($"2.视频下载成功，正在查询AutodL钱包信息....");
                        var Wallet = await AutoDLClass.Wallet(tb.Authorization);
                        if (Wallet <= 1.5) { await SendProcess($"3.AutoDL余额不足1.5元,请保证余额充足再使用！"); return; }
                        #endregion

                        #region
                        await SendProcess($"4.当前AutoDL账户余额为{Wallet}元,正在查询AutoDL设备信息....");
                        var Check = await AutoDLClass.Check(tb.Authorization, tb.Instance_uuid);
                        if (Check == -1) { await SendProcess($"当前AutoDL实列已存在,请稍后使用！"); return; }
                        if (Check == 0) { await SendProcess($"当前AutoDL实列没有可用GPU,请稍后使用！"); return; }
                        #endregion

                        #region
                        await SendProcess($"5.当前AutoDL实列可用GPU为{Check}个,正在进行开机,预计30s....");
                        var Open = await AutoDLClass.Open(tb.Authorization, tb.Instance_uuid, tb.Payload);
                        if (!Open) { await SendProcess($"当前AutoDL实列开机失败,请联系管理员！"); return; }
                        do { if (await AutoDLClass.Check(tb.Authorization, tb.Instance_uuid) == -1) { break; }; await Task.Delay(5000); } while (true);
                        #endregion

                        #region
                        await SendProcess($"6.开机成功,正在进行远程链接....");
                        using var sshHelper = new LinuxSshHelper(tb.Host, tb.Port, tb.Username, tb.Password);
                        sshHelper.Connect(); sshHelper.OpenPort(tb.ComfyUIPort);
                        #endregion

                        #region
                        await SendProcess($"7.远程链接成功,正在上传文件....");
                        sshHelper.UploadStream(video, tb.InputPath);
                        #endregion

                        #region
                        await SendProcess($"8.服务运行成功,正在获取工作流....");
                        using var PromptStream = sshHelper.DownloadStream(tb.ComfyUIPrompt);
                        #endregion

                        #region
                        await SendProcess($"9.工作流获取成功,正在启动服务并执行工作流....");
                        ComfyUIClass comfyUIClass = new($"127.0.0.1:{tb.ComfyUIPort}", tb.ComfyUIPrompt, PromptStream.ToArray().ToByteString(), (_msg) => SendProcess(_msg));
                        do { if (await comfyUIClass.GetPrompt() is (bool, int) GetPromptResult && GetPromptResult.Item1) { break; } await Task.Delay(10000); } while (true);
                        if (await comfyUIClass.Prompt() is (bool, string) PromptResult && !PromptResult.Item1) { await SendProcess(PromptResult.Item2); await AutoDLClose(); return; }
                        #endregion

                        #region
                        await SendProcess($"10.工作流执行成功,正在获取工作流执行状态....");
                        do
                        {
                            if (await comfyUIClass.GetPrompt() is (bool, int) GetPromptResult && GetPromptResult.Item1)
                            {
                                if (GetPromptResult.Item2 == 0) { break; }
                                if (GetPromptResult.Item2 == -1) { await SendProcess("工作流执行失败！"); await AutoDLClose(); return; }    
                            }
                            await SendProcess($"当前剩余任务列队的数量{GetPromptResult.Item2},1分钟后重新获取");
                            await Task.Delay(TimeSpan.FromMinutes(1));
                        } while (true);//await comfyUIClass.Websocket();//await comfyUIClass.Task;
                        #endregion

                        #region
                        await SendProcess($"11.工作流执行完成,正在下载生成的视频....");
                        using var InputVideo = sshHelper.DownloadStream(tb.InputPath);
                        if (InputVideo == Stream.Null || InputVideo.Length == 0) { await SendProcess("视频获取失败！"); await AutoDLClose(); return; }
                        await _bot.SendVideo(msg.Chat.Id, InputVideo);
                        using var AIvideo = sshHelper.DownloadStream(tb.OutputPath[0]);
                        if (AIvideo == Stream.Null || AIvideo.Length == 0) { await SendProcess("视频获取失败！"); await AutoDLClose(); return; }
                        await _bot.SendVideo(msg.Chat.Id, AIvideo);
                        foreach (var path in tb.OutputPath) { sshHelper.DeleteFile(path); }
                        #endregion
                        await SendProcess($"12.任务执行成功,正在关闭实例....");
                        await AutoDLClose();
                    }
                    catch (Exception ex)
                    {
                        await AutoDLClose();
                        await SendProcess($"❌ 操作失败,已关闭实例: {ex.Message}");
                    }
                    return;
                }
                await SendProcess($"{detail}！");
                async Task<Message> SendProcess(string text)
                {
                    stringBuilder.AppendLine(text);
                    return await _bot.EditMessageText(msg.Chat.Id, StartMessage.Id, stringBuilder.ToString());
                }
                async Task AutoDLClose()
                {
                    var Close = await AutoDLClass.Close(tb.Authorization, tb.Instance_uuid);
                    if (!Close) { await SendProcess($"当前AutoDL实列关机失败,未关机将持续计费,请火速联系管理员！"); return; }
                }
            }
        }
        public async Task OnUpdate(Update update)
        {
            if (update is { CallbackQuery: { } query })
            {
                await _bot.AnswerCallbackQuery(query.Id, $"正在加载 {query.Data}");
                await _bot.SendMessage(query.Message!.Chat, $"用户 {query.From} 点击了 {query.Data}");
            }
        }
        [GeneratedRegex(@"@[A-Za-z0-9_]+_bot\b")]
        private static partial Regex BotRegex();
        [GeneratedRegex(@"https?:\/\/(?:v\.douyin\.com\/[A-Za-z0-9_-]+\/?|b23\.tv\/[A-Za-z0-9_-]+\/?|www\.douyin\.com\/(?:video\/\d+|discover\?[^ \n]+)|www\.tiktok\.com\/(?:t\/[A-Za-z0-9]+\/?|@[A-Za-z0-9._-]+\/video\/\d+))")]
        private static partial Regex MatchUrlRegex();
    }
    public class TgBotClassRespose
    {
        public required string Token { get; set; }
        public long ChatId { get; set; }
        public required string DownApiUrl { get; set; }
        public required string Authorization { get; set; }
        public required string Instance_uuid { get; set; }
        public required string Payload { get; set; }
        public required string Host { get; set; }
        public int Port { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string InputPath { get; set; }
        public required string[] OutputPath { get; set; }
        public uint ComfyUIPort { get; set; }
        public required string ComfyUIPrompt { get; set; }
    }
}
