// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Cors;

internal static partial class CorsLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Skipping the execution of current filter as its not the most effective filter implementing the policy {FilterPolicy}.", EventName = "NotMostEffectiveFilter")]
    public static partial void NotMostEffectiveFilter(this ILogger logger, Type filterPolicy);
}
