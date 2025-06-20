using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using HKRM_Server_C.CommonBusiness;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient.DataClassification;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Tool;
using Tool.SqlCore;
using Tool.Utils;
using Tool.Web.Api;
using Tool.Web.Routing;
using static HKRM_Server_C.CommonBusiness.VueClass;
using static System.Net.Mime.MediaTypeNames;

namespace OxalisApi.Controllers.Major
{
    public class Major : MinApi
    {
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> School(string year, string subject, string is985, string pici, string province, string prefecture, string county)
        {
            var dbHelper = DbBase.GetDynamicDb("MAJOR");
            string str = $"select ROW_NUMBER() OVER (ORDER BY 最低分数线 Desc) AS id,学校,省,城市,区,是否985,是否211,是否双一流,录取批次,招生类型,选科要求,最低分数线,最低分段,办学性质 from T甘肃_投档线_2024 where 年份=N'{year}' and 科类=N'{subject}'";
            if (!string.IsNullOrWhiteSpace(is985) && is985 != "全部") { str += $" and 是否985=N'{is985}'"; }
            if (!string.IsNullOrWhiteSpace(pici) && pici != "全部") { str += $" and 录取批次=N'{pici}'"; }
            if (!string.IsNullOrWhiteSpace(province) && province != "全部") { str += $" and 省=N'{province}'"; }
            if (!string.IsNullOrWhiteSpace(prefecture) && prefecture != "全部") { str += $" and 城市=N'{prefecture}'"; }
            if (!string.IsNullOrWhiteSpace(county) && county != "全部") { str += $" and 区=N'{county}'"; }
            var Proddict = await dbHelper.SelectDictionaryAsync(str);
            return new JsonOut(new { message = Proddict });
        }
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> GetLogoUrl([ApiVal(Val.Service)] IHttpClientFactory clientFactory, [ApiVal(Val.Service)] ILogger<Major> logger, string name)
        {
            var dbHelper = DbBase.GetDynamicDb("MAJOR");
            var Proddict = await dbHelper.SelectDictionaryAsync($"select name,logourl from SchoolLogo where name=N'{name}'");
            if (Proddict.Count == 0) { return new JsonOut(new { message = "没找到学校", issuccess = true }); }
            return new JsonOut(new { message = Proddict[0]["logourl"], issuccess = true });
        }
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> GetPici()
        {
            var dbHelper = DbBase.GetDynamicDb("MAJOR");
            var Proddict = await dbHelper.SelectDictionaryAsync($"select DISTINCT 录取批次 from T甘肃_投档线_2024 order by 录取批次");
            if (Proddict.Count == 0) { return new JsonOut(new { message = "没找到批次", issuccess = true }); }
            List<string?> datas = [];
            foreach (var (index, item) in Proddict.Index())
            {
                if (index == 0) { datas.Add("全部"); }
                datas.Add(item["录取批次"]?.ToString());
            }
            return new JsonOut(new { message = datas, issuccess = true });
        }
        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> GetCitySchool()
        {
            var dbHelper = DbBase.GetDynamicDb("MAJOR");
            var Proddict = await dbHelper.SelectDictionaryAsync($"SELECT DISTINCT 省, 城市, 区 FROM T甘肃_投档线_2024 order by 省, 城市, 区");
            if (Proddict.Count == 0)
            {
                return new JsonOut(new { message = "没找到城市", issuccess = true });
            }
            var result = new List<VanPicker>();
            var groupedByProvince = Proddict.GroupBy(r => r["省"]);
            foreach (var (provinceindex, provinceGroup) in groupedByProvince.Index())
            {
                if (provinceindex == 0) { result.Add(new VanPicker("全部", [new VanPicker("全部", [new VanPicker("全部", null)])])); }
                var provinceNode = new VanPicker(provinceGroup.Key.ToString(), [new VanPicker("全部", [new VanPicker("全部",null )])]);
                var provinceNodeNew = provinceNode;
                var groupedByCity = provinceGroup.GroupBy(r => r["城市"]);
                foreach (var (Cityindex, cityGroup) in groupedByCity.Index())
                {
                    var cityselect = cityGroup.Select(r => r is null ? null : new VanPicker(r["区"]?.ToString(),null)).ToList();
                    if (Cityindex == 0){cityselect.Insert(0,new VanPicker("全部", null));}
                    var cityNode = new VanPicker(cityGroup.Key.ToString(), [.. cityselect]);
                    provinceNode.Children?.Add(cityNode);
                }
                result.Add(provinceNode);
            }
            return new JsonOut(new { message = result, issuccess = true });
        }
    }
    public class SchoolEntry
    {
        public string? Name { get; set; }
        public string? EnName { get; set; }
    }
}
