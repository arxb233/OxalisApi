namespace OxalisApi.CommonBusiness
{
    public class FileClass
    {
        public static async Task DownloadFileAsStreamAsync(Stream Stream, string tempPath)
        {
            using FileStream localFileStream = new(tempPath, FileMode.Create, FileAccess.Write);
            await Stream.CopyToAsync(localFileStream);
        }
        public static Stream ReadLocalFileAsStream(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"本地缓存文件不存在！");
            }
            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }
        public static bool DeleteFile(string filePath)
        {
            if (File.Exists(filePath)) { File.Delete(filePath); return true; }
            return false;
        }
    }
}
