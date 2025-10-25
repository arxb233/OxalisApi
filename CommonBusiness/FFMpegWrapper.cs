using Org.BouncyCastle.Utilities.Zlib;
using System;
using System.Diagnostics;

namespace OxalisApi.CommonBusiness
{
    public class FFMpegWrapper
    {
        public static async Task RunFFMpegCommand(string arguments,string ffmpegPath)
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
            using var process = new Process { StartInfo = processInfo };
            await FFMpegCommand(process);
        }
        public static async Task FFMpegCommand(Process process)
        {
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                process.Dispose();
                if (process.ExitCode != 0) { Ext.Info($"FFmpeg执行失败: {error}"); }
                Ext.Info("FFmpeg执行成功！");
            }
            catch (Exception ex)
            {
                Ext.Info($"FFmpeg 处理失败: {ex.Message}");
            }
        }
    }
}
