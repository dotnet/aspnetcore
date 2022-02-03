// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpsPolicy;

internal static partial class HstsLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "The request is insecure. Skipping HSTS header.", EventName = "NotSecure")]
    public static partial void SkippingInsecure(this ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "The host '{host}' is excluded. Skipping HSTS header.", EventName = "ExcludedHost")]
    public static partial void SkippingExcludedHost(this ILogger logger, string host);

    [LoggerMessage(3, LogLevel.Trace, "Adding HSTS header to response.", EventName = "AddingHstsHeader")]
    public static partial void AddingHstsHeader(this ILogger logger);
}
