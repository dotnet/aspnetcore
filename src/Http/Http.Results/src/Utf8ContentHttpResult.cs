// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="Utf8ContentHttpResult"/> that when executed
/// will produce a response with content.
/// </summary>
public sealed partial class Utf8ContentHttpResult : IResult, IStatusCodeHttpResult, IContentTypeHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8ContentHttpResult"/> class with the values
    /// </summary>
    /// <param name="utf8Content">The value to format in the entity body.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="contentType">The Content-Type header for the response.</param>
    internal Utf8ContentHttpResult(ReadOnlySpan<byte> utf8Content, string? contentType, int? statusCode)
    {
        // We need to make a copy here since we have to stash it on the heap
        ResponseContent = utf8Content.ToArray();
        StatusCode = statusCode;
        ContentType = contentType;
    }

    /// <summary>
    /// Gets the content representing the body of the response.
    /// </summary>
    public ReadOnlyMemory<byte> ResponseContent { get; internal init; }

    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    public string? ContentType { get; internal init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; internal init; }

    /// <summary>
    /// Writes the content to the HTTP response.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (StatusCode is { } statusCode)
        {
            // Creating the logger with a string to preserve the category after the refactoring.
            // It's important to only access RequestServices & create the logger if we're actually going to use it
            // to avoid the costs when they're not necessary.
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.Utf8ContentHttpResult");

            HttpResultsHelper.Log.WritingResultAsStatusCode(logger, statusCode);
            httpContext.Response.StatusCode = statusCode;
        }

        httpContext.Response.ContentType = ContentType ?? ContentTypeConstants.DefaultContentType;

        httpContext.Response.ContentLength = ResponseContent.Length;
        return httpContext.Response.Body.WriteAsync(ResponseContent).AsTask();
    }
}
