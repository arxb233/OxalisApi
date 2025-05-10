using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using System.Text;
using Tool.Web.Api;

namespace OxalisApi.Controllers.ABD
{
    public class Adb : MinApi
    {
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> RebootUpdate(string deviceIp, string port = "5555")
        {
            try
            {
                var _adbClient = new AdbClient();
                string deviceSerial = $"{deviceIp}:{port}";
                _adbClient.Connect(deviceSerial);

                var devices = await _adbClient.GetDevicesAsync();
                var device = devices.FirstOrDefault(d => d.Serial == deviceSerial)!;

                if (device == null)
                {
                    return new JsonOut(new { message = $"未找到设备，序列号: {deviceSerial}" }) { StatusCode = 500 };
                }
                if (string.IsNullOrEmpty(device.Serial) || device.State != DeviceState.Online)
                {
                    return new JsonOut(new { message = $"设备状态异常: Serial={device.Serial}, State={device.State}" }) { StatusCode = 500 };
                }

                var response = _adbClient.ExecuteRemoteEnumerableAsync("reboot update", device, Encoding.UTF8, CancellationToken.None);
                await foreach (var item in response) { }

                return new JsonOut(new { message = $"设备 {deviceSerial} 已重启进入更新模式" });
            }
            catch (Exception ex)
            {
                return new JsonOut(new { message = $"执行重启命令时出错: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
