using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Tool.Web.Api;

namespace OxalisApi.Controllers
{
    public class HayFrp : MinApi
    {
        public HayFrp()
        {
        }

        [Ashx(State = AshxState.Get)]
        public async Task<IApiOut> Split(string file)
        {
            return await ApiOut.JsonAsync(file);
        }

    }
}
