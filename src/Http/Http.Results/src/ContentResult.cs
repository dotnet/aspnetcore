// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result;

internal sealed partial class ContentResult : IResult
{
    private const string DefaultContentType = "text/plain; charset=utf-8";
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    /// <summary>
    /// Gets or set the content representing the body of the response.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Gets or sets the Content-Type header for the response.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Writes the content to the HTTP response.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;

        ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
            ContentType,
            response.ContentType,
            (DefaultContentType, DefaultEncoding),
            ResponseContentTypeHelper.GetEncoding,
            out var resolvedContentType,
            out var resolvedContentTypeEncoding);

        response.ContentType = resolvedContentType;

        if (StatusCode != null)
        {
            response.StatusCode = StatusCode.Value;
        }

        var logger = httpContext.RequestServices.GetRequiredService<ILogger<ContentResult>>();

        Log.ContentResultExecuting(logger, resolvedContentType);

        if (Content != null)
        {
            response.ContentLength = resolvedContentTypeEncoding.GetByteCount(Content);
            await response.WriteAsync(Content, resolvedContentTypeEncoding);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information,
            "Executing ContentResult with HTTP Response ContentType of {ContentType}",
            EventName = "ContentResultExecuting")]
        internal static partial void ContentResultExecuting(ILogger logger, string contentType);
    }
}
