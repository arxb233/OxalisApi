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
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> GetLogoUrl([ApiVal(Val.Service)] IHttpClientFactory clientFactory, [ApiVal(Val.Service)] ILogger<Major> logger,string name)
        {
            var dbHelper = DbBase.GetDynamicDb("MAJOR");
            var Proddict = await dbHelper.SelectDictionaryAsync($"select name,logourl from SchoolLogo where name=N'{name}'");
            if (Proddict.Count == 0) {
                return new JsonOut(new { message = "没找到学校", issuccess = false });
            }
            return new JsonOut(new { message = Proddict[0]["logourl"], issuccess = true });
        }
    }
    public class SchoolEntry
    {
        public string? Name { get; set; }
        public string? EnName { get; set; }
    }
}
