using System.Text.Json.Serialization;

namespace OxalisApi.CommonBusiness
{
    public class BarkClass
    {
        public class BarkRequest
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("subtitle")]
            public string? Subtitle { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }

            [JsonPropertyName("markdown")]
            public string? Markdown { get; set; }

            [JsonPropertyName("device_key")]
            public string? DeviceKey { get; set; }

            [JsonPropertyName("device_keys")]
            public List<string>? DeviceKeys { get; set; }

            /// <summary>
            /// critical / active / timeSensitive / passive
            /// </summary>
            [JsonPropertyName("level")]
            public string? Level { get; set; } = "active";

            /// <summary>
            /// 0 - 10（仅 critical 有效）
            /// </summary>
            [JsonPropertyName("volume")]
            public int? Volume { get; set; }

            [JsonPropertyName("badge")]
            public int? Badge { get; set; }

            /// <summary>
            /// "1" 重复响铃
            /// </summary>
            [JsonPropertyName("call")]
            public string? Call { get; set; }

            /// <summary>
            /// "1" 自动复制
            /// </summary>
            [JsonPropertyName("autoCopy")]
            public string? AutoCopy { get; set; }

            [JsonPropertyName("copy")]
            public string? Copy { get; set; }

            [JsonPropertyName("sound")]
            public string? Sound { get; set; }

            [JsonPropertyName("icon")]
            public string? Icon { get; set; }

            [JsonPropertyName("image")]
            public string? Image { get; set; }

            [JsonPropertyName("group")]
            public string? Group { get; set; }

            [JsonPropertyName("ciphertext")]
            public string? Ciphertext { get; set; }

            /// <summary>
            /// 1 保存
            /// </summary>
            [JsonPropertyName("isArchive")]
            public int? IsArchive { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            /// <summary>
            /// "alert"
            /// </summary>
            [JsonPropertyName("action")]
            public string? Action { get; set; }

            /// <summary>
            /// 相同 id 会更新通知
            /// </summary>
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            /// <summary>
            /// "1" 删除通知（需配合 id）
            /// </summary>
            [JsonPropertyName("delete")]
            public string? Delete { get; set; }
        }
    }
}
