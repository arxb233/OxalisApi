using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using HKRM_Server_C.CommonBusiness;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Tool;
using Tool.SqlCore;
using Tool.Utils;
using Tool.Utils.Data;
using Tool.Web.Api;
using Tool.Web.Routing;

namespace OxalisApi.Controllers.Major
{
    public class Major : MinApi
    {
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> School(string year,string subject)
        {
            var dbHelper = DbBase.GetDynamicDb("MAJOR");
            var Proddict = await dbHelper.SelectDictionaryAsync($"select ROW_NUMBER() OVER (ORDER BY 最低分数线 Desc) AS id,学校,省,城市,区,是否985,是否211,是否双一流,录取批次,招生类型,选科要求,最低分数线,最低分段,办学性质 from T甘肃_投档线_2024 where 年份=N'{year}' and 科类=N'{subject}'");
            return new JsonOut(new { message = Proddict });
        }
        private async Task<IApiOut> GetLogoUrl([ApiVal(Val.Service)] IHttpClientFactory clientFactory, [ApiVal(Val.Service)] ILogger<Major> logger)
        {
            var _httpClient = clientFactory.CreateClient("Major");
            try
            {
                var respJson = await _httpClient.GetStringAsync("https://www.urongda.com/data/search.json");
                var list = JsonSerializer.Deserialize<List<SchoolEntry>>(respJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                List<Dictionary<string,object>?> base64DataUriList = [];
                var dbHelper = DbBase.GetDynamicDb("MAJOR");
                if (list is not null) {
                    foreach (var (index,match) in list.Index())
                    {
                        var slug = match?.EnName?.ToLower().Replace(" ", "-");
                        var logoUrl = $"https://cdn.urongda.com/images/normal/medium/{slug}-logo-1024px.png";

                        var request = new HttpRequestMessage(HttpMethod.Get, logoUrl);
                        request.Headers.Referrer = new Uri("https://www.urongda.com/");
                        var imgResp = await _httpClient.SendAsync(request);
                        if (!imgResp.IsSuccessStatusCode)
                        {
                            continue;
                        }
                        var imgBytes = await imgResp.Content.ReadAsByteArrayAsync();
                        var contentType = imgResp.Content.Headers.ContentType?.MediaType ?? "image/png";
                        var base64 = Convert.ToBase64String(imgBytes);
                        var base64DataUri = $"data:{contentType};base64,{base64}";
                        var dict = new { name = match?.Name, LogoUrl = base64DataUri };
                        base64DataUriList.Add(dict?.ToDictionary());
                        await dbHelper.InsertAsync("SchoolLogo", dict);
                    }
                }
                return new JsonOut(new { Message = base64DataUriList, Issuccess = true });
            }
            catch (Exception)
            {
                return new JsonOut(new { Message = "未找到学校", Issuccess = false });
            }
        }
    }
    public class SchoolEntry
    {
        public string? Name { get; set; }
        public string? EnName { get; set; }
    }
}
