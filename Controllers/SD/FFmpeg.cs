using Microsoft.AspNetCore.Mvc;
using Tool.Web.Api;
using OxalisApi.CommonBusiness;

namespace OxalisApi.Controllers.SD
{
    public class FFmpeg : MinApi
    {
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> Split(string file,string ffmpegPath)
        {
            string arguments = $"-y -i \"{file}\\video.mp4\" -vf \"fps=10\" \"{file}\\img\\%03d.png\"";
            await FFMpegWrapper.RunFFMpegCommand(arguments, ffmpegPath);
            return ApiOut.Write("执行完成！");
        }

        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> Merge(string file, string ffmpegPath)
        {
            string arguments = $"-y -framerate 10 -i \"{file}\\out\\%03d.png\" -c:v libx264 -r 10 -g 10 -crf 18 -profile:v high -level 4.2 -pix_fmt yuv420p \"{file}\\output_video.mp4\"";
            await FFMpegWrapper.RunFFMpegCommand(arguments, ffmpegPath);
            return ApiOut.Write("执行完成！");
        }

        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> Bgm(string file, string ffmpegPath)
        {
            string arguments = $"-y -i \"{file}\\video.mp4\" -q:a 0 -map a \"{file}\\bgm.mp3\"";
            await FFMpegWrapper.RunFFMpegCommand(arguments, ffmpegPath);

            return ApiOut.Write("执行完成！");
        }

        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> Finished(string file, string ffmpegPath)
        {
            string arguments = $"-y -i \"{file}\\output_video.mp4\" -i \"{file}\\bgm.mp3\" -c:v copy -map 0:v:0 -map 1:a:0 -shortest \"{file}\\output_video_bgm.mp4\"";
            await FFMpegWrapper.RunFFMpegCommand(arguments, ffmpegPath);
            return ApiOut.Write("执行完成！");
        }
    }
}
