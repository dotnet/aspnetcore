// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions to <see cref="IEndpointRouteBuilder"/> for razor component applications.
/// </summary>
public static class RazorComponentsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the page components defined in the specified <typeparamref name="TRootComponent"/> to the given assembly
    /// and renders the component specified by <typeparamref name="TRootComponent"/> when the route matches.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <returns>An <see cref="RazorComponentsEndpointConventionBuilder"/> that can be used to further configure the API.</returns>
    public static RazorComponentsEndpointConventionBuilder MapRazorComponents<[DynamicallyAccessedMembers(Component)] TRootComponent>(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        EnsureRazorComponentServices(endpoints);

        var result = GetOrCreateDataSource<TRootComponent>(endpoints).DefaultBuilder;

        // Setup the convention to find the list of descriptors in the endpoint builder and
        // populate a resource collection out of them.
        // The user can call WithStaticAssets with a manifest path to override the manifest
        // to use for the resource collection in case more than one has been mapped.
        result.WithStaticAssets();

        return result;
    }

    private static RazorComponentEndpointDataSource<TRootComponent> GetOrCreateDataSource<[DynamicallyAccessedMembers(Component)] TRootComponent>(
        IEndpointRouteBuilder endpoints)
    {
        var dataSource = endpoints.DataSources.OfType<RazorComponentEndpointDataSource<TRootComponent>>().FirstOrDefault();
        if (dataSource == null)
        {
            var factory = endpoints.ServiceProvider.GetRequiredService<RazorComponentEndpointDataSourceFactory>();
            dataSource = factory.CreateDataSource<TRootComponent>(endpoints);
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
