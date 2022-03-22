// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="ContentHttpResult"/> that when executed
/// will produce a response with content.
/// </summary>
public sealed partial class ContentHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentHttpResult"/> class with the values.
    /// </summary>
    /// <param name="content">The value to format in the entity body.</param>
    /// <param name="contentType">The Content-Type header for the response</param>
    internal ContentHttpResult(string? content, string? contentType)
        : this(content, contentType, statusCode: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentHttpResult"/> class with the values
    /// </summary>
    /// <param name="content">The value to format in the entity body.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="contentType">The Content-Type header for the response</param>
    internal ContentHttpResult(string? content, string? contentType, int? statusCode)
    {
        Content = content;
        StatusCode = statusCode;
        ContentType = contentType;
    }

    /// <summary>
    /// Gets or set the content representing the body of the response.
    /// </summary>
    public string? Content { get; internal init; }

    /// <summary>
    /// Gets or sets the Content-Type header for the response.
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
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.ContentResult");

        return HttpResultsHelper.WriteResultAsContentAsync(httpContext, logger, Content, StatusCode, ContentType);
    }
}
