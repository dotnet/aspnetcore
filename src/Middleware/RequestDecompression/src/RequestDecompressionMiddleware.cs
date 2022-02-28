// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;

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
        if (next is null)
        {
            throw new ArgumentNullException(nameof(next));
        }

        if (provider is null)
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
        var provider = _provider.GetDecompressionProvider(context);
        if (provider is null)
        {
            return _next(context);
        }

        return InvokeCore(context, provider);
    }

    private async Task InvokeCore(HttpContext context, IDecompressionProvider provider)
    {
        var request = context.Request.Body;
        try
        {
            await using var stream = provider.GetDecompressionStream(request);

            var sizeLimit =
                context.GetEndpoint()?.Metadata?.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize
                    ?? context.Features.GetRequiredFeature<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize;

            context.Request.Body = new SizeLimitedStream(stream, sizeLimit);
            await _next(context);
        }
        finally
        {
            context.Request.Body = request;
        }
    }
}
