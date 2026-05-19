// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal static partial class CertificatePathWatcherLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Directory '{Directory}' does not exist so changes to the certificate '{Path}' will not be tracked.", EventName = "DirectoryDoesNotExist")]
    public static partial void DirectoryDoesNotExist(this ILogger<CertificatePathWatcher> logger, string directory, string path);

    [LoggerMessage(2, LogLevel.Warning, "Attempted to remove watch from unwatched path '{Path}'.", EventName = "UnknownFile")]
    public static partial void UnknownFile(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(3, LogLevel.Warning, "Attempted to remove unknown observer from path '{Path}'.", EventName = "UnknownObserver")]
    public static partial void UnknownObserver(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(4, LogLevel.Debug, "Created directory watcher for '{Directory}'.", EventName = "CreatedDirectoryWatcher")]
    public static partial void CreatedDirectoryWatcher(this ILogger<CertificatePathWatcher> logger, string directory);

    [LoggerMessage(5, LogLevel.Debug, "Created file watcher for '{Path}'.", EventName = "CreatedFileWatcher")]
    public static partial void CreatedFileWatcher(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(6, LogLevel.Debug, "Removed directory watcher for '{Directory}'.", EventName = "RemovedDirectoryWatcher")]
    public static partial void RemovedDirectoryWatcher(this ILogger<CertificatePathWatcher> logger, string directory);

    [LoggerMessage(7, LogLevel.Debug, "Removed file watcher for '{Path}'.", EventName = "RemovedFileWatcher")]
    public static partial void RemovedFileWatcher(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(8, LogLevel.Debug, "Error retrieving last modified time for '{Path}'.", EventName = "LastModifiedTimeError")]
    public static partial void LastModifiedTimeError(this ILogger<CertificatePathWatcher> logger, string path, Exception e);

    [LoggerMessage(9, LogLevel.Debug, "Ignored event for presently untracked file '{Path}'.", EventName = "UntrackedFileEvent")]
    public static partial void UntrackedFileEvent(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(10, LogLevel.Trace, "Reused existing observer on file watcher for '{Path}'.", EventName = "ReusedObserver")]
    public static partial void ReusedObserver(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(11, LogLevel.Trace, "Added observer to file watcher for '{Path}'.", EventName = "AddedObserver")]
    public static partial void AddedObserver(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(12, LogLevel.Trace, "Removed observer from file watcher for '{Path}'.", EventName = "RemovedObserver")]
    public static partial void RemovedObserver(this ILogger<CertificatePathWatcher> logger, string path);

    [LoggerMessage(13, LogLevel.Trace, "File '{Path}' now has {Count} observers.", EventName = "ObserverCount")]
    public static partial void ObserverCount(this ILogger<CertificatePathWatcher> logger, string path, int count);

    [LoggerMessage(14, LogLevel.Trace, "Directory '{Directory}' now has watchers on {Count} files.", EventName = "FileCount")]
    public static partial void FileCount(this ILogger<CertificatePathWatcher> logger, string directory, int count);

    [LoggerMessage(15, LogLevel.Trace, "Flagged {Count} observers of '{Path}' as changed.", EventName = "FlaggedObservers")]
    public static partial void FlaggedObservers(this ILogger<CertificatePathWatcher> logger, string path, int count);

    [LoggerMessage(16, LogLevel.Trace, "Ignored event since '{Path}' was unavailable.", EventName = "EventWithoutFile")]
    public static partial void EventWithoutFile(this ILogger<CertificatePathWatcher> logger, string path);
}
