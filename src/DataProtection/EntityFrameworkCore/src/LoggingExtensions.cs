// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Reading data with key '{FriendlyName}', value '{Value}'.", EventName = "ReadKeyFromElement")]
    public static partial void ReadingXmlFromKey(this ILogger logger, string? friendlyName, string? value);

    [LoggerMessage(2, LogLevel.Debug, "Saving key '{FriendlyName}' to '{DbContext}'.", EventName = "SavingKeyToDbContext")]
    public static partial void LogSavingKeyToDbContext(this ILogger logger, string friendlyName, string dbContext);

    [LoggerMessage(3, LogLevel.Debug, "Deleting key '{FriendlyName}' from '{DbContext}'.", EventName = "DeletingKeyFromDbContext")]
    public static partial void DeletingKeyFromDbContext(this ILogger logger, string? friendlyName, string dbContext);

    [LoggerMessage(4, LogLevel.Error, "Failed to delete key '{FriendlyName}' from '{DbContext}'.", EventName = "FailedToDeleteKeyFromDbContext")]
    public static partial void FailedToDeleteKeyFromDbContext(this ILogger logger, string? friendlyName, string dbContext, Exception exception);

    [LoggerMessage(5, LogLevel.Error, "Failed to save key deletions to '{DbContext}'.", EventName = "FailedToSaveKeyDeletionsToDbContext")]
    public static partial void FailedToSaveKeyDeletionsToDbContext(this ILogger logger, string dbContext, Exception exception);
}
