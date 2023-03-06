// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 
/// </summary>
public static class RazorComponentsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpoints"></param>
    /// <returns></returns>
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
            // Very likely this needs to become a factory and we might need to have multiple endpoint data
            // sources, once we figure out the exact scenarios for
            // https://github.com/dotnet/aspnetcore/issues/46992
            dataSource = endpoints.ServiceProvider.GetRequiredService<RazorComponentEndpointDataSource>();
            endpoints.DataSources.Add(dataSource);
        }

        return dataSource;
    }

    private static void EnsureRazorComponentServices(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var marker = endpoints.ServiceProvider.GetService<RazorComponentsMarkerService>();
        if (marker == null)
        {
            throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                nameof(RazorComponentsServiceCollectionExtensions.AddRazorComponents)));
        }
    }
}
