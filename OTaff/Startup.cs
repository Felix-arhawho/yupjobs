using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OTaff.Lib.Money;
using Stripe;

namespace OTaff
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            StripeConfiguration.ApiKey = "sk_test_51HfgfpItUMuYVtWkTIn4tc7L5j2ScHmxFwbNu7W6o8s95NHGg1OvaRWvLm4qodYOkoF59DZRRxLVdDg05Y36EKWh00TU30w588";
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Task.Factory.StartNew(async () => {
                //Workers.BillWorker.Start = true;
                //Workers.SubscriptionsWorker.Start = true;
                //Workers.TransactionsWorker.Start = true;

                //Workers.TransactionsWorker.DoWork();
                //Workers.MediaCleanup.Cleanup();
                //Workers.BillWorker.ActionWork();
                //Workers.BillWorker.PayCheck();
                Workers.TagsWorker.DoWork();
                //Workers.StatsWorker.MoneyTask();
                Task.Run(CurrencyConversion.RatesWorker);
                //Workers.DbCleaner.DbClean();
            });

            //services.AddCors(options =>
            //{
            //    options.AddPolicy("CorsApi",
            //        builder => builder.WithOrigins("http://localhost:5000", "https://yupjobs.net")
            //    .AllowAnyHeader()
            //    .AllowAnyMethod());
            //});
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddCors(options => 
            {
                options.AddPolicy("AllowAll", p => p
                    .WithOrigins("http://localhost:5000", "http://localhost:15040", "https://www.yupjobs.net", "https://yupjobs.net")
                    .AllowAnyMethod()
                    .AllowAnyHeader());
                options.AddDefaultPolicy(new Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicy() { IsOriginAllowed = x => true });
            });

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddControllers();
            
            //MvcOptions.Ena
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (env.Dev)
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            app.Use(async (ctx, next)=>             
            {
                if (!await Controllers.DDoS.HandleRequest(ref ctx))
                    ctx.Abort();

                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Access-Control-Allow-Headers, Origin,Accept, X-Requested-With, Content-Type, Access-Control-Request-Method, Access-Control-Request-Headers");

                await next();
            });

            app.UseRouting();

            app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //app.UseMvc();
        }

    }
}
