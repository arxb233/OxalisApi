using OxalisApi.CommonBusiness;
using Telegram.Bot;
using Tool.Web.Api;

namespace OxalisApi.Controllers.TgBot
{
    public class TgBot : MinApi
    {
        private TelegramBotClient? _bot;
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> Create(string token, long chatid)
        {
            if (_bot is not null) { return ApiOut.Write("已存在机器人，只允许创建一个机器人！"); }
            var TgBot = new TgBotClass(token,chatid);
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
