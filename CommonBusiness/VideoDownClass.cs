using Azure;
using Microsoft.Extensions.FileSystemGlobbing;
using Org.BouncyCastle.Utilities.Zlib;
using OxalisApi.Controllers.SD;
using OxalisApi.Job;
using Quartz.Util;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using Telegram.Bot.Types;
using Tool;
using Tool.Utils;
using static OxalisApi.Model.VideoDownloadModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OxalisApi.CommonBusiness
{
    public class VideoDownClass()
    {
        public static async Task<Stream> DownLoad(VideoInfo VideoInfo, string MatchUrl, string detail)
        {
            if (detail.Trim().StartsWith("BV")) { return await BilibiliDownLoad(VideoInfo, MatchUrl); }
            return await DouyinDownLoad(VideoInfo, MatchUrl);
        }
        public static async Task<Stream> DouyinDownLoad(VideoInfo videoInfo, string MatchUrl)
        {
            string url = $"{videoInfo.DownApiUrl}/api/download?url={MatchUrl}&prefix=true&with_watermark=false";
            var video = await HttpClientClass.StreamAsync(url);
            if (video is not null)
            {
                await FileClass.DownloadFileAsStreamAsync(video, videoInfo.OutputPath);
                await SpiltVideo(videoInfo);
                Stream outputStream = FileClass.ReadLocalFileAsStream(videoInfo.OutputSplitPath);
                return outputStream;
            }
            return Stream.Null;
        }
        public static async Task<Stream> BilibiliDownLoad(VideoInfo VideoInfo, string MatchUrl)
        {
            string Aidurl = $"{VideoInfo.DownApiUrl}/api/bilibili/web/fetch_video_parts?bv_id={MatchUrl}";
            var AidResult = await HttpClientClass.GetAsync(Aidurl);
            if (AidResult.TryGet(out var cid, "data", "data"))
            {
                var cidinfo = cid.Select(x => x["cid"].ToString()).FirstOrDefault();
                if (cidinfo is not null)
                {
                    string Videourl = $"{VideoInfo.DownApiUrl}/api/bilibili/web/fetch_video_playurl?bv_id={MatchUrl}&cid={cidinfo}";
                    var VideoResult = await HttpClientClass.GetAsync(Videourl);
                    var video = await BilibiliParse(VideoResult, VideoInfo);
                    return video;
                }
            }
            return Stream.Null;
        }
        private static async Task<Stream> BilibiliParse(JsonVar jsonFilePath, VideoInfo VideoInfo)
        {
            if (jsonFilePath.TryGet(out var jsonFile, "data", "data", "dash"))
            {
                if (jsonFile.TryGet(out var video, "video"))
                {
                    var videoUrl = video.Select(x => x["baseUrl"].ToString()).FirstOrDefault();
                    if (videoUrl is not null) { await DownloadFileAsync(videoUrl, VideoInfo.VideoPath); }
                }
                if (jsonFile.TryGet(out var audio, "audio"))
                {
                    var audioUrl = audio.Select(x => x["baseUrl"].ToString()).FirstOrDefault();
                    if (audioUrl is not null) { await DownloadFileAsync(audioUrl, VideoInfo.AudioPath); }
                }
            }
            string arguments = $"-i \"{VideoInfo.VideoPath}\" -i \"{VideoInfo.AudioPath}\" -c:v copy -c:a copy -f mp4 -y \"{VideoInfo.OutputPath}\"";
            using var Process = FFMpegWrapper.ProcessCreate(arguments, VideoInfo.FFmpegPath);
            await FFMpegWrapper.FFMpegCommand(Process);
            await SpiltVideo(VideoInfo);
            Stream outputStream = FileClass.ReadLocalFileAsStream(VideoInfo.OutputSplitPath);
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
        public static async Task SpiltVideo(VideoInfo VideoInfo)
        {
            string FFprobeArguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{VideoInfo.OutputPath}\"";
            using var FFprobeProcess = FFMpegWrapper.ProcessCreate(FFprobeArguments, VideoInfo.FFprobePath);
            double duration = Convert.ToDouble(await FFMpegWrapper.FFProbeCommand(FFprobeProcess));
            double startTime = duration > 15 ? (int)((duration - 13) / 2) : 0;
            double Time = duration > 15 ? 13 : duration;
            string arguments = $"-i \"{VideoInfo.OutputPath}\" -ss {startTime} -t {Time} -c:v libx264 -c:a aac -f mp4 -y \"{VideoInfo.OutputSplitPath}\"";
            using var Process = FFMpegWrapper.ProcessCreate(arguments, VideoInfo.FFmpegPath);
            await FFMpegWrapper.FFMpegCommand(Process);
        }
    }
}
