// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

public static class RazorComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapRazorComponents(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        EnsureRazorComponentServices(endpoints);

        return GetOrCreateDataSource(endpoints).DefaultBuilder;
    }

    private static RazorComponentEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder endpoints)
    {
        var dataSource = endpoints.DataSources.OfType<RazorComponentEndpointDataSource>().FirstOrDefault();
        if (dataSource == null)
        {
            dataSource = endpoints.ServiceProvider.GetRequiredService<RazorComponentEndpointDataSource>();
            endpoints.DataSources.Add(dataSource);
        }

        return dataSource;
    }

    private static void EnsureRazorComponentServices(IEndpointRouteBuilder endpoints)
    {
        // TODO: Check that `AddRazorComponents` has been called using a marker service, like MVC.
    }
}
