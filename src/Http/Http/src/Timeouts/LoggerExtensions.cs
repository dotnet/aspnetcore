// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Timeouts;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Timeout exception handled.")]
    public static partial void TimeoutExceptionHandled(this ILogger logger, Exception exception);
}
