using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Tool;
using Tool.Utils;
using Tool.Utils.Data;
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
        public static async Task DownloadFileAsStreamAsync(Stream Stream, string localFilePath)
        {
            using FileStream localFileStream = new(localFilePath, FileMode.Create, FileAccess.Write);
            await Stream.CopyToAsync(localFileStream);
        }
        public static Stream ReadLocalFileAsStream(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"本地缓存文件不存在！");
            }
            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }
        public static bool DeleteFile(string filePath)
        {
            if (File.Exists(filePath)) { File.Delete(filePath); return true; }
            return false;
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
    }
}
