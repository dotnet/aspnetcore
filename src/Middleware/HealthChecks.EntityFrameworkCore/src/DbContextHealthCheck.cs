// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
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
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _dbContext = dbContext;
            _options = options;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var options = _options.Get(context.Registration.Name);
            var testQuery = options.CustomTestQuery ?? DefaultTestQuery;

            if (await testQuery(_dbContext, cancellationToken))
            {
                return HealthCheckResult.Healthy();
            }
            
            return new HealthCheckResult(context.Registration.FailureStatus);
        }
    }
}
