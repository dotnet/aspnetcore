// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class DbContextHealthCheck<TContext> : IHealthCheck where TContext : DbContext
{
    private static readonly Func<TContext, CancellationToken, Task<bool>> DefaultTestQuery = (dbContext, cancellationToken) =>
    {
        return dbContext.Database.CanConnectAsync(cancellationToken);
    };

    private readonly TContext _dbContext;
    private readonly IOptionsMonitor<DbContextHealthCheckOptions<TContext>> _options;

    public DbContextHealthCheck(TContext dbContext, IOptionsMonitor<DbContextHealthCheckOptions<TContext>> options)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(options);

        _dbContext = dbContext;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = _options.Get(context.Registration.Name);
        var testQuery = options.CustomTestQuery ?? DefaultTestQuery;

        try
        {
            if (await testQuery(_dbContext, cancellationToken))
            {
                return HealthCheckResult.Healthy();
            }

            return new HealthCheckResult(context.Registration.FailureStatus);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(exception.Message, exception);
        }
    }
}
