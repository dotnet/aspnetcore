// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Reading data with key '{FriendlyName}', value '{Value}'.", EventName = "ReadKeyFromElement")]
    public static partial void ReadingXmlFromKey(this ILogger logger, string? friendlyName, string? value);

    [LoggerMessage(2, LogLevel.Debug, "Saving key '{FriendlyName}' to '{DbContext}'.", EventName = "SavingKeyToDbContext")]
    public static partial void LogSavingKeyToDbContext(this ILogger logger, string friendlyName, string dbContext);
}
