using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HealthChecksSample
{
    // Pass in `--scenario writer` at the command line to run this sample.
    public class CustomWriterStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Registers required services for health checks
            services.AddHealthChecks();

            // This is an example of registering a custom health check as a service.
            // All IHealthCheck services will be available to the health check service and
            // middleware.
            //
            // We recommend registering all health checks as Singleton services.
            services.AddSingleton<IHealthCheck, GCInfoHealthCheck>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // This will register the health checks middleware at the URL /health
            // 
            // This example overrides the HealthCheckResponseWriter to write the health
            // check result in a totally custom way.
            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                // This custom writer formats the detailed status as an HTML table.
                ResponseWriter = WriteResponse,
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Go to /health to see the health status");
            });
        }

        private static Task WriteResponse(HttpContext httpContext, CompositeHealthCheckResult result)
        {
            httpContext.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Results.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(p => new JProperty(p.Key, p.Value))))))))));
            return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}
