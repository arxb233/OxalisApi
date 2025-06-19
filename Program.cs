using Quartz;
using System.Threading.Tasks;
using Tool.Web;
using OxalisApi.Job;
using HKRM_Server_C.CommonBusiness;

namespace OxalisApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.UseDiyServiceProvider();
            builder.Logging.AddLogSave();
            builder.Services.AddHttpClient();
            builder.Services.AddAshx(o=>o.EnableEndpointRouting = true);
            //builder.Services.AddControllers();
            // Add services to the container.
            builder.Services.AddAuthorization();

            builder.Services.AddQuartzJob<HayFrpJob>("HayFrpJob", "HayFrpJob-trigger", "0 20 08 * * ?"); //凌晨1点30分

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            
            app.UseRouting();
            app.UseAuthorization();
            app.MapAshxs();
            //app.MapControllers();
            DbModel.UseDbService();
            await app.RunAsync();
        }
    }

    public static class QuartzExtensions
    {
        public static void AddQuartzJob<T>(this IServiceCollection services, string jobName, string triggerName, string cronSchedule) where T : IJob
        {
            services.AddQuartz(q =>
            {
                // 直接使用默认的作业工厂
                var jobKey = new JobKey(jobName);
                q.AddJob<T>(opts => opts.WithIdentity(jobKey));

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity(triggerName)
                    .WithCronSchedule(cronSchedule));
            });
            DbModel.AddDbService(services);
            
            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        }
    }
}
