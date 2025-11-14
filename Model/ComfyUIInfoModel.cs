namespace OxalisApi.Model
{
    public class ComfyUIInfoModel
    {
        public class ComfyUIInfo
        {
            public required string InputPath { get; set; }
            public required string[] OutputPath { get; set; }
            public uint Port { get; set; }
            public required string Prompt { get; set; }
            public int WaitTimeMin { get; set; }
        }
    }
}
