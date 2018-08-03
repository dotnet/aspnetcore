using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecksSample
{
    // This is an example of a custom health check that implements IHealthCheck.
    //
    // See CustomWriterStartup to see how this is registered.
    public class GCInfoHealthCheck : IHealthCheck
    {
        public string Name { get; } = "GCInfo";

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default(CancellationToken))
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
        }
    }
}
