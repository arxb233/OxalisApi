using Renci.SshNet;
using Tool;
using Tool.Sockets.WebHelper;
using Tool.Utils;
using Tool.Utils.TaskHelper;
namespace OxalisApi.CommonBusiness
{
    public class LinuxSshHelper(string host, int port, string username, string password) : IDisposable
    {
        private ForwardedPortLocal? _forward;

        private readonly SshClient ssh = new(host, port, username, password);
        private readonly SftpClient sftp = new(host, port, username, password);

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void Connect()
        { 
            ssh.Connect();
            sftp.Connect();
            Console.WriteLine("✅ 已连接到服务器");
        }
        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void OpenPort(uint port)
        {
            _forward = new ForwardedPortLocal("127.0.0.1", port, "127.0.0.1", port);
            ssh.AddForwardedPort(_forward);
            _forward.Start();
            if (_forward.IsStarted)
            {
                Console.WriteLine("✅ 已连接到服务器");
            }
        }

        /// <summary>
        /// 上传流文件
        /// </summary>
        public void UploadFile(string localPath, string remotePath)
        {
            using (var fs = new FileStream(localPath, FileMode.Open))
            {
                sftp.UploadFile(fs, remotePath, true);
            }
            Console.WriteLine($"⬆️ 文件上传成功: {localPath} → {remotePath}");
        }
        /// <summary>
        /// 上传文件
        /// </summary>
        public void UploadStream(Stream input, string path)
        {
            sftp.UploadFile(input, path, true);
        }

        /// <summary>
        /// 下载流文件
        /// </summary>
        public void DownloadFile(string remotePath, string localPath)
        {
            using (var fs = new FileStream(localPath, FileMode.Create))
            {
                sftp.DownloadFile(remotePath, fs);
            }
            Console.WriteLine($"⬇️ 文件下载成功: {remotePath} → {localPath}");
        }
        public MemoryStream DownloadStream(string localPath)
        {
            var ms = new MemoryStream();
            sftp.DownloadFile(localPath, ms);
            ms.Position = 0;
            return ms;
        }
        
        public bool fileExists(string localPath)
        {
             return sftp.Exists(localPath);
        }
        /// <summary>
        /// 设置远程文件权限
        /// </summary>
        public void Chmod(string remoteFile, string mode = "+x")
        {
            ssh.RunCommand($"chmod {mode} {remoteFile}");
            Console.WriteLine($"🔧 设置权限 {mode} : {remoteFile}");
        }
        public void DeleteFile(string remotePath)
        {
            sftp.DeleteFile(remotePath);
        }
        public SshCommand RunCommand(string Script)
        {
            var cmd = ssh.RunCommand(Script);
            return cmd;
        }
        public void Disconnect()
        {
            if (ssh.IsConnected == true) ssh.Disconnect();
            if (sftp.IsConnected == true) sftp.Disconnect();
            Console.WriteLine("🔌 已断开连接");
        }

        public void Dispose()
        {
            ssh.Dispose();
            sftp.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
