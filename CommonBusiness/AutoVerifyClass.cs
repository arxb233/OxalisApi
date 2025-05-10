using System.Drawing;
using System.Runtime.Versioning;
using Tesseract;

namespace OxalisApi.CommonBusiness
{
    [SupportedOSPlatform("windows")]
    public class AutoVerifyClass
    {
        // 将Base64编码转换为图像
        public static Bitmap ConvertBase64ToImage(string base64)
        {
            byte[] imageBytes = Convert.FromBase64String(base64);
            using MemoryStream ms = new(imageBytes);
            return new Bitmap(ms);
        }

        // 下载并缓存 Tesseract 训练数据
        public static string DownloadAndCacheTesseractData()
        {
            return @"./tessdata";
        }

        // 使用Tesseract进行OCR识别
        public static string PerformOCR(Bitmap image, string tessDataPath)
        {
            string language = "eng"; // 英文训练数据

            // 将Bitmap转换为Pix
            using var pix = ConvertBitmapToPix(image);
            // 使用Tesseract进行OCR识别
            using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
            using var page = engine.Process(pix);
            return page.GetText(); // 获取识别的文本
        }

        // 将Bitmap转换为Pix
        public static Pix ConvertBitmapToPix(Bitmap bitmap)
        {
            // 使用Tesseract.Pix.LoadFromMemory将Bitmap转换为Pix对象
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            return Pix.LoadFromMemory(ms.ToArray());
        }
    }
}
