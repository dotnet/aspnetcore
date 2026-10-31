// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class DbContextHealthCheckOptions<TContext> where TContext : DbContext
{
    public Func<TContext, CancellationToken, Task<bool>>? CustomTestQuery { get; set; }
}
