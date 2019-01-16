using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HealthChecksSample
{
    // Pass in `--scenario basic` at the command line to run this sample.
    public class BasicStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Registers required services for health checks
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // This will register the health checks middleware at the URL /health.
            // 
            // By default health checks will return a 200 with 'Healthy'.
            // - No health checks are registered by default, the app is healthy if it is reachable
            // - The default response writer writes the HealthCheckStatus as text/plain content
            //
            // This is the simplest way to use health checks, it is suitable for systems
            // that want to check for 'liveness' of an application.
            app.UseHealthChecks("/health");

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Go to /health to see the health status");
            });
        }
    }
}
