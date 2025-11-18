using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text.Json;
using Tool;
using Tool.Sockets.TrojanHelper;
using Tool.Utils;
using Tool.Utils.Data;
using static OxalisApi.Model.TgBotModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OxalisApi.CommonBusiness
{
    public static partial class Ext
    {
        public static ILogger Logger => ObjectExtension.Provider.GetService<ILoggerFactory>()?.CreateLogger("Console.Write")!;


        public static void Info(string msg, params object[] args)
        {
            Logger.LogInformation(msg, args);
        }

        public static ValueTask InfoAsync(string msg, params object[] args)
        {
            Info(msg, args);
            return ValueTask.CompletedTask;
        }

        public static void Debug(string msg, params object[] args)
        {
            Logger.LogDebug(msg, args);
        }

        public static ValueTask DebugAsync(string msg, params object[] args)
        {
            Debug(msg, args);
            return ValueTask.CompletedTask;
        }

        public static void Error(Exception exception, string msg, params object[] args)
        {
            Logger.LogError(exception, msg, args);
        }

        public static ValueTask ErrorAsync(Exception exception, string msg, params object[] args)
        {
            Error(exception, msg, args);
            return ValueTask.CompletedTask;
        }
        public static bool IsEmptyJsonVar(JsonVar jsonvar)
        {
            foreach (JsonVar item in jsonvar)
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        if (item.IsNullOrEmpty()) { return true; }
                        if (IsEmptyJsonVar(item)) { return true; }
                        break;
                    case JsonValueKind.Undefined:
                    case JsonValueKind.Null:
                        return true;
                    default:
                        if (item.ToString().IsNullOrEmpty()) { return true; }
                        break;
                }
            }
            return false;
        }
        public static HttpClient? TrojanToHttpClient(TgBotClassRespose tb)
        {
            var TrojanList = new List<TrojanConnect>();
            for (var i = tb.TgBot.Trojan.Port.Start; i <= tb.TgBot.Trojan.Port.End; i++)
            {
                TrojanList.Add(new TrojanConnect(tb.TgBot.Trojan.Host, i, tb.TgBot.Trojan.Password));
            }
            var _Trojan = new TrojanHttpHandlerFactory([.. TrojanList]);
            return new HttpClient(_Trojan.HttpMessageHandler) { Timeout = TimeSpan.FromSeconds(300) };
        }
    }
}
