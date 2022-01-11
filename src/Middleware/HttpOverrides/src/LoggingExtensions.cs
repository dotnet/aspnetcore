// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(0, LogLevel.Warning, "Could not read certificate from header.", EventName = "NoCertificate")]
    public static partial void NoCertificate(this ILogger logger, Exception exception);
}
