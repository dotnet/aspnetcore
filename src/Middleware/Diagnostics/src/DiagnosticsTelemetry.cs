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
            tagsFeature.Tags.Add(new KeyValuePair<string, object?>("error.type", ex.GetType().FullName));
        }
    }
}
