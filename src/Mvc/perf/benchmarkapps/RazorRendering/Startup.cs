using System;
using System.Linq;
using System.Collections.Generic;
using Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Configuration;
using System.IO;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<List<DataA>>(_ => DataA);
        services.AddScoped<List<DataB>>(_ => DataB);
        services.AddMvc();
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapRazorPages();
        });
    }

    private static List<DataA> DataA = GenerateDataA();

    private static List<DataA> GenerateDataA()
    {
        var dataA = new List<DataA>();

        foreach (var i in Enumerable.Range(0, 100))
        {
            dataA.Add(new DataA(i, new HtmlString(i.ToString()), new HtmlString(i.ToString()), i.ToString(), i, i, 60f / i));
        }

        return dataA;
    }

    private static List<DataB> DataB = GenerateDataB();

    private static List<DataB> GenerateDataB()
    {
        var utc = DateTimeOffset.UtcNow;
        var dataB = new List<DataB>();
        foreach (var i in Enumerable.Range(0, 100))
        {
            dataB.Add(new DataB(i, new HtmlString(i.ToString()), i.ToString(), i, utc, utc));
        }

        return dataB;
    }

    public static void Main(string[] args)
    {
        var host = CreateWebHostBuilder(args)
            .Build();

        host.Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        return new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://+:5000")
            .UseConfiguration(configuration)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseStartup<Startup>();
    }
}
