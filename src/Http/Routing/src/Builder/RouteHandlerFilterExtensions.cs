// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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

    [RequiresUnreferencedCode(EndpointRouteBuilderExtensions.MapEndpointTrimmerWarning)]
    public static TBuilder AddRouteHandlerFilter<TBuilder>(this TBuilder builder, IRouteHandlerFilter filter) where TBuilder : IEndpointConventionBuilder =>
        builder.AddRouteHandlerFilter((routeHandlerContext, next) => (context) => filter.InvokeAsync(context, next));

    /// <summary>
    /// Registers a filter of type <typeparamref name="TFilterType"/> onto the route handler.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the <see cref="IEndpointConventionBuilder"/> to configure.</typeparam>
    /// <typeparam name="TFilterType">The type of the <see cref="IRouteHandlerFilter"/> to register.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    [RequiresUnreferencedCode(EndpointRouteBuilderExtensions.MapEndpointTrimmerWarning)]
    public static TBuilder AddRouteHandlerFilter<TBuilder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilterType>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        where TFilterType : IRouteHandlerFilter
    {
        // We call `CreateFactory` twice here since the `CreateFactory` API does not support optional arguments.
        // See https://github.com/dotnet/runtime/issues/67309 for more info.
        ObjectFactory filterFactory;
        try
        {
            filterFactory = ActivatorUtilities.CreateFactory(typeof(TFilterType), new[] { typeof(RouteHandlerContext) });
        }
        catch (InvalidOperationException)
        {
            filterFactory = ActivatorUtilities.CreateFactory(typeof(TFilterType), Type.EmptyTypes);
        }

        builder.AddRouteHandlerFilter((routeHandlerContext, next) =>
        {
            var invokeArguments = new[] { routeHandlerContext };
            return (context) =>
            {
                var filter = (IRouteHandlerFilter)filterFactory.Invoke(context.HttpContext.RequestServices, invokeArguments);
                return filter.InvokeAsync(context, next);
            };
        });
        return builder;
    }

    /// <summary>
    /// Registers a filter of type <typeparamref name="TFilterType"/> onto the route handler.
    /// </summary>
    /// <typeparam name="TFilterType">The type of the <see cref="IRouteHandlerFilter"/> to register.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    [RequiresUnreferencedCode(EndpointRouteBuilderExtensions.MapEndpointTrimmerWarning)]
    public static RouteHandlerBuilder AddRouteHandlerFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilterType>(this RouteHandlerBuilder builder)
        where TFilterType : IRouteHandlerFilter
    {
        // We have a RouteHandlerBuiler and GroupRouteBuilder-specific AddFilter methods for convenience so you don't have to specify both arguments most the time.
        return builder.AddRouteHandlerFilter<RouteHandlerBuilder, TFilterType>();
    }

    /// <summary>
    /// Registers a filter of type <typeparamref name="TFilterType"/> onto the route handler.
    /// </summary>
    /// <typeparam name="TFilterType">The type of the <see cref="IRouteHandlerFilter"/> to register.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    [RequiresUnreferencedCode(EndpointRouteBuilderExtensions.MapEndpointTrimmerWarning)]
    public static RouteGroupBuilder AddRouteHandlerFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilterType>(this RouteGroupBuilder builder)
        where TFilterType : IRouteHandlerFilter
    {
        // We have a RouteHandlerBuiler and GroupRouteBuilder-specific AddFilter methods for convenience so you don't have to specify both arguments most the time.
        return builder.AddRouteHandlerFilter<RouteGroupBuilder, TFilterType>();
    }

    /// <summary>
    /// Registers a filter given a delegate onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="routeHandlerFilter">A method representing the core logic of the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    [RequiresUnreferencedCode(EndpointRouteBuilderExtensions.MapEndpointTrimmerWarning)]
    public static TBuilder AddRouteHandlerFilter<TBuilder>(this TBuilder builder, Func<RouteHandlerInvocationContext, RouteHandlerFilterDelegate, ValueTask<object?>> routeHandlerFilter)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddRouteHandlerFilter((routeHandlerContext, next) => (context) => routeHandlerFilter(context, next));
    }

    /// <summary>
    /// Register a filter given a delegate representing the filter factory.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="filterFactory">A method representing the logic for constructing the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the route handler.</returns>
    [RequiresUnreferencedCode(EndpointRouteBuilderExtensions.MapEndpointTrimmerWarning)]
    public static TBuilder AddRouteHandlerFilter<TBuilder>(this TBuilder builder, Func<RouteHandlerContext, RouteHandlerFilterDelegate, RouteHandlerFilterDelegate> filterFactory)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            if (endpointBuilder is not RouteEndpointBuilder routeEndpointBuilder)
            {
                return;
            }

            routeEndpointBuilder.RouteHandlerFilterFactories ??= new();
            routeEndpointBuilder.RouteHandlerFilterFactories.Add(filterFactory);
        });

        return builder;
    }
}
