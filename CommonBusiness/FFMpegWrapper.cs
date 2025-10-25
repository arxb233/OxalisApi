using Org.BouncyCastle.Utilities.Zlib;
using System;
using System.Diagnostics;

namespace OxalisApi.CommonBusiness
{
    public class FFMpegWrapper
    {
        public static Process ProcessCreate(string arguments,string ffmpegPath)
        {
            ProcessStartInfo processInfo = new()
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return new Process { StartInfo = processInfo };
        }
        public static async Task FFMpegCommand(Process process)
        {
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                string error = await process.StandardError.ReadToEndAsync();
                if (process.ExitCode != 0) { Ext.Info($"FFmpeg执行失败: {error}"); }
                Ext.Info("FFmpeg执行成功！");
            }
            catch (Exception ex)
            {
                Ext.Info($"FFmpeg 处理失败: {ex.Message}");
            }
        }
        public static async  Task<string> FFProbeCommand(Process process)
        {
            try
            {
                process.Start();
                var reader = await process.StandardOutput.ReadToEndAsync();
                return reader;
            }
            catch (Exception ex)
            {
                Ext.Info($"FFmpeg 处理失败: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
