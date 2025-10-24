using Azure;
using Microsoft.Extensions.FileSystemGlobbing;
using Org.BouncyCastle.Utilities.Zlib;
using OxalisApi.Job;
using Quartz.Util;
using System;
using System.Diagnostics;
using System.Text.Json;
using Tool;
using Tool.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OxalisApi.CommonBusiness
{
    public class VideoDownClass()
    {
        public static async Task<Stream> DownLoad(string DownApiUrl, string MatchUrl,string detail)
        {
            if (detail.StartsWith("BV")) { return await BilibiliDownLoad(DownApiUrl, MatchUrl); }
            return await DouyinDownLoad(DownApiUrl, MatchUrl);
        }
        public static async Task<Stream> DouyinDownLoad(string DownApiUrl, string MatchUrl)
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
            string videoPath = Path.Combine(Path.GetTempPath(), "video.m4s");
            string audioPath = Path.Combine(Path.GetTempPath(), "audio.m4s");
            if (jsonFilePath.TryGet(out var jsonFile, "data", "data", "dash"))
            {
                if (jsonFile.TryGet(out var video, "video"))
                {
                    var videoUrl = video.Select(x => x["baseUrl"].ToString()).FirstOrDefault();
                    if (videoUrl is not null) { await DownloadFileAsync(videoUrl, videoPath); }
                }
                if (jsonFile.TryGet(out var audio, "audio"))
                {
                    var audioUrl = audio.Select(x => x["baseUrl"].ToString()).FirstOrDefault();
                    if (audioUrl is not null) { await DownloadFileAsync(audioUrl, videoPath); }
                }
            }
            string arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a copy -f mp4 pipe:";
            using Stream outputStream = await FFMpegWrapper.MergeFilesWithFFmpeg(arguments, @"D:\ffmpeg\bin\ffmpeg.exe", videoPath, audioPath);
            File.Delete(videoPath); File.Delete(audioPath);
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
        }
    }
}
