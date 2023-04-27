// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;

internal static class AsyncQueryExecutorSupplier
{
    // The primary goal with this is to ensure that:
    //  - If you're using EF Core, then we resolve queries efficiently using its ToXyzAsync async extensions and don't
    //    just fall back on the synchronous IQueryable ToXyz calls
    //  - ... but without QuickGrid referencing Microsoft.EntityFramework directly. That's because it would bring in
    //    heavy dependencies you may not be using (and relying on trimming isn't enough, as it's still desirable to have
    //    heavy unused dependencies for Blazor Server).
    //
    // As a side-effect, we have an abstraction IAsyncQueryExecutor that developers could use to plug in their own
    // mechanism for resolving async queries from other data sources than EF. It's not really a major goal to make this
    // adapter generally useful beyond EF, but fine if people do have their own uses for it.

    private static readonly ConcurrentDictionary<Type, bool> IsEntityFrameworkProviderTypeCache = new();

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111",
               Justification = "The reflection is a best effort to warn developers about sync-over-async behavior which can cause thread pool starvation.")]
    public static IAsyncQueryExecutor? GetAsyncQueryExecutor<T>(IServiceProvider services, IQueryable<T>? queryable)
    {
        if (queryable is not null)
        {
            var executor = services.GetService<IAsyncQueryExecutor>();

            if (executor is null)
            {
                // It's useful to detect if the developer is unaware that they should be using the EF adapter, otherwise
                // they will likely never notice and simply deploy an inefficient app that blocks threads on each query.
                var providerType = queryable.Provider?.GetType();
                if (providerType is not null && IsEntityFrameworkProviderTypeCache.GetOrAdd(providerType, IsEntityFrameworkProviderType))
                {
                    throw new InvalidOperationException($"The supplied {nameof(IQueryable)} is provided by Entity Framework. To query it efficiently, you must reference the package Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter and call AddQuickGridEntityFrameworkAdapter on your service collection.");
                }
            }
            else if (executor.IsSupported(queryable))
            {
                return executor;
            }
        }

        return null;
    }

    // We have to do this via reflection because the whole point is to avoid any static dependency on EF unless you
    // reference the adapter. Trimming won't cause us any problems because this is only a way of detecting misconfiguration
    // so it's sufficient if it can detect the misconfiguration in development.
    private static bool IsEntityFrameworkProviderType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type queryableProviderType)
        => queryableProviderType.GetInterfaces().Any(x => string.Equals(x.FullName, "Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider", StringComparison.Ordinal)) == true;
}
