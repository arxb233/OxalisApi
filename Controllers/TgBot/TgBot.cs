using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OxalisApi.CommonBusiness;
using Telegram.Bot;
using Telegram.Bot.Types;
using Tool;
using Tool.Sockets.TrojanHelper;
using Tool.Utils.Data;
using Tool.Web.Api;
using static OxalisApi.Model.TgBotModel;

namespace OxalisApi.Controllers.TgBot
{
    public class TgBot : MinApi
    {
        private TelegramBotClient? _bot;
        [Ashx(State = AshxState.Post)]
        public async Task<IApiOut> Create([ApiVal(Val.BodyJson)] TgBotClassRespose tb)
        {
            if (_bot is not null) { return ApiOut.Write("已存在机器人，只允许创建一个机器人！"); }
            if (tb is null || Ext.IsEmptyJsonVar(tb.ToJson().JsonVar())) { return ApiOut.Write("创建数据不能有一条为空，请按照示例构造请求！"); }
            if (tb.TgBot.ChatId == tb.TgBot.ChatVideoId && tb.TgBot.ChatId == tb.TgBot.ChatVideoGroupId && tb.TgBot.ChatVideoId == tb.TgBot.ChatVideoGroupId)
            {
                return ApiOut.Write("TG机器人三个群组ID不允许重复！");
            }
            var TrojanList = new List<TrojanConnect>();
            for (var i = tb.TgBot.Trojan.Port.Start; i <= tb.TgBot.Trojan.Port.End; i++)
            {
                TrojanList.Add(new TrojanConnect(tb.TgBot.Trojan.Host, i, tb.TgBot.Trojan.Password));
            }
            var _Trojan = new TrojanHttpHandlerFactory([.. TrojanList]);
            var Trojanclient = new HttpClient(_Trojan.HttpMessageHandler) { Timeout = TimeSpan.FromSeconds(300) };
            var TgBot = new TgBotClass(tb, Trojanclient);
            _bot = await TgBot.Start();
            return ApiOut.Write("Tg机器人创建成功！");
        }
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> Stop()
        {
            if (_bot is null) { return ApiOut.Write("没有可以关闭的机器人！"); }
            await _bot.Close();
            return ApiOut.Write("Tg机器人关闭成功！");
        }
    }
}
