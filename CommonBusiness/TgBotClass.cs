using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tool;
using Tool.Sockets.TrojanHelper;
using Tool.Sockets.WebHelper;
using static OxalisApi.Model.TgBotModel;

namespace OxalisApi.CommonBusiness
{
    public partial class TgBotClass(TgBotClassRespose tb, HttpClient? Client = null) : IDisposable
    {
        public TelegramBotClient _bot = new(tb.TgBot.Token, Client);
        public async Task<TelegramBotClient> Start()
        {
            var me = await _bot.GetMe();
            _bot.DeleteWebhook().Wait();
            Ext.Info($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
            _bot.OnError += OnError;
            _bot.OnMessage += OnMessage;
            _bot.OnUpdate += OnUpdate;
            return _bot;
        }
        public async Task OnError(Exception exception, HandleErrorSource source)
        {
            if (!(exception.InnerException is HttpRequestException httpRequestException && httpRequestException.InnerException is HttpIOException))
            {
                await Ext.InfoAsync($"发生其他错误：{exception.Message}");
            }
        }
        public async Task OnMessage(Message msg, UpdateType type)
        {
            using var Message = new TgBotMessages(tb, _bot, msg);
            await Message.OnMessage();
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
        public void Dispose()
        {
            _bot.Close();
            GC.SuppressFinalize(this);
        }
    }
}
