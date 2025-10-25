namespace OxalisApi.Model
{
    public class VideoDownloadModel
    {
        public class VideoInfo
        {
            public required string DownApiUrl { get; set; }
            public required string FFmpegPath { get; set; }
            public required string FFprobePath { get; set; }
            public required string VideoPath { get; set; }
            public required string AudioPath { get; set; }
            public required string OutputPath { get; set; }
            public required string OutputSplitPath { get; set; }
        }
    }
}
