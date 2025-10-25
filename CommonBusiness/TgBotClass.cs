using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tool;

namespace OxalisApi.CommonBusiness
{
    public partial class TgBotClass(TgBotClassRespose tb)
    {
        public TelegramBotClient _bot = new(tb.TgBot.Token);
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
            if (msg.Text != null && msg.Chat.Id == tb.TgBot.ChatId && BotRegex().Match(msg.Text) is Match MatchBot && MatchBot.Success)
            {
                var detail = msg.Text.Replace(MatchBot.Value, "");
                StringBuilder stringBuilder = new(); stringBuilder.AppendLine("我已收到消息:");
                if (detail.IsNullOrEmpty()) { await _bot.SendMessage(msg.Chat.Id, $"消息内容不能为空！"); return; }
                var StartMessage = await _bot.SendMessage(msg.Chat.Id, stringBuilder.ToString());
                var EndMessage = StartMessage;
                if (MatchUrlRegex().Match(detail) is Match MatchUrl && MatchUrl.Success)
                {
                    try
                    {
                        #region Wallet
                        await SendProcess($"任务开始执行....");
                        await SendProcess($"1.正在查询AutodL钱包信息....");
                        var Wallet = await AutoDLClass.Wallet(tb.AutoDL.Authorization);
                        if (Wallet <= 1.5) { await SendProcess($"1.AutoDL余额不足1.5元,请保证余额充足再使用！"); return; }
                        #endregion

                        #region GPU
                        await SendProcess($"2.当前AutoDL账户余额为{Wallet}元,正在查询AutoDL设备信息....");
                        var Check = await AutoDLClass.Check(tb.AutoDL.Authorization, tb.AutoDL.Instance_uuid);
                        if (Check == -1) { await SendProcess($"当前AutoDL实列已存在,请稍后使用！"); return; }
                        if (Check == 0) { await SendProcess($"当前AutoDL实列没有可用GPU,请稍后使用！"); return; }
                        #endregion

                        #region Download
                        await SendProcess($"3.当前AutoDL实列可用GPU为{Check}个,满足运行条件！");
                        await SendProcess($"4.视频链接获取成功，正在下载视频，请耐心等待....");
                        using var video = await VideoDownClass.DownLoad(tb.Video, MatchUrl.Value, detail);
                        if (video == Stream.Null || video.Length <= 333) { await SendProcess("视频获取失败！"); return; }
                        #endregion

                        #region Power_on
                        await SendProcess($"5.视频下载成功,正在进行开机,预计30s....");
                        var Open = await AutoDLClass.Open(tb.AutoDL.Authorization, tb.AutoDL.Instance_uuid, tb.AutoDL.Payload);
                        if (!Open) { await SendProcess($"当前AutoDL实列开机失败,请联系管理员！"); return; }
                        do { if (await AutoDLClass.Check(tb.AutoDL.Authorization, tb.AutoDL.Instance_uuid) == -1) { break; }; await Task.Delay(5000); } while (true);
                        #endregion

                        #region SSH
                        await SendProcess($"6.开机成功,正在进行远程链接....");
                        using var sshHelper = new LinuxSshHelper(tb.AutoDL.Host, tb.AutoDL.Port, tb.AutoDL.Username, tb.AutoDL.Password);
                        sshHelper.Connect(); sshHelper.OpenPort(tb.ComfyUI.Port);
                        await SendProcess($"8.远程链接成功,正在获取工作流....");
                        using var PromptStream = sshHelper.DownloadStream(tb.ComfyUI.Prompt);
                        if (sshHelper.FileExists(tb.ComfyUI.OutputPath[0]))
                        {
                            await SendProcess("输出文件已存在,取消当前任务，返回上条任务文件数据！");
                            await OutPutMessage(sshHelper); return;
                        }
                        await SendProcess($"7.工作流获取成功,正在上传视频....");
                        sshHelper.UploadStream(video, tb.ComfyUI.InputPath);
                        #endregion

                        #region ComfyUI
                        await SendProcess($"9.工作流获取成功,正在启动服务....");
                        ComfyUIClass comfyUIClass = new($"127.0.0.1:{tb.ComfyUI.Port}", tb.ComfyUI.Prompt, PromptStream.ToArray().ToByteString(), tb.ComfyUI.WaitHour, (_msg) => SendProcess(_msg));
                        //do { if (await comfyUIClass.GetPrompt() is (bool, int) GetPromptResult && GetPromptResult.Item1) { break; } await Task.Delay(TimeSpan.FromSeconds(10)); } while (true);
                        await SendProcess($"10.启动服务成功,正在执行并获取工作流状态....");
                        await comfyUIClass.Websocket();
                        if (await comfyUIClass.Prompt() is (bool, string) PromptResult && !PromptResult.Item1) { await SendProcess(PromptResult.Item2); await AutoDLClose(); return; }
                        #endregion

                        #region Prompt
                        await comfyUIClass.Task;
                        #endregion

                        #region OutPut
                        await OutPutMessage(sshHelper);
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        await AutoDLClose();
                        await SendProcess($"❌ 操作失败: {ex.Message}");
                    }
                    return;
                }
                await SendProcess($"{detail}！");
                async Task SendProcess(string? text)
                {
                    if (text is not null)
                    {
                        string EditMessage = stringBuilder.ToString() + text;
                        if (!text.Contains("进度")) { stringBuilder.AppendLine(text); EditMessage = stringBuilder.ToString(); }
                        try { if (EndMessage.Text != EditMessage) { EndMessage = await _bot.EditMessageText(msg.Chat.Id, StartMessage.Id, EditMessage); } } catch { }
                    }
                }
                async Task OutPutMessage(LinuxSshHelper ssh)
                {
                    await SendProcess($"11.工作流执行完成,正在下载生成的视频....");
                    using var AIvideo = ssh.DownloadStream(tb.ComfyUI.OutputPath[0]);
                    if (AIvideo == Stream.Null || AIvideo.Length == 0) { await SendProcess("视频获取失败！"); await AutoDLClose(); return; }
                    await FileClass.DownloadFileAsStreamAsync(AIvideo, Path.Combine(Path.GetTempPath(), tb.TgBot.CacheFile));
                    using var InputVideo = ssh.DownloadStream(tb.ComfyUI.InputPath);
                    if (InputVideo == Stream.Null || InputVideo.Length == 0) { await SendProcess("视频获取失败！"); await AutoDLClose(); return; }
                    await _bot.SendVideo(tb.TgBot.ChatVideoId, InputVideo, msg.Text);
                    foreach (var path in tb.ComfyUI.OutputPath) { ssh.DeleteFile(path); }
                    await SendProcess($"12.任务执行成功,正在关闭实例....");
                    await AutoDLClose();
                }
                async Task AutoDLClose()
                {
                    var Close = await AutoDLClass.Close(tb.AutoDL.Authorization, tb.AutoDL.Instance_uuid);
                    if (!Close) { await SendProcess($"当前AutoDL实列关机失败,未关机将持续计费,请火速联系管理员！"); return; }
                    await SendProcess($"13.实列已关闭！");
                }
            }
            if (msg.Caption != null && msg.Chat.Id == tb.TgBot.ChatVideoGroupId && BotRegex().Match(msg.Caption) is Match MatchBotGroup && MatchBotGroup.Success)
            {
                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), tb.TgBot.CacheFile);
                    using var _outputStream = FileClass.ReadLocalFileAsStream(tempPath);
                    await _bot.SendVideo(msg.Chat.Id, _outputStream, "", ParseMode.None, msg);
                    FileClass.DeleteFile(tempPath);
                }
                catch
                {
                    await _bot.SendMessage(msg.Chat.Id, "缓存文件获取或删除失败！", ParseMode.None, msg);
                }
            }
        }
        public async Task OnUpdate(Update update)
        {
            switch (update)
            {
                case { CallbackQuery: { } callbackQuery }: await OnCallbackQuery(callbackQuery); break;
                case { PollAnswer: { } pollAnswer }: await OnPollAnswer(pollAnswer); break;
                default: Console.WriteLine($"Received unhandled update {update.Type}"); break;
            }
        }
        public async Task OnCallbackQuery(CallbackQuery callbackQuery)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id, $"正在加载 {callbackQuery.Data}");
            await _bot.SendMessage(callbackQuery.Message!.Chat, $"用户 {callbackQuery.From} 点击了 {callbackQuery.Data}");
        }

        public async Task OnPollAnswer(PollAnswer pollAnswer)
        {
            if (pollAnswer.User != null)
                await _bot.SendMessage(pollAnswer.User.Id, $"You voted for option(s) id [{string.Join(',', pollAnswer.OptionIds)}]");
        }
        [GeneratedRegex(@"@[A-Za-z0-9_]+_bot\b")]
        private static partial Regex BotRegex();
        [GeneratedRegex(@"https?:\/\/(?:v\.douyin\.com\/[A-Za-z0-9_-]+\/?)|BV[A-Za-z0-9]+")]
        private static partial Regex MatchUrlRegex();
    }
    public class TgBotClassRespose
    {
        public required TgBotInfo TgBot { get; set; }
        public required VideoInfo Video { get; set; }
        public required AutoDLInfo AutoDL { get; set; }
        public required ComfyUIInfo ComfyUI { get; set; }
    }
    public class TgBotInfo
    {
        public required string Token { get; set; }
        public long ChatId { get; set; }
        public long ChatVideoId { get; set; }
        public long ChatVideoGroupId { get; set; }
        public required string CacheFile { get; set; }
    }
    public class VideoInfo
    {
        public required string DownApiUrl { get; set; }
        public required string FFmpegPath { get; set; }
        public required string FFprobePath { get; set; }
        public required string VideoPath { get; set; }
        public required string AudioPath { get; set; }
        public required string OutputPath { get; set; }
        public required string OutputSplitPath { get; set; }
    }
    public class AutoDLInfo
    {
        public required string Authorization { get; set; }
        public required string Instance_uuid { get; set; }
        public required string Payload { get; set; }
        public required string Host { get; set; }
        public int Port { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
    public class ComfyUIInfo
    {
        public required string InputPath { get; set; }
        public required string[] OutputPath { get; set; }
        public uint Port { get; set; }
        public required string Prompt { get; set; }
        public int WaitHour { get; set; }
    }
}
