// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
        AddBlazorWebJsEndpoint(endpoints);
        OpaqueRedirection.AddBlazorOpaqueRedirectionEndpoint(endpoints);

        var result = GetOrCreateDataSource<TRootComponent>(endpoints).DefaultBuilder;

        // Setup the convention to find the list of descriptors in the endpoint builder and
        // populate a resource collection out of them.
        // The user can call WithStaticAssets with a manifest path to override the manifest
        // to use for the resource collection in case more than one has been mapped.
        result.WithStaticAssets();

        return result;
    }

    private static void AddBlazorWebJsEndpoint(IEndpointRouteBuilder endpoints)
    {
        var options = new StaticFileOptions
        {
            FileProvider = new ManifestEmbeddedFileProvider(typeof(RazorComponentsEndpointRouteBuilderExtensions).Assembly),
            OnPrepareResponse = CacheHeaderSettings.SetCacheHeaders
        };

        var app = endpoints.CreateApplicationBuilder();
        app.Use(next => context =>
        {
            // Set endpoint to null so the static files middleware will handle the request.
            context.SetEndpoint(null);

            return next(context);
        });
        app.UseStaticFiles(options);

        var blazorEndpoint = endpoints.Map("/_framework/blazor.web.js", app.Build())
            .WithDisplayName("Blazor web static files");

        blazorEndpoint.Add((builder) => ((RouteEndpointBuilder)builder).Order = int.MinValue);

#if DEBUG
        // We only need to serve the sourcemap when working on the framework, not in the distributed packages
        endpoints.Map("/_framework/blazor.web.js.map", app.Build())
            .WithDisplayName("Blazor web static files sourcemap")
            .Add((builder) => ((RouteEndpointBuilder)builder).Order = int.MinValue);
#endif
    }

    private static RazorComponentEndpointDataSource<TRootComponent> GetOrCreateDataSource<[DynamicallyAccessedMembers(Component)] TRootComponent>(
        IEndpointRouteBuilder endpoints)
    {
        var dataSource = endpoints.DataSources.OfType<RazorComponentEndpointDataSource<TRootComponent>>().FirstOrDefault();
        if (dataSource == null)
        {
            // Very likely this needs to become a factory and we might need to have multiple endpoint data
            // sources, once we figure out the exact scenarios for
            // https://github.com/dotnet/aspnetcore/issues/46992
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
