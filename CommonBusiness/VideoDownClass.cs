using Azure;
using Microsoft.Extensions.FileSystemGlobbing;
using Org.BouncyCastle.Utilities.Zlib;
using OxalisApi.Job;
using System;
using System.Diagnostics;
using System.Text.Json;
using Tool;
using Tool.Utils;

namespace OxalisApi.CommonBusiness
{
    public class VideoDownClass()
    {
        public static async Task<Stream> DownLoad(string DownApiUrl, string MatchUrl)
        {
            string url = $"{DownApiUrl}/api/download?url={MatchUrl}&prefix=true&with_watermark=false";
            var video = await HttpClientClass.StreamAsync(url);
            return video;
        }
        public static async Task<Stream> BilibiliDownLoad(string DownApiUrl, string MatchUrl)
        {
            string Aidurl = $"{DownApiUrl}/api/bilibili/web/bv_to_aid?bv_id={MatchUrl}";
            var AidResult = await HttpClientClass.GetAsync(Aidurl);
            string Videourl = $"{DownApiUrl}/api/bilibili/web/fetch_video_playurl?bv_id={MatchUrl}&cid={AidResult}";
            var VideoResult = await HttpClientClass.GetAsync(Aidurl);
            var video = await BilibiliParse(VideoResult);
            return video;
        }
        private static async Task<Stream> BilibiliParse(JsonVar jsonFilePath)
        {
            string jsonString = File.ReadAllText(jsonFilePath);
            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;
            JsonElement dash = root.GetProperty("data").GetProperty("data").GetProperty("dash");
            string videoUrl = dash.GetProperty("video")[0].GetProperty("baseUrl").GetString();
            string audioUrl = dash.GetProperty("audio")[0].GetProperty("baseUrl").GetString();
            string videoPath = "video.m4s";
            string audioPath = "audio.m4s";
           // if(jsonFilePath.TryGet())
            await DownloadFileAsync(videoUrl, videoPath);
            await DownloadFileAsync(audioUrl, audioPath);
            using Stream outputStream = await MergeFilesWithFFmpeg(videoPath, audioPath);
            File.Delete(videoPath);
            File.Delete(audioPath);
            return outputStream;
        }
        private static async Task DownloadFileAsync(string url, string outputPath)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            client.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com");
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[] content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(outputPath, content);
            Console.WriteLine($"下载完成: {outputPath}");
        }

        private static async Task<Stream> MergeFilesWithFFmpeg(string videoPath, string audioPath)
        {
            string ffmpegPath = "ffmpeg";
            string arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a copy -f mp4 pipe:";
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
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                if (process.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg 合并失败: {error}");
                }
                Console.WriteLine("FFmpeg 合并成功！");
                outputStream.Position = 0;
                return outputStream;
            }
            catch (Exception ex)
            {
                outputStream.Dispose();
                throw new Exception($"FFmpeg 处理失败: {ex.Message}");
            }
        }

    }
}
