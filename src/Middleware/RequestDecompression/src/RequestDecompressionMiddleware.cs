// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Enables HTTP request decompression.
/// </summary>
public class RequestDecompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestDecompressionProvider _provider;

    /// <summary>
    /// Initialize the request decompression middleware.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <param name="provider">The <see cref="IRequestDecompressionProvider"/>.</param>
    public RequestDecompressionMiddleware(
        RequestDelegate next,
        IRequestDecompressionProvider provider)
    {
        if (next == null)
        {
            throw new ArgumentNullException(nameof(next));
        }

        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

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
        var decompressionProvider = _provider.GetDecompressionProvider(context);
        if (decompressionProvider == null)
        {
            return _next(context);
        }

        return InvokeCore(context, decompressionProvider);
    }

    private async Task InvokeCore(HttpContext context, IDecompressionProvider decompressionProvider)
    {
        var originalBody = context.Request.Body;

        await using var decompressionStream = decompressionProvider.CreateStream(originalBody);

        context.Request.Body = decompressionStream;
        context.Request.Headers.Remove(HeaderNames.ContentEncoding);

        _provider.SetRequestSizeLimit(context);

        try
        {
            await _next(context);
        }
        finally
        {
            context.Request.Body = originalBody;
        }
    }
}
