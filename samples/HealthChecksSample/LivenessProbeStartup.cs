using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecksSample
{
    // Pass in `--scenario liveness` at the command line to run this sample.
    public class LivenessProbeStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Registers required services for health checks
            services
                .AddHealthChecks()
                .AddCheck("identity", () => Task.FromResult(HealthCheckResult.Healthy()))
                .AddCheck(new SlowDependencyHealthCheck());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // This will register the health checks middleware twice:
            // - at /health/ready for 'readiness'
            // - at /health/live for 'liveness'
            //
            // Using a separate liveness and readiness check is useful in an environment like Kubernetes
            // when an application needs to do significant work before accepting requests. Using separate
            // checks allows the orchestrator to distinguish whether the application is functioning but 
            // not yet ready or if the application has failed to start.
            //
            // For instance the liveness check will do a quick set of checks to determine if the process
            // is functioning correctly.
            //
            // The readiness check might do a set of more expensive or time-consuming checks to determine
            // if all other resources are responding.
            //
            // See https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-probes/ for
            // more details about readiness and liveness probes in Kubernetes.
            //
            // In this example, the liveness check will us an 'identity' check that always returns healthy.
            //
            // In this example, the readiness check will run all registered checks, include a check with an 
            // long initialization time (15 seconds).


            // The readiness check uses all of the registered health checks (default)
            app.UseHealthChecks("/health/ready");

            // The liveness check uses an 'identity' health check that always returns healty
            app.UseHealthChecks("/health/live", new HealthCheckOptions()
            {
                // Filters the set of health checks run by this middleware
                HealthCheckNames =
                {
                    "identity",
                },
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Go to /health/ready to see the readiness status");
                await context.Response.WriteAsync(Environment.NewLine);
                await context.Response.WriteAsync("Go to /health/live to see the liveness status");
            });
        }
    }
}
