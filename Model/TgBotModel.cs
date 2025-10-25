using static OxalisApi.Model.AutoDLInfoModel;
using static OxalisApi.Model.ComfyUIInfoModel;
using static OxalisApi.Model.VideoDownloadModel;

namespace OxalisApi.Model
{
    public class TgBotModel
    {
        public class TgBotClassRespose
        {
            public required TgBotInfo TgBot { get; set; }
            public required VideoInfo Video { get; set; }
            public required AutoDLInfo AutoDL { get; set; }
            public required ComfyUIInfo ComfyUI { get; set; }
        }
        public class TgBotInfo
        {
            public required string Token { get; set; }
            public long ChatId { get; set; }
            public long ChatVideoId { get; set; }
            public long ChatVideoGroupId { get; set; }
            public required TrojanConnectRange Trojan { get; set; }
            public required string CacheFile { get; set; }
        }
        public class TrojanConnectRange
        {
            public required string Host { get; set; }
            public required RangePort Port { get; set; }
            public required string Password { get; set; }
        }
        public class RangePort
        {
            public int Start { get; set; }
            public int End { get; set; }
        }
    }
}
