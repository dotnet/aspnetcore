using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecksSample
{
    // Pass in `--scenario detailed` at the command line to run this sample.
    public class DetailedStatusStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Registers required services for health checks
            services
                .AddHealthChecks()

                // Registers a custom health check, in this case it will execute an
                // inline delegate.
                .AddCheck("GC Info", () =>
                {
                    // This example will report degraded status if the application is using
                    // more than 1gb of memory.
                    //
                    // Additionally we include some GC info in the reported diagnostics.
                    var allocated = GC.GetTotalMemory(forceFullCollection: false);
                    var data = new Dictionary<string, object>()
                    {
                        { "Allocated", allocated },
                        { "Gen0Collections", GC.CollectionCount(0) },
                        { "Gen1Collections", GC.CollectionCount(1) },
                        { "Gen2Collections", GC.CollectionCount(2) },
                    };

                    // Report degraded status if the allocated memory is >= 1gb (in bytes)
                    var status = allocated >= 1024 * 1024 * 1024 ? HealthCheckStatus.Degraded : HealthCheckStatus.Healthy;

                    return Task.FromResult(new HealthCheckResult(
                        status, 
                        exception: null,
                        description: "reports degraded status if allocated bytes >= 1gb",
                        data: data));
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // This will register the health checks middleware at the URL /health
            // 
            // This example overrides the ResponseWriter to include a detailed
            // status as JSON. Use this response writer (or create your own) to include
            // detailed diagnostic information for use by a monitoring system.
            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                ResponseWriter = HealthCheckResponseWriters.WriteDetailedJson,
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Go to /health to see the health status");
            });
        }
    }
}
