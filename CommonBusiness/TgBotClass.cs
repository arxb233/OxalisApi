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
                if (detail.IsNullOrEmpty()) { await _bot.SendMessage(msg.Chat.Id, $"消息内容不能为空！"); return; }
                if (MatchUrlRegex().Match(detail) is Match MatchUrl && MatchUrl.Success)
                {
                    try
                    {
                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"视频链接获取成功，正在下载视频，请耐心等待....");
                        var video = await VideoDownClass.DownLoad(tb.DownApiUrl, MatchUrl.Value);
                        if (video == Stream.Null || video.Length <= 333) { await _bot.SendMessage(msg.Chat.Id, "视频获取失败！"); return; }
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"视频下载成功,正在查询AutodL钱包信息....");
                        var Wallet = await AutoDLClass.Wallet(tb.Authorization);
                        if (Wallet <= 1.5) { await _bot.SendMessage(msg.Chat.Id, $"AutoDL余额不足1.5元,请保证余额充足再使用！"); return; }
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"当前AutoDL账户余额为{Wallet}元,正在查询AutoDL设备信息....");
                        var Check = await AutoDLClass.Check(tb.Authorization, tb.Instance_uuid, tb.Payload);
                        if (Check == -1) { await _bot.SendMessage(msg.Chat.Id, $"当前AutoDL实列已存在,请稍后使用！"); return; }
                        if (Check == 0) { await _bot.SendMessage(msg.Chat.Id, $"当前AutoDL实列没有可用GPU,请稍后使用！"); return; }
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"当前AutoDL实列可用GPU为{Check}个,正在进行开机,预计30s....");
                        var Open = await AutoDLClass.Open(tb.Authorization, tb.Instance_uuid, tb.Payload);
                        if (!Open) { await _bot.SendMessage(msg.Chat.Id, $"当前AutoDL实列开机失败,请联系管理员！"); return; }
                        do { if (await AutoDLClass.Check(tb.Authorization, tb.Instance_uuid, tb.Payload) == -1) { break; }; await Task.Delay(5000); } while (true);
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"开机成功,正在进行远程链接....");
                        using var sshHelper = new LinuxSshHelper(tb.Host, tb.Port, tb.Username, tb.Password);
                        sshHelper.Connect(); sshHelper.OpenPort(tb.ComfyUIPort);
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"远程链接成功,正在上传文件....");
                        sshHelper.UploadStream(video, tb.InputPath);
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"服务运行成功,正在获取工作流....");
                        using var PromptStream = sshHelper.DownloadStream(tb.ComfyUIPrompt);
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"工作流获取成功,正在启动服务并执行工作流....");
                        using ComfyUIClass comfyUIClass = new($"127.0.0.1:{tb.ComfyUIPort}", tb.ComfyUIPrompt, PromptStream.ToArray().ToByteString(), (_msg) => _bot.SendMessage(msg.Chat.Id, _msg));
                        do{if (await comfyUIClass.Prompt() is (bool, string) PromptResult && PromptResult.Item1) {await _bot.SendMessage(msg.Chat.Id, PromptResult.Item2);break; }await Task.Delay(10000);} while (true);
                        #endregion

                        #region
                        //await _bot.SendMessage(msg.Chat.Id, $"工作流执行成功,正在获取工作流执行状态....");
                        //await comfyUIClass.Websocket();
                        //await comfyUIClass.Task;
                        await Task.Delay(TimeSpan.FromMinutes(2));
                        #endregion

                        #region
                        await _bot.SendMessage(msg.Chat.Id, $"工作流执行完成,正在下载生成的视频....");
                        var AIvideo = sshHelper.DownloadStream(tb.OutputPath[0]);
                        if (AIvideo == Stream.Null || AIvideo.Length == 0) { await _bot.SendVideo(msg.Chat.Id, "视频获取失败！"); return; }
                        foreach (var path in tb.OutputPath) { sshHelper.DeleteFile(path); }
                        await _bot.SendVideo(msg.Chat.Id, video);
                        await _bot.SendVideo(msg.Chat.Id, AIvideo);
                        #endregion

                        #region
                        var Close = await AutoDLClass.Close(tb.Authorization, tb.Instance_uuid);
                        if (!Close) { await _bot.SendMessage(msg.Chat.Id, $"当前AutoDL实列关机失败,未关机将持续计费,请火速联系管理员！"); return; }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        var Close = await AutoDLClass.Close(tb.Authorization, tb.Instance_uuid);
                        if (!Close) { await _bot.SendMessage(msg.Chat.Id, $"当前AutoDL实列关机失败,未关机将持续计费,请火速联系管理员！"); return; }
                        await _bot.SendMessage(msg.Chat.Id, $"❌ 操作失败,已关闭实例: {ex.Message}");
                    }
                    return;
                }
                await _bot.SendMessage(msg.Chat.Id, $"我已收到消息:{detail}！");
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
        [GeneratedRegex(@"https?:\/\/(?:v\.douyin\.com\/[A-Za-z0-9]+\/?|www\.douyin\.com\/(?:video\/\d+|discover\?[^ \n]+)|www\.tiktok\.com\/(?:t\/[A-Za-z0-9]+\/?|@[A-Za-z0-9._]+\/video\/\d+))")]
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

        public required string ComfyUIIsStart { get; set; }
        public uint ComfyUIPort { get; set; }
        public required string ComfyUIPrompt { get; set; }
    }
}
