// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for adding <see cref="IRouteHandlerFilter"/> to a route handler.
/// </summary>
public static class RouteHandlerFilterExtensions
{
    /// <summary>
    /// Registers a filter onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="filter">The <see cref="IRouteHandlerFilter"/> to register.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static RouteHandlerBuilder AddFilter(this RouteHandlerBuilder builder, IRouteHandlerFilter filter)
    {
        builder.RouteHandlerFilterFactories.Add((routeHandlerContext, next) => (context) => filter.InvokeAsync(context, next));
        return builder;
    }

    /// <summary>
    /// Registers a filter of type <typeparamref name="TFilterType"/> onto the route handler.
    /// </summary>
    /// <typeparam name="TFilterType">The type of the <see cref="IRouteHandlerFilter"/> to register.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static RouteHandlerBuilder AddFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilterType>(this RouteHandlerBuilder builder) where TFilterType : IRouteHandlerFilter
    {
        var filterFactory = ActivatorUtilities.CreateFactory(typeof(TFilterType), Type.EmptyTypes);
        builder.RouteHandlerFilterFactories.Add((routeHandlerContext, next) => (context) =>
        {
            var filter = (IRouteHandlerFilter)filterFactory.Invoke(context.HttpContext.RequestServices, Array.Empty<object>());
            return filter.InvokeAsync(context, next);
        });
        return builder;
    }

    /// <summary>
    /// Registers a filter given a delegate onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="routeHandlerFilter">A <see cref="Delegate"/> representing the core logic of the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static RouteHandlerBuilder AddFilter(this RouteHandlerBuilder builder, Func<RouteHandlerInvocationContext, RouteHandlerFilterDelegate, ValueTask<object?>> routeHandlerFilter)
    {
        builder.RouteHandlerFilterFactories.Add((routeHandlerContext, next) => (context) => routeHandlerFilter(context, next));
        return builder;
    }

    /// <summary>
    /// Register a filter given a delegate representing the filter factory.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="filterFactory">A <see cref="Delegate"/> representing the logic for constructing the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static RouteHandlerBuilder AddFilter(this RouteHandlerBuilder builder, Func<RouteHandlerContext, RouteHandlerFilterDelegate, RouteHandlerFilterDelegate> filterFactory)
    {
        builder.RouteHandlerFilterFactories.Add(filterFactory);
        return builder;
    }
}
