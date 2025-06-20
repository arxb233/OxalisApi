using System.Text.Json.Serialization;

namespace HKRM_Server_C.CommonBusiness
{
    public class VueClass
    {
        public class Option(string value, string? label)
        {
            [JsonPropertyName("value")]
            public string Value { get; set; } = value;
            [JsonPropertyName("label")]
            public string? Label { get; set; } = label;
        }

        public class VanPicker(string? text, List<VanPicker?>? children)
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; } = text;
            [JsonPropertyName("children")]
            public List<VanPicker?>? Children { get; set; } = children;
        }
    }
}
