// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{ContentResult}"/> that is responsible for <see cref="ContentResult"/>
/// </summary>
public partial class ContentResultExecutor : IActionResultExecutor<ContentResult>
{
    private const string DefaultContentType = "text/plain; charset=utf-8";
    private readonly ILogger<ContentResultExecutor> _logger;
    private readonly IHttpResponseStreamWriterFactory _httpResponseStreamWriterFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="ContentResultExecutor"/>.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="httpResponseStreamWriterFactory">The stream writer factory.</param>
    public ContentResultExecutor(ILogger<ContentResultExecutor> logger, IHttpResponseStreamWriterFactory httpResponseStreamWriterFactory)
    {
        _logger = logger;
        _httpResponseStreamWriterFactory = httpResponseStreamWriterFactory;
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAsync(ActionContext context, ContentResult result)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var response = context.HttpContext.Response;

        ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
            result.ContentType,
            response.ContentType,
            (DefaultContentType, Encoding.UTF8),
            MediaType.GetEncoding,
            out var resolvedContentType,
            out var resolvedContentTypeEncoding);

        response.ContentType = resolvedContentType;

        if (result.StatusCode != null)
        {
            response.StatusCode = result.StatusCode.Value;
        }

        Log.ContentResultExecuting(_logger, resolvedContentType);

        if (result.Content != null)
        {
            response.ContentLength = resolvedContentTypeEncoding.GetByteCount(result.Content);

            await using (var textWriter = _httpResponseStreamWriterFactory.CreateWriter(response.Body, resolvedContentTypeEncoding))
            {
                await textWriter.WriteAsync(result.Content);

                // Flushing the HttpResponseStreamWriter does not flush the underlying stream. This just flushes
                // the buffered text in the writer.
                // We do this rather than letting dispose handle it because dispose would call Write and we want
                // to call WriteAsync.
                await textWriter.FlushAsync();
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Executing ContentResult with HTTP Response ContentType of {ContentType}", EventName = "ContentResultExecuting")]
        public static partial void ContentResultExecuting(ILogger logger, string contentType);
    }
}
