// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

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
        if (!_provider.ShouldDecompressRequest(context))
        {
            return _next(context);
        }

        if (!_provider.IsContentEncodingSupported(context))
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;

            return Task.CompletedTask;
        }

        return InvokeCore(context);
    }

    private async Task InvokeCore(HttpContext context)
    {
        var decompressionBody = new RequestDecompressionBody(context, _provider);
        context.Request.Body = decompressionBody;

        await _next(context);
        await decompressionBody.FinishDecompressionAsync();
    }
}
