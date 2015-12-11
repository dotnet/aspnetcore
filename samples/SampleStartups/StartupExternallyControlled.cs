using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups
{
    public class StartupExternallyControlled
    {
        private readonly IWebApplication _host;
        private IDisposable _application;

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
            _host = new WebApplicationBuilder().UseStartup<StartupExternallyControlled>().Build();

            // Clear all configured addresses
            _host.GetAddresses().Clear();
        }

        public void Start()
        {
            _application = _host.Start();
        }

        public void Stop()
        {
            _application.Dispose();
        }

        public void AddUrl(string url)
        {
            var addresses = _host.GetAddresses();

            addresses.Add(url);
        }
    }
}
