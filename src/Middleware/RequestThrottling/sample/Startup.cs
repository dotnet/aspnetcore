using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RequestThrottling;


namespace RequestThrottlingSample
{
    public static class RequestThrottlingExtensions
    {
        public static IApplicationBuilder UseRequestThrottling(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<RequestThrottlingMiddleware>();
        }
    }
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Default");

            //var s = new SemaphoreWrapper(2);
            //int totalRequests = 0;

            //app.Use(async (context, next) =>
            //{
            //    totalRequests += 1;
            //    var localId = totalRequests;

            //    await s.EnterQueue();
            //    logger.LogDebug($"Received #{localId}");

            //    await next();

            //    s.LeaveQueue();
            //    logger.LogDebug($"#{localId} finished: {s.Count} spots left");
            //});


            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello world! <p></p>");
                await Task.Delay(3000);
            });
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory()) // for the cert file
                .ConfigureLogging(factory =>
                {
                    factory.SetMinimumLevel(LogLevel.Debug);
                    factory.AddConsole();
                })
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
