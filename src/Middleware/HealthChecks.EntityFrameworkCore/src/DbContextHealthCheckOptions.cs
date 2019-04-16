// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    internal sealed class DbContextHealthCheckOptions<TContext> where TContext : DbContext
    {
        public Func<TContext, CancellationToken, Task<bool>> CustomTestQuery { get; set; }
    }
}
