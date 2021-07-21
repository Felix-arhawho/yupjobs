using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Blazored.LocalStorage;
using BlazorApp.Shared.Components;
using Tewr.Blazor.FileReader;
using Microsoft.AspNetCore.Components;
using Blazored.Modal;

namespace BlazorApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<ToastService>();
            builder.Services.AddFileReaderService(o => o.UseWasmSharedBuffer = true);
            builder.Services.AddBlazoredModal();

            //builder.Services.AddTransient(sp => SharedLib.Lib.Ez.HttpClient = new HttpClient(new WebAssemblyHttpHandler())
            //{
            //    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
            //});

            await builder.Build().RunAsync();
        }

        
    }
}
