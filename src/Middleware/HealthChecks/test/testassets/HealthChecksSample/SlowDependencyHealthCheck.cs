// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecksSample;

// Simulates a health check for an application dependency that takes a while to initialize.
// This is part of the readiness/liveness probe sample.
public class SlowDependencyHealthCheck : IHealthCheck
{
    public static readonly string HealthCheckName = "slow_dependency";

    private readonly Task _task;

    public SlowDependencyHealthCheck()
    {
        _task = Task.Delay(15 * 1000);
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_task.IsCompleted)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Dependency is ready"));
        }

        return Task.FromResult(new HealthCheckResult(
            status: context.Registration.FailureStatus,
            description: "Dependency is still initializing"));
    }
}
