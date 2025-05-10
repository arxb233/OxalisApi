using Tool;

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

    }
}
