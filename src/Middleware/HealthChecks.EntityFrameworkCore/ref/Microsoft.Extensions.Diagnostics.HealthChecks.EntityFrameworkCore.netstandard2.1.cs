// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EntityFrameworkCoreHealthChecksBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddDbContextCheck<TContext>(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name = null, Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus = default(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus?), System.Collections.Generic.IEnumerable<string> tags = null, System.Func<TContext, System.Threading.CancellationToken, System.Threading.Tasks.Task<bool>> customTestQuery = null) where TContext : Microsoft.EntityFrameworkCore.DbContext { throw null; }
    }
}
