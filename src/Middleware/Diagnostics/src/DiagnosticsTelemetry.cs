// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics;

internal static class DiagnosticsTelemetry
{
    public static void ReportUnhandledException(ILogger logger, HttpContext context, Exception ex)
    {
        logger.UnhandledException(ex);

        if (context.Features.Get<IHttpMetricsTagsFeature>() is { } tagsFeature)
        {
            // Multiple exception middleware could be registered that have already added the tag.
            // We don't want to add a duplicate tag here because that breaks some metrics systems.
            tagsFeature.TryAddTag("error.type", ex.GetType().FullName);
        }
    }
}
