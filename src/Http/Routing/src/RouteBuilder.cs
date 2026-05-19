// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides support for specifying routes in an application.
/// </summary>
public class RouteBuilder : IRouteBuilder
{
    /// <summary>
    /// Constructs a new <see cref="RouteBuilder"/> instance given an <paramref name="applicationBuilder"/>.
    /// </summary>
    /// <param name="applicationBuilder">An <see cref="IApplicationBuilder"/> instance.</param>
    public RouteBuilder(IApplicationBuilder applicationBuilder)
        : this(applicationBuilder, defaultHandler: null)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="RouteBuilder"/> instance given an <paramref name="applicationBuilder"/>
    /// and <paramref name="defaultHandler"/>.
    /// </summary>
    /// <param name="applicationBuilder">An <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="defaultHandler">The default <see cref="IRouter"/> used if a new route is added without a handler.</param>
    public RouteBuilder(IApplicationBuilder applicationBuilder, IRouter? defaultHandler)
    {
        ArgumentNullException.ThrowIfNull(applicationBuilder);

        if (applicationBuilder.ApplicationServices.GetService(typeof(RoutingMarkerService)) == null)
        {
            throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                nameof(RoutingServiceCollectionExtensions.AddRouting),
                "ConfigureServices(...)"));
        }

        ApplicationBuilder = applicationBuilder;
        DefaultHandler = defaultHandler;
        ServiceProvider = applicationBuilder.ApplicationServices;

        Routes = new List<IRouter>();
    }

    /// <inheritdoc />
    public IApplicationBuilder ApplicationBuilder { get; }

    /// <inheritdoc />
    public IRouter? DefaultHandler { get; set; }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public IList<IRouter> Routes { get; }

    /// <inheritdoc />
    public IRouter Build()
    {
        var routeCollection = new RouteCollection();

        foreach (var route in Routes)
        {
            routeCollection.Add(route);
        }

        return routeCollection;
    }
}
