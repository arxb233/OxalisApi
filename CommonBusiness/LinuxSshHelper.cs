using Renci.SshNet;
namespace OxalisApi.CommonBusiness
{
    public class LinuxSshHelper(string host, int port, string username, string password) : IDisposable
    {
        private readonly string host = host;
        private readonly int port = port;
        private readonly string username = username;
        private readonly string password = password;

        private SshClient? ssh;
        private SftpClient? sftp;

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void Connect()
        {
            ssh = new SshClient(host, port, username, password);
            sftp = new SftpClient(host, port, username, password);
            ssh.Connect();
            sftp.Connect();
            Console.WriteLine("✅ 已连接到服务器");
        }

        /// <summary>
        /// 上传流文件
        /// </summary>
        public void UploadFile(string localPath, string remotePath)
        {
            using (var fs = new FileStream(localPath, FileMode.Open))
            {
                sftp?.UploadFile(fs, remotePath, true);
            }
            Console.WriteLine($"⬆️ 文件上传成功: {localPath} → {remotePath}");
        }
        /// <summary>
        /// 上传文件
        /// </summary>
        public void UploadStream(Stream input, string path)
        {
            sftp?.UploadFile(input, path, true);
        }

        /// <summary>
        /// 下载流文件
        /// </summary>
        public void DownloadFile(string remotePath, string localPath)
        {
            using (var fs = new FileStream(localPath, FileMode.Create))
            {
                sftp?.DownloadFile(remotePath, fs);
            }
            Console.WriteLine($"⬇️ 文件下载成功: {remotePath} → {localPath}");
        }
        public Stream DownloadStream(string localPath)
        {
            var ms = new MemoryStream();
            sftp.DownloadFile(localPath, ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public string? RunCommand(string command)
        {
            Console.WriteLine($"🚀 执行命令: {command}");
            var cmd = ssh?.RunCommand(command);
            if (!string.IsNullOrEmpty(cmd?.Error))
            {
                Console.WriteLine($"⚠️ 错误: {cmd.Error}");
            }
            return cmd?.Result?.Trim();
        }

        /// <summary>
        /// 设置远程文件权限
        /// </summary>
        public void Chmod(string remoteFile, string mode = "+x")
        {
            ssh?.RunCommand($"chmod {mode} {remoteFile}");
            Console.WriteLine($"🔧 设置权限 {mode} : {remoteFile}");
        }
        public void DeleteFile(string remotePath)
        {
            sftp?.DeleteFile(remotePath);
        }

        public void Disconnect()
        {
            if (ssh?.IsConnected == true) ssh.Disconnect();
            if (sftp?.IsConnected == true) sftp.Disconnect();
            Console.WriteLine("🔌 已断开连接");
        }

        public void Dispose()
        {
            ssh?.Dispose();
            sftp?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
