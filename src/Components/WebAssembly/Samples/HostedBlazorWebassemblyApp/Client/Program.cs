using HostedBlazorWebassemblyApp.Client.Data;
using HostedBlazorWebassemblyApp.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HostedBlazorWebassemblyApp.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.DynamicComponentDefinitions.Register<CoolCounter>("my-cool-counter");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<IWeatherForecastService, HttpWeatherForecastService>();

            await builder.Build().RunAsync();
        }
    }
}
