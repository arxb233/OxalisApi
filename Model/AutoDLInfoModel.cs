namespace OxalisApi.Model
{
    public class AutoDLInfoModel
    {
        public class AutoDLInfo
        {
            public required string Authorization { get; set; }
            public required string Instance_uuid { get; set; }
            public required string Payload { get; set; }
            public required string Host { get; set; }
            public int Port { get; set; }
            public required string Username { get; set; }
            public required string Password { get; set; }
        }
    }
}
