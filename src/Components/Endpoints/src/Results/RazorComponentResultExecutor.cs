// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Executes a <see cref="RazorComponentResult"/>.
/// </summary>
public class RazorComponentResultExecutor
{
    /// <summary>
    /// The default content-type header value for Razor Components, <c>text/html; charset=utf-8</c>.
    /// </summary>
    public static readonly string DefaultContentType = "text/html; charset=utf-8";

    /// <summary>
    /// Executes a <see cref="RazorComponentResult"/> asynchronously.
    /// </summary>
    public virtual Task ExecuteAsync(HttpContext httpContext, RazorComponentResult result)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var response = httpContext.Response;
        response.ContentType = result.ContentType ?? DefaultContentType;

        if (result.StatusCode != null)
        {
            response.StatusCode = result.StatusCode.Value;
        }
        
        return RazorComponentEndpoint.RenderComponentToResponse(
            httpContext,
            result.ComponentType,
            result.Parameters);
    }
}
