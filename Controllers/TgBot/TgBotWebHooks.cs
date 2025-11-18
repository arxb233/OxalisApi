using Microsoft.AspNetCore.Mvc;
using OxalisApi.CommonBusiness;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Tool;
using Tool.Sockets.TrojanHelper;
using Tool.Utils;
using Tool.Web.Api;
using Tool.Web.Routing;
using static OxalisApi.Model.TgBotModel;
using static System.Net.Mime.MediaTypeNames;

namespace OxalisApi.Controllers.TgBot
{
    public class TgBotWebHooks : MinApi
    {
        private static readonly Dictionary<string, TelegramBotClient> bots = [];
        private static readonly Dictionary<string, string> webhookUrls = [];
        private static readonly Dictionary<string, TgBotClassRespose> botsInfo = [];
        private static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        [Ashx(State = AshxState.Post)]
        public async Task<IApiOut> Create([ApiVal(Val.BodyJson)] TgBotClassRespose[] tbList)
        {
            foreach (var tb in tbList) { AddBot(tb, tb.TgBot.Token); }
            return ApiOut.Write("OK");
        }

        [Ashx(State = AshxState.Post)]
        [AshxRoute("webhook/{token?}")]
        public async Task<IApiOut> Webhook([ApiVal(Val.RouteKey, "token")] string token, [ApiVal(Val.BodyString)] string updatejson)
        {
            var update = updatejson.Json<Update>(options);
            if (update is not null && update?.ChannelPost is not null)
            {
                if (bots.TryGetValue(token, out TelegramBotClient? botClient))
                {
                    using var Message = new TgBotMessages(botsInfo[token], botClient, update.ChannelPost);
                    await Message.OnMessage();
                    return ApiOut.Write("OK");
                }
            }
            return ApiOut.Write("Bot not found.");
        }

        [Ashx(State = AshxState.Post)]
        public async Task<IApiOut> GetWebhookInfo([ApiVal(Val.BodyJson)] TgBotClassRespose tb)
        {
            if (!bots.ContainsKey(tb.TgBot.Token))
            {
                var TgBot = new TgBotClass(tb, Ext.TrojanToHttpClient(tb));
                var webhookinfo = await TgBot._bot.GetWebhookInfo();
                return new JsonOut(webhookinfo);
            }
            return ApiOut.Write("没有找到");
        }

        [Ashx(State = AshxState.Get)]
        public IApiOut GetBots()
        {
            return ApiOut.Write($"{bots.Keys}");
        }

        private static void AddBot(TgBotClassRespose tb, string token)
        {
            if (!bots.ContainsKey(token))
            {
                using var TgBot = new TgBotClass(tb, Ext.TrojanToHttpClient(tb));
                bots[token] = TgBot._bot;
                botsInfo[token] = tb;
                string webhookUrl = $"{tb.TgBot.WebhooksUrl}/webhook/{token}";
                webhookUrls[token] = webhookUrl;
                // TgBot._bot.DeleteWebhook().Wait();
                TgBot._bot.SetWebhook(webhookUrl).Wait();
            }
        }
        private static void RemoveBot(string token)
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
