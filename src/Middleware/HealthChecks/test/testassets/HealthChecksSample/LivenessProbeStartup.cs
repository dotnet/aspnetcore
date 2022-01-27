// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace HealthChecksSample;

// Pass in `--scenario liveness` at the command line to run this sample.
public class LivenessProbeStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Registers required services for health checks
        services
            .AddHealthChecks()
            .AddCheck<SlowDependencyHealthCheck>("Slow", failureStatus: null, tags: new[] { "ready", });
    }

    public void Configure(IApplicationBuilder app)
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
        // In this example, the readiness check will run all registered checks, include a check with a 
        // long initialization time (15 seconds).

        // The readiness check uses all registered checks with the 'ready' tag.
        app.UseHealthChecks("/health/ready", new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains("ready"),
        });

        // The liveness filters out all checks and just returns success
        app.UseHealthChecks("/health/live", new HealthCheckOptions()
        {
            // Exclude all checks, just return a 200.
            Predicate = (check) => false,
        });

        app.Run(async (context) =>
        {
            await context.Response.WriteAsync("Go to /health/ready to see the readiness status");
            await context.Response.WriteAsync(Environment.NewLine);
            await context.Response.WriteAsync("Go to /health/live to see the liveness status");
        });
    }
}
