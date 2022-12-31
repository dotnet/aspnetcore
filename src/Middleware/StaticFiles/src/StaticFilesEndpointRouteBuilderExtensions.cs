// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Contains extension methods for using static files with endpoint routing.
/// </summary>
public static class StaticFilesEndpointRouteBuilderExtensions
{
    // By explicitly stating the supported HTTP methods for static files,
    // we limit the types of situations where the fallback to file is matched
    // after an endpoint is discarded, for example, due to a mismatched content type.
    // See: https://github.com/dotnet/aspnetcore/issues/41060
    private static readonly string[] _supportedHttpMethods = new[] { HttpMethods.Get, HttpMethods.Head };

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// requests for non-filenames with the lowest possible priority. The request will be routed to a
    /// <see cref="StaticFileMiddleware"/> that attempts to serve the file specified by <paramref name="filePath"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="filePath">The file path of the file to serve.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/></returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallbackToFile(IEndpointRouteBuilder, string)"/> is intended to handle cases where URL path of
    /// the request does not contain a filename, and no other endpoint has matched. This is convenient for routing
    /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
    /// result in an HTTP 404.
    /// </para>
    /// <para>
    /// The default <see cref="StaticFileOptions"/> for the <see cref="StaticFileMiddleware"/> will be used.
    /// </para>
    /// <para>
    /// <see cref="MapFallbackToFile(IEndpointRouteBuilder, string)"/> registers an endpoint using the pattern
    /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// </remarks>
    [UnconditionalSuppressMessage("Trimmer", "IL2026",
        Justification = "MapFallbackToFile RequireUnreferencedCode if the RequestDelegate has a Task<T> return type which is not the case here.")]
    public static IEndpointConventionBuilder MapFallbackToFile(
        this IEndpointRouteBuilder endpoints,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(filePath);

        return endpoints
            .MapFallback(CreateRequestDelegate(endpoints, filePath))
            .WithMetadata(new HttpMethodMetadata(_supportedHttpMethods));
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// requests for non-filenames with the lowest possible priority. The request will be routed to a
    /// <see cref="StaticFileMiddleware"/> that attempts to serve the file specified by <paramref name="filePath"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="filePath">The file path of the file to serve.</param>
    /// <param name="options"><see cref="StaticFileOptions"/> for the <see cref="StaticFileMiddleware"/>.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/></returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallbackToFile(IEndpointRouteBuilder, string, StaticFileOptions)"/> is intended to handle cases
    /// where URL path of the request does not contain a file name, and no other endpoint has matched. This is convenient
    /// for routing requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
    /// result in an HTTP 404.
    /// </para>
    /// <para>
    /// <see cref="MapFallbackToFile(IEndpointRouteBuilder, string, StaticFileOptions)"/> registers an endpoint using the pattern
    /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// </remarks>
    [UnconditionalSuppressMessage("Trimmer", "IL2026",
        Justification = "MapFallbackToFile RequireUnreferencedCode if the RequestDelegate has a Task<T> return type which is not the case here.")]
    public static IEndpointConventionBuilder MapFallbackToFile(
        this IEndpointRouteBuilder endpoints,
        string filePath,
        StaticFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(filePath);

        return endpoints
            .MapFallback(CreateRequestDelegate(endpoints, filePath, options))
            .WithMetadata(new HttpMethodMetadata(_supportedHttpMethods));
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// requests for non-filenames with the lowest possible priority. The request will be routed to a
    /// <see cref="StaticFileMiddleware"/> that attempts to serve the file specified by <paramref name="filePath"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="filePath">The file path of the file to serve.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/></returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallbackToFile(IEndpointRouteBuilder, string, string)"/> is intended to handle
    /// cases where URL path of the request does not contain a filename, and no other endpoint has matched. This is
    /// convenient for routing requests for dynamic content to a SPA framework, while also allowing requests for
    /// non-existent files to result in an HTTP 404.
    /// </para>
    /// <para>
    /// The default <see cref="StaticFileOptions"/> for the <see cref="StaticFileMiddleware"/> will be used.
    /// </para>
    /// <para>
    /// The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// <para>
    /// This overload will use the provided <paramref name="pattern"/> verbatim. Use the <c>:nonfile</c> route contraint
    /// to exclude requests for static files.
    /// </para>
    /// </remarks>
    [UnconditionalSuppressMessage("Trimmer", "IL2026",
        Justification = "MapFallbackToFile RequireUnreferencedCode if the RequestDelegate has a Task<T> return type which is not the case here.")]
    public static IEndpointConventionBuilder MapFallbackToFile(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(filePath);

        return endpoints
            .MapFallback(pattern, CreateRequestDelegate(endpoints, filePath))
            .WithMetadata(new HttpMethodMetadata(_supportedHttpMethods));
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that will match
    /// requests for non-filenames with the lowest possible priority. The request will be routed to a
    /// <see cref="StaticFileMiddleware"/> that attempts to serve the file specified by <paramref name="filePath"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>\
    /// <param name="pattern">The route pattern.</param>
    /// <param name="filePath">The file path of the file to serve.</param>
    /// <param name="options"><see cref="StaticFileOptions"/> for the <see cref="StaticFileMiddleware"/>.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/></returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallbackToFile(IEndpointRouteBuilder, string, string, StaticFileOptions)"/> is intended to handle
    /// cases where URL path of the request does not contain a filename, and no other endpoint has matched. This is
    /// convenient for routing requests for dynamic content to a SPA framework, while also allowing requests for
    /// non-existent files to result in an HTTP 404.
    /// </para>
    /// <para>
    /// The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// <para>
    /// This overload will use the provided <paramref name="pattern"/> verbatim. Use the <c>:nonfile</c> route contraint
    /// to exclude requests for static files.
    /// </para>
    /// </remarks>
    [UnconditionalSuppressMessage("Trimmer", "IL2026",
        Justification = "MapFallbackToFile RequireUnreferencedCode if the RequestDelegate has a Task<T> return type which is not the case.")]
    public static IEndpointConventionBuilder MapFallbackToFile(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        string filePath,
        StaticFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(filePath);

        return endpoints
            .MapFallback(pattern, CreateRequestDelegate(endpoints, filePath, options))
            .WithMetadata(new HttpMethodMetadata(_supportedHttpMethods));
    }

    private static RequestDelegate CreateRequestDelegate(
        IEndpointRouteBuilder endpoints,
        string filePath,
        StaticFileOptions? options = null)
    {
        var app = endpoints.CreateApplicationBuilder();
        app.Use(next => context =>
        {
            context.Request.Path = "/" + filePath;

            // Set endpoint to null so the static files middleware will handle the request.
            context.SetEndpoint(null);

            return next(context);
        });

        if (options == null)
        {
            app.UseStaticFiles();
        }
        else
        {
            app.UseStaticFiles(options);
        }

        return app.Build();
    }
}
