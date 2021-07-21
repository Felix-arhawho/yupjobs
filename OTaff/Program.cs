using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OTaff.Lib.Money;

namespace OTaff
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var builder = 
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseKestrel(x=> {
                    x.ListenAnyIP(15040);
                    x.Limits.MaxRequestBodySize = 52428800; //50MB
                });
    }
}
