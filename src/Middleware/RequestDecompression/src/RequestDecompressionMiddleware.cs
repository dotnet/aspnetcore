// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Enables HTTP request decompression.
/// </summary>
internal sealed partial class RequestDecompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDecompressionMiddleware> _logger;
    private readonly IRequestDecompressionProvider _provider;

    /// <summary>
    /// Initialize the request decompression middleware.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The <see cref="IRequestDecompressionProvider"/>.</param>
    public RequestDecompressionMiddleware(
        RequestDelegate next,
        ILogger<RequestDecompressionMiddleware> logger,
        IRequestDecompressionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(provider);

        _next = next;
        _logger = logger;
        _provider = provider;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(HttpContext context)
    {
        var decompressionStream = _provider.GetDecompressionStream(context);
        if (decompressionStream is null)
        {
            return _next(context);
        }

        return InvokeCore(context, decompressionStream);
    }

    private async Task InvokeCore(HttpContext context, Stream decompressionStream)
    {
        var request = context.Request.Body;
        try
        {
            var sizeLimit =
                context.GetEndpoint()?.Metadata?.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize
                    ?? context.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize;

            context.Request.Body = new SizeLimitedStream(decompressionStream, sizeLimit);
            await _next(context);
        }
        finally
        {
            context.Request.Body = request;
            await decompressionStream.DisposeAsync();
        }
    }
}
