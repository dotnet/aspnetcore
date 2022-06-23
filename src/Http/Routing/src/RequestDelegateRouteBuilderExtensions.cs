// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides extension methods for adding new handlers to a <see cref="IRouteBuilder"/>.
/// </summary>
public static class RequestDelegateRouteBuilderExtensions
{
    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> for the given <paramref name="template"/>, and
    /// <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The <see cref="RequestDelegate"/> route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapRoute(this IRouteBuilder builder, [StringSyntax("Route")] string template, RequestDelegate handler)
    {
        var route = new Route(
            new RouteHandler(handler),
            template,
            defaults: null,
            constraints: null,
            dataTokens: null,
            inlineConstraintResolver: GetConstraintResolver(builder));

        builder.Routes.Add(route);
        return builder;
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> for the given <paramref name="template"/>, and
    /// <paramref name="action"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="action">The action to apply to the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapMiddlewareRoute(this IRouteBuilder builder, [StringSyntax("Route")] string template, Action<IApplicationBuilder> action)
    {
        var nested = builder.ApplicationBuilder.New();
        action(nested);
        return builder.MapRoute(template, nested.Build());
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP DELETE requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The <see cref="RequestDelegate"/> route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapDelete(this IRouteBuilder builder, [StringSyntax("Route")] string template, RequestDelegate handler)
    {
        return builder.MapVerb("DELETE", template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP DELETE requests for the given
    /// <paramref name="template"/>, and <paramref name="action"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="action">The action to apply to the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapMiddlewareDelete(this IRouteBuilder builder, [StringSyntax("Route")] string template, Action<IApplicationBuilder> action)
    {
        return builder.MapMiddlewareVerb("DELETE", template, action);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP DELETE requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapDelete(
        this IRouteBuilder builder,
        [StringSyntax("Route")] string template,
        Func<HttpRequest, HttpResponse, RouteData, Task> handler)
    {
        return builder.MapVerb("DELETE", template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP GET requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The <see cref="RequestDelegate"/> route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapGet(this IRouteBuilder builder, [StringSyntax("Route")] string template, RequestDelegate handler)
    {
        return builder.MapVerb(HttpMethods.Get, template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP GET requests for the given
    /// <paramref name="template"/>, and <paramref name="action"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="action">The action to apply to the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapMiddlewareGet(this IRouteBuilder builder, [StringSyntax("Route")] string template, Action<IApplicationBuilder> action)
    {
        return builder.MapMiddlewareVerb(HttpMethods.Get, template, action);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP GET requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapGet(
        this IRouteBuilder builder,
        [StringSyntax("Route")] string template,
        Func<HttpRequest, HttpResponse, RouteData, Task> handler)
    {
        return builder.MapVerb(HttpMethods.Get, template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP POST requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The <see cref="RequestDelegate"/> route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapPost(this IRouteBuilder builder, [StringSyntax("Route")] string template, RequestDelegate handler)
    {
        return builder.MapVerb(HttpMethods.Post, template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP POST requests for the given
    /// <paramref name="template"/>, and <paramref name="action"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="action">The action to apply to the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapMiddlewarePost(this IRouteBuilder builder, [StringSyntax("Route")] string template, Action<IApplicationBuilder> action)
    {
        return builder.MapMiddlewareVerb(HttpMethods.Post, template, action);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP POST requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapPost(
        this IRouteBuilder builder,
        [StringSyntax("Route")] string template,
        Func<HttpRequest, HttpResponse, RouteData, Task> handler)
    {
        return builder.MapVerb(HttpMethods.Post, template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP PUT requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The <see cref="RequestDelegate"/> route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapPut(this IRouteBuilder builder, [StringSyntax("Route")] string template, RequestDelegate handler)
    {
        return builder.MapVerb(HttpMethods.Put, template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP PUT requests for the given
    /// <paramref name="template"/>, and <paramref name="action"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="action">The action to apply to the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapMiddlewarePut(this IRouteBuilder builder, [StringSyntax("Route")] string template, Action<IApplicationBuilder> action)
    {
        return builder.MapMiddlewareVerb(HttpMethods.Put, template, action);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP PUT requests for the given
    /// <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapPut(
        this IRouteBuilder builder,
        [StringSyntax("Route")] string template,
        Func<HttpRequest, HttpResponse, RouteData, Task> handler)
    {
        return builder.MapVerb(HttpMethods.Put, template, handler);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP requests for the given
    /// <paramref name="verb"/>, <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="verb">The HTTP verb allowed by the route.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapVerb(
        this IRouteBuilder builder,
        string verb,
        [StringSyntax("Route")] string template,
        Func<HttpRequest, HttpResponse, RouteData, Task> handler)
    {
        RequestDelegate requestDelegate = (httpContext) =>
        {
            return handler(httpContext.Request, httpContext.Response, httpContext.GetRouteData());
        };

        return builder.MapVerb(verb, template, requestDelegate);
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP requests for the given
    /// <paramref name="verb"/>, <paramref name="template"/>, and <paramref name="handler"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="verb">The HTTP verb allowed by the route.</param>
    /// <param name="template">The route template.</param>
    /// <param name="handler">The <see cref="RequestDelegate"/> route handler.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapVerb(
        this IRouteBuilder builder,
        string verb,
        [StringSyntax("Route")] string template,
        RequestDelegate handler)
    {
        var constraints = new RouteValueDictionary
        {
            ["httpMethod"] = new HttpMethodRouteConstraint(verb),
        };

        var route = new Route(
            new RouteHandler(handler),
            template,
            defaults: null,
            constraints: constraints!,
            dataTokens: null,
            inlineConstraintResolver: GetConstraintResolver(builder));

        builder.Routes.Add(route);
        return builder;
    }

    /// <summary>
    /// Adds a route to the <see cref="IRouteBuilder"/> that only matches HTTP requests for the given
    /// <paramref name="verb"/>, <paramref name="template"/>, and <paramref name="action"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRouteBuilder"/>.</param>
    /// <param name="verb">The HTTP verb allowed by the route.</param>
    /// <param name="template">The route template.</param>
    /// <param name="action">The action to apply to the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>A reference to the <paramref name="builder"/> after this operation has completed.</returns>
    public static IRouteBuilder MapMiddlewareVerb(
        this IRouteBuilder builder,
        string verb,
        [StringSyntax("Route")] string template,
        Action<IApplicationBuilder> action)
    {
        var nested = builder.ApplicationBuilder.New();
        action(nested);
        return builder.MapVerb(verb, template, nested.Build());
    }

    private static IInlineConstraintResolver GetConstraintResolver(IRouteBuilder builder)
    {
        return builder.ServiceProvider.GetRequiredService<IInlineConstraintResolver>();
    }
}
