
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace IStartupInjectionAssemblyName
{
    public class Startup : IStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            var applicationName = app.ApplicationServices.GetRequiredService<IHostingEnvironment>().ApplicationName;
            app.Run(context =>
            {
                return context.Response.WriteAsync(applicationName);
            });
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
    }
}
