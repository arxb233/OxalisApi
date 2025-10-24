using Org.BouncyCastle.Utilities.Zlib;
using System;
using System.Diagnostics;

namespace OxalisApi.CommonBusiness
{
    public class FFMpegWrapper
    {
        public async Task RunFFMpegCommand(string arguments)
        {
            using Process ffmpegProcess = new();
            string ffmpegPath = @"D:\ffmpeg\bin\ffmpeg.exe";
            ffmpegProcess.StartInfo.FileName = ffmpegPath;
            ffmpegProcess.StartInfo.Arguments = arguments;
            ffmpegProcess.StartInfo.UseShellExecute = true;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            await FFMpegCommand(ffmpegProcess);
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();
        }
        public static async Task<Stream> MergeFilesWithFFmpeg(string arguments,string ffmpegPath, string videoPath, string audioPath)
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
            using var outputStream = new MemoryStream();
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(args.Data);
                    outputStream.Write(buffer, 0, buffer.Length);
                }
            };
            await FFMpegCommand(process);
            return outputStream;
        }
        public static async Task FFMpegCommand(Process process)
        {
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
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
