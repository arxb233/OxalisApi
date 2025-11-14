using Microsoft.AspNetCore.Mvc;
using OxalisApi.CommonBusiness;
using Telegram.Bot;
using Telegram.Bot.Types;
using Tool.Web.Api;
using Tool.Web.Routing;
using static OxalisApi.Model.TgBotModel;

namespace OxalisApi.Controllers.TgBot
{
    public class TgBotWebHooks : MinApi
    {
        private static readonly Dictionary<string, TelegramBotClient> bots = [];
        private static readonly Dictionary<string, string> webhookUrls = [];
        private static readonly Dictionary<string, TgBotClassRespose> botsInfo = [];

        [Ashx(State = AshxState.Post)]
        public async Task<IApiOut> Create([ApiVal(Val.BodyJson)] TgBotClassRespose[] tbList)
        {
            foreach (var tb in tbList) { AddBot(tb, tb.TgBot.Token); }
            return ApiOut.Write("OK");
        }

        [Ashx(State = AshxState.Post)]
        [AshxRoute("webhook/{token}")]
        public async Task<IApiOut> Webhook([ApiVal(Val.RouteKey, "token")] string token, [ApiVal(Val.BodyJson)] Update update)
        {
            if (update is not null && update?.Message is not null)
            {
                if (bots.TryGetValue(token, out TelegramBotClient? botClient))
                {
                    using var Message = new TgBotMessages(botsInfo[token], botClient, update.Message);
                    return ApiOut.Write("OK");
                }
            }
            return ApiOut.Write("Bot not found.");
        }
        [Ashx(State = AshxState.Get)]
        public IApiOut GetBots()
        {
            return ApiOut.Write($"{bots.Keys}");
        }

        public static void AddBot(TgBotClassRespose tb, string token)
        {
            if (!bots.ContainsKey(token))
            {
                using var TgBot = new TgBotClass(tb);
                bots[token] = TgBot._bot;
                botsInfo[token] = tb;
                string webhookUrl = $"{tb.TgBot.WebhooksUrl}/webhook/{token}";
                webhookUrls[token] = webhookUrl;
                TgBot._bot.SetWebhook(webhookUrl).Wait();
            }
        }
        public static void RemoveBot(string token)
        {
            if (bots.TryGetValue(token, out TelegramBotClient? value))
            {
                value.DeleteWebhook().Wait();
                bots.Remove(token);
                webhookUrls.Remove(token);
            }
        }
    }
}
