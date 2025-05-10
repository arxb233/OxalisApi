

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Tool.Web.Api;
using OxalisApi.CommonBusiness;
using Tool.Utils;
using Tool;

namespace OxalisApi.Controllers.SD
{
    public partial class Img2imgClass
    {
        public static async Task<string?> ProcessBatchImages(img2imgbatch model)
        {
            string url = $"{model.ApiUrl}/sdapi/v1/img2img";
            var requestData = new
            {
                prompt = model.Prompt,
                negative_prompt = model.NegativePrompt,
                steps = 20,
                sampler_name = "DPM++ 2M",
                scheduler = "Karras",
                cfg_scale = 7,
                seed = 2438066964,
                batch_size = 1,
                mask_blur = 4,
                denoising_strength = 0.75,
                init_images = new string[] { model.ImagePaths },
                mask = model.MaskPaths,
                resize_mode = 1,
                image_cfg_scale = model.ImageScale
            };
            return await SdApi(url, requestData, HttpMethod.Post);
        }

        public static async Task<string?> SAM(img2imgbatch model)
        {
            string url = $"{model.ApiUrl}/sam/sam-predict";
            var requestData = new
            {
                sam_model_name = "sam_vit_b_01ec64.pth",
                input_image = model.ImagePaths,
                dino_enabled = true,
                dino_model_name = "GroundingDINO_SwinT_OGC (694MB)",
                dino_text_prompt = model.MaskPrompt,
                dino_box_threshold = 0.3
            };
            return await SdApi(url, requestData, HttpMethod.Post);
        }

        public static async Task<string?> Amount(img2imgbatch model)
        {
            string url = $"{model.ApiUrl}/sam/dilate-mask";
            var requestData = new
            {
                input_image = model.ImagePaths,
                mask = model.MaskPaths,
                dilate_amount = 30
            };
            return await SdApi(url, requestData, HttpMethod.Post);
        }

        public static async Task<string?> SdApi(string url, object requestData, HttpMethod method)
        {
            var requestMessage = HttpHelpers.CreateHttpRequestMessage(method, url);
            requestMessage.Content = HttpHelpers.BodyString(requestData.ToJson());

            var response = await HttpHelpers.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                Ext.Info($"请求失败，状态码：{response.StatusCode}");
                return null;
            }
        }

        [SupportedOSPlatform("windows")]
        public static string ConvertImageToBase64(string imagePath)
        {
            try
            {
                using Image image = Image.FromFile(imagePath);
                using MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                Ext.Info($"错误：{ex.Message}");
                return string.Empty;
            }
        }

        public static int ExtractNumberFromFileName(string fileName)
        {
            // 匹配文件名中的数字部分
            var match = IsNumber().Match(fileName);
            return match.Success ? int.Parse(match.Value) : 0;
        }

        public class img2imgbatch
        {
            public required string ApiUrl { get; set; }
            public required string ImagePaths { get; set; }
            public required string MaskPaths { get; set; }
            public required string OutputDir { get; set; }
            public required string Prompt { get; set; }
            public  required string NegativePrompt { get; set; }
            public required double ImageScale { get; set; }

            public required string MaskPrompt { get; set; }

            public required string[] FilterImagePaths { get; set; }

            public bool Onlymask { get; set; } = false;
        }

        public class UploadFile
        {
            public required string FilePath { get; set; }
            public required string FileName { get; set; }
            public required string FileExtension { get; set; }

            public IFormFile? File { get; set; }
        }

        public class img2imgcopy
        {
            public required string ImagePaths { get; set; }

            public required string OutputDir { get; set; }
            public required string[] FilterImagePaths { get; set; }

        }

        [GeneratedRegex(@"\d+")]
        private static partial Regex IsNumber();
    }
}
