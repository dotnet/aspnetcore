using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups
{
    public class StartupExternallyControlled
    {
        private IWebHost _host;
        private readonly List<string> _urls = new List<string>();

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        public StartupExternallyControlled()
        {
        }

        public void Start()
        {
            _host = new WebHostBuilder()
                    .UseStartup<StartupExternallyControlled>()
                    .Start(_urls.ToArray());
        }

        public void Stop()
        {
            _host.Dispose();
        }

        public void AddUrl(string url)
        {
            _urls.Add(url);
        }
    }
}
