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
        if ((HttpLoggingFields.Request & logContext.LoggingFields) == HttpLoggingFields.None)
        {
            return;
        }
        if (logContext.LoggingFields.HasFlag(HttpLoggingFields.RequestPath))
        {
            // We've handled the path, turn off the default logging
            RedactPath(logContext);
            logContext.LoggingFields &= ~HttpLoggingFields.RequestPath;
        }
        if (logContext.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
        {
            RedactResponseHeaders(logContext);
            // We've handled the request headers, turn off the default logging
            logContext.LoggingFields &= ~HttpLoggingFields.RequestHeaders;
        }
        EnrichRequest(logContext);
    }

    public void OnResponse(HttpLoggingContext logContext)
    {
        // Don't enrich if we're not going to log any part of the response
        if ((HttpLoggingFields.Response & logContext.LoggingFields) == HttpLoggingFields.None)
        {
            return;
        }
        if (logContext.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
        {
            RedactResponseHeaders(logContext);
            // We've handled the response headers, turn off the default logging
            logContext.LoggingFields &= ~HttpLoggingFields.ResponseHeaders;
        }
        EnrichResponse(logContext);
    }

    private void EnrichResponse(HttpLoggingContext logContext)
    {
        logContext.Add("Response", "Enriched");
    }

    private void EnrichRequest(HttpLoggingContext logContext)
    {
        logContext.Add("RequestEnrichment", "Enriched");
    }

    private void RedactPath(HttpLoggingContext logContext)
    {
        logContext.Add("Path", "RedactedPath");
    }

    private void RedactResponseHeaders(HttpLoggingContext logContext)
    {
        logContext.Add("ResponseHeaders", "RedactedResponseHeaders");
    }
}
