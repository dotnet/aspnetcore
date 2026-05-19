// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace CorsMiddlewareWebSite;

public class EchoMiddleware
{
    /// <summary>
    /// Instantiates a new <see cref="EchoMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public EchoMiddleware(RequestDelegate next)
    {
    }

    /// <summary>
    /// Echo the request's path in the response. Does not invoke later middleware in the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> of the current request.</param>
    /// <returns>A <see cref="Task"/> that completes when writing to the response is done.</returns>
    public Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.ContentType = "text/plain; charset=utf-8";
        var path = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        return context.Response.WriteAsync(path, Encoding.UTF8);
    }
}
