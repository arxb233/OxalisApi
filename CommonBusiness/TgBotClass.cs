using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OxalisApi.CommonBusiness
{
    public class TgBotClass(TgBotClassRespose tb)
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
            await Task.Run(() => { Console.WriteLine(exception.ToString()); });
        }
        public async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text != null && msg.Text.Contains($"@") && msg.Text.Contains($"_bot") && msg.Chat.Id == tb.ChatId)
            {
                var split = msg.Text.Split(' ');
                if (split.Length <= 1) { await _bot.SendMessage(msg.Chat.Id, $"消息内容不能为空！");return; }
                var detail = split[1];
                if (msg.Text.Contains("https://v.douyin.com"))
                {
                    await _bot.SendMessage(msg.Chat.Id, $"视频链接获取成功，正在下载视频，请耐心等待....");
                    var video =await VideoDownClass.DownLoad($"{tb.DownApiUrl}/api/download?url={detail}&prefix=true&with_watermark=false");
                    if (video != Stream.Null && video.Length > 0) { await _bot.SendVideo(msg.Chat.Id, video); }return;
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
    }
    public class TgBotClassRespose 
    {
        public required string Token { get; set; }
        public long ChatId { get; set; }
        public required string DownApiUrl { get; set; } 
    }
}
