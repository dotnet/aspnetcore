// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpLogging;

namespace HttpLogging.Sample;

internal class SampleHttpLoggingInterceptor : IHttpLoggingInterceptor
{
    public void OnRequest(HttpLoggingContext logContext)
    {
        // Compare to ExcludePathStartsWith
        if (!logContext.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            logContext.LoggingFields = HttpLoggingFields.None;
        }

        // Don't enrich if we're not going to log any part of the request
        if (!logContext.IsAnyEnabled(HttpLoggingFields.Request))
        {
            return;
        }

        if (logContext.TryOverride(HttpLoggingFields.RequestPath))
        {
            RedactPath(logContext);
        }

        if (logContext.TryOverride(HttpLoggingFields.RequestHeaders))
        {
            RedactRequestHeaders(logContext);
        }

        EnrichRequest(logContext);
    }

    private void RedactRequestHeaders(HttpLoggingContext logContext)
    {
        foreach (var header in logContext.HttpContext.Request.Headers)
        {
            logContext.Add(header.Key, "RedactedHeader"); // TODO: Redact header value
        }
    }

    private void RedactResponseHeaders(HttpLoggingContext logContext)
    {
        foreach (var header in logContext.HttpContext.Response.Headers)
        {
            logContext.Add(header.Key, "RedactedHeader"); // TODO: Redact header value
        }
    }

    public void OnResponse(HttpLoggingContext logContext)
    {
        // Don't enrich if we're not going to log any part of the response
        if (!logContext.IsAnyEnabled(HttpLoggingFields.Response))
        {
            return;
        }

        if (logContext.TryOverride(HttpLoggingFields.ResponseHeaders))
        {
            RedactResponseHeaders(logContext);
        }

        EnrichResponse(logContext);
    }

    private void EnrichResponse(HttpLoggingContext logContext)
    {
        logContext.Add("ResponseEnrichment", "Stuff");
    }

    private void EnrichRequest(HttpLoggingContext logContext)
    {
        logContext.Add("RequestEnrichment", "Stuff");
    }

    private void RedactPath(HttpLoggingContext logContext)
    {
        logContext.Add(nameof(logContext.HttpContext.Request.Path), "RedactedPath");
    }
}
