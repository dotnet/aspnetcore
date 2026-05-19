// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for adding <see cref="IEndpointFilter"/> to a route handler.
/// </summary>
public static class EndpointFilterExtensions
{
    /// <summary>
    /// Registers a filter onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="filter">The <see cref="IEndpointFilter"/> to register.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilter<TBuilder>(this TBuilder builder, IEndpointFilter filter) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilterFactory((routeHandlerContext, next) => (context) => filter.InvokeAsync(context, next));

    /// <summary>
    /// Registers a filter of type <typeparamref name="TFilterType"/> onto the route handler.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the <see cref="IEndpointConventionBuilder"/> to configure.</typeparam>
    /// <typeparam name="TFilterType">The type of the <see cref="IEndpointFilter"/> to register.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilter<TBuilder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilterType>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        where TFilterType : IEndpointFilter
    {
        // We call `CreateFactory` twice here since the `CreateFactory` API does not support optional arguments.
        // See https://github.com/dotnet/runtime/issues/67309 for more info.
        ObjectFactory filterFactory;
        try
        {
            filterFactory = ActivatorUtilities.CreateFactory(typeof(TFilterType), new[] { typeof(EndpointFilterFactoryContext) });
        }
        catch (InvalidOperationException)
        {
            filterFactory = ActivatorUtilities.CreateFactory(typeof(TFilterType), Type.EmptyTypes);
        }

        builder.AddEndpointFilterFactory((routeHandlerContext, next) =>
        {
            var invokeArguments = new[] { routeHandlerContext };
            return (context) =>
            {
                var filter = (IEndpointFilter)filterFactory.Invoke(context.HttpContext.RequestServices, invokeArguments);
                return filter.InvokeAsync(context, next);
            };
        });
        return builder;
    }

    /// <summary>
    /// Registers a filter of type <typeparamref name="TFilterType"/> onto the route handler.
    /// </summary>
    /// <typeparam name="TFilterType">The type of the <see cref="IEndpointFilter"/> to register.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static RouteHandlerBuilder AddEndpointFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilterType>(this RouteHandlerBuilder builder)
        where TFilterType : IEndpointFilter
    {
        // We have a RouteHandlerBuiler and GroupRouteBuilder-specific AddFilter methods for convenience so you don't have to specify both arguments most the time.
        return builder.AddEndpointFilter<RouteHandlerBuilder, TFilterType>();
    }

    /// <summary>
    /// Registers a filter of type <typeparamref name="TFilterType"/> onto the route handler.
    /// </summary>
    /// <typeparam name="TFilterType">The type of the <see cref="IEndpointFilter"/> to register.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static RouteGroupBuilder AddEndpointFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilterType>(this RouteGroupBuilder builder)
        where TFilterType : IEndpointFilter
    {
        // We have a RouteHandlerBuiler and GroupRouteBuilder-specific AddFilter methods for convenience so you don't have to specify both arguments most the time.
        return builder.AddEndpointFilter<RouteGroupBuilder, TFilterType>();
    }

    /// <summary>
    /// Registers a filter given a delegate onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="routeHandlerFilter">A method representing the core logic of the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilter<TBuilder>(this TBuilder builder, Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> routeHandlerFilter)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilterFactory((routeHandlerContext, next) => (context) => routeHandlerFilter(context, next));
    }

    /// <summary>
    /// Register a filter given a delegate representing the filter factory.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="filterFactory">A method representing the logic for constructing the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilterFactory<TBuilder>(this TBuilder builder, Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> filterFactory)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.FilterFactories.Add(filterFactory);
        });

        return builder;
    }
}
