// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.StaticAssets;

internal class StaticAssetEndpointFactory(IServiceProvider serviceProvider)
{
    private static readonly HttpMethodMetadata _supportedMethods = new([HttpMethods.Get, HttpMethods.Head]);

    public Endpoint Create(StaticAssetDescriptor resource, List<Action<EndpointBuilder>> conventions, List<Action<EndpointBuilder>> finallyConventions)
    {
        var routeEndpointBuilder = new RouteEndpointBuilder(
            null,
            RoutePatternFactory.Parse(resource.Route),
            // Static resources always take precedence over default routes to mimic the behavior of UseStaticFiles.
            // We give a -100 order to ensure that they are selected under normal circumstances, but leave a small lee-way
            // for the user to override this if they want to.
            -100);

        foreach (var selector in resource.Selectors)
        {
            switch (selector.Name)
            {
                case "Content-Encoding":
                    routeEndpointBuilder.Metadata.Add(new ContentEncodingMetadata(selector.Value, double.Parse(selector.Quality, CultureInfo.InvariantCulture)));
                    break;
                default:
                    break;
            }
        }

        var logger = serviceProvider.GetRequiredService<ILogger<StaticAssetsInvoker>>();
        var fileProvider = serviceProvider.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
        var invoker = new StaticAssetsInvoker(resource, fileProvider, logger);

        routeEndpointBuilder.RequestDelegate = invoker.Invoke;

        routeEndpointBuilder.Metadata.Add(resource);
        routeEndpointBuilder.Metadata.Add(_supportedMethods);

        foreach (var convention in conventions)
        {
            convention(routeEndpointBuilder);
        }

        foreach (var finallyConvention in finallyConventions)
        {
            finallyConvention(routeEndpointBuilder);
        }

        return routeEndpointBuilder.Build();
    }
}
