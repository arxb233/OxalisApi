using Microsoft.AspNetCore.Mvc;
using OxalisApi.CommonBusiness;
using Telegram.Bot;
using Tool.Web.Api;

namespace OxalisApi.Controllers.TgBot
{
    public class TgBot : MinApi
    {
        private TelegramBotClient? _bot;
        [Ashx(State = AshxState.Post)]
        public async Task<IApiOut> Create([ApiVal(Val.BodyJson)] TgBotClassRespose tb)
        {
            //TgBotClassRespose tb = new() { Token = Token,ChatId= ChatId,DownApiUrl= DownApiUrl };
            if (_bot is not null) { return ApiOut.Write("已存在机器人，只允许创建一个机器人！"); }
            if (tb is null || string.IsNullOrWhiteSpace(tb.Token) || string.IsNullOrWhiteSpace(tb.DownApiUrl)) { return ApiOut.Write("创建数据不能为空！"); }
            var TgBot = new TgBotClass(tb);
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
