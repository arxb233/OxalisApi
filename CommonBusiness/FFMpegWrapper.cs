using System.Diagnostics;

namespace OxalisApi.CommonBusiness
{
    public class FFMpegWrapper
    {
        public void RunFFMpegCommand(string arguments)
        {
            try
            {
                Process ffmpegProcess = new();
                string ffmpegPath = @"D:\ffmpeg\bin\ffmpeg.exe";

                // 配置进程启动信息
                ffmpegProcess.StartInfo.FileName = ffmpegPath;
                ffmpegProcess.StartInfo.Arguments = arguments;
                ffmpegProcess.StartInfo.UseShellExecute = true;  // 不需要重定向
                ffmpegProcess.StartInfo.CreateNoWindow = true;   // 不显示命令行窗口

                // 启动进程
                ffmpegProcess.Start();

                // 等待进程完成
                ffmpegProcess.WaitForExit();

                // 检查进程退出码
                if (ffmpegProcess.ExitCode != 0)
                {
                    Ext.Info($"FFmpeg command failed with exit code {ffmpegProcess.ExitCode}.");
                }
                else
                {
                    Ext.Info("FFmpeg command executed successfully.");
                }
            }
            catch (Exception ex)
            {
                Ext.Info("An error occurred: " + ex.Message);
            }
        }
    }
}
