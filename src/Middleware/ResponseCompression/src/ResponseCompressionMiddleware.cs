// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Enable HTTP response compression.
/// </summary>
public class ResponseCompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IResponseCompressionProvider _provider;

    /// <summary>
    /// Initialize the Response Compression middleware.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <param name="provider">The <see cref="IResponseCompressionProvider"/>.</param>
    public ResponseCompressionMiddleware(RequestDelegate next, IResponseCompressionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(provider);

        _next = next;
        _provider = provider;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(HttpContext context)
    {
        if (!_provider.CheckRequestAcceptsCompression(context))
        {
            return _next(context);
        }
        return InvokeCore(context);
    }

    private async Task InvokeCore(HttpContext context)
    {
        var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>();
        var originalCompressionFeature = context.Features.Get<IHttpsCompressionFeature>();

        Debug.Assert(originalBodyFeature != null);

        var compressionBody = new ResponseCompressionBody(context, _provider, originalBodyFeature);
        context.Features.Set<IHttpResponseBodyFeature>(compressionBody);
        context.Features.Set<IHttpsCompressionFeature>(compressionBody);

        try
        {
            await _next(context);
            await compressionBody.FinishCompressionAsync();
        }
        finally
        {
            context.Features.Set(originalBodyFeature);
            context.Features.Set(originalCompressionFeature);
        }
    }
}
