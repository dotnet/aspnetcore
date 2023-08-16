// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed partial class CertificatePathWatcher : IDisposable
{
    private readonly Func<string, IFileProviderWithLinkInfo?> _fileProviderFactory;
    private readonly string _contentRootDir;
    private readonly ILogger<CertificatePathWatcher> _logger;

    private readonly object _metadataLock = new();

    /// <remarks>Acquire <see cref="_metadataLock"/> before accessing.</remarks>
    private readonly Dictionary<string, DirectoryWatchMetadata> _metadataForDirectory = new();
    /// <remarks>Acquire <see cref="_metadataLock"/> before accessing.</remarks>
    private readonly Dictionary<string, FileWatchMetadata> _metadataForFile = new();

    private ConfigurationReloadToken _reloadToken = new();
    private bool _disposed;

    public CertificatePathWatcher(IHostEnvironment hostEnvironment, ILogger<CertificatePathWatcher> logger)
        : this(
              hostEnvironment.ContentRootPath,
              logger,
              dir => Directory.Exists(dir) ? new PhysicalFileProviderWithLinkInfo(dir, ExclusionFilters.None) : null)
    {
    }

    /// <remarks>
    /// For testing.
    /// </remarks>
    internal CertificatePathWatcher(string contentRootPath, ILogger<CertificatePathWatcher> logger, Func<string, IFileProviderWithLinkInfo?> fileProviderFactory)
    {
        _contentRootDir = contentRootPath;
        _logger = logger;
        _fileProviderFactory = fileProviderFactory;
    }

    /// <summary>
    /// Returns a token that will fire when any watched <see cref="CertificateConfig"/> is changed on disk.
    /// The affected <see cref="CertificateConfig"/> will have <see cref="CertificateConfig.FileHasChanged"/>
    /// set to <code>true</code>.
    /// </summary>
    public IChangeToken GetChangeToken()
    {
        return _reloadToken;
    }

    /// <summary>
    /// Update the set of <see cref="CertificateConfig"/>s being watched for file changes.
    /// If a given <see cref="CertificateConfig"/> appears in both lists, it is first removed and then added.
    /// </summary>
    /// <remarks>
    /// Does not consider targets when watching files that are symlinks.
    /// </remarks>
    public void UpdateWatches(List<CertificateConfig> certificateConfigsToRemove, List<CertificateConfig> certificateConfigsToAdd)
    {
        var addSet = new HashSet<CertificateConfig>(certificateConfigsToAdd, ReferenceEqualityComparer.Instance);
        var removeSet = new HashSet<CertificateConfig>(certificateConfigsToRemove, ReferenceEqualityComparer.Instance);

        // Don't remove anything we're going to re-add anyway.
        // Don't remove such items from addSet to guard against the (hypothetical) possibility
        // that a caller might remove a config that isn't already present.
        removeSet.ExceptWith(certificateConfigsToAdd);

        if (addSet.Count == 0 && removeSet.Count == 0)
        {
            return;
        }

        lock (_metadataLock)
        {
            foreach (var certificateConfig in removeSet)
            {
                RemoveWatchUnsynchronized(certificateConfig);
            }

            foreach (var certificateConfig in addSet)
            {
                AddWatchUnsynchronized(certificateConfig);
            }

            // We don't clean up unused watchers until the Adds have been processed to maximize reuse
            if (removeSet.Count > 0)
            {
                // We could conceivably save time by propagating a list of files affected out of RemoveWatchUnsynchronized

                List<string>? filePathsToRemove = null;
                List<string>? dirPathsToRemove = null;
                foreach (var (path, fileMetadata) in _metadataForFile)
                {
                    if (fileMetadata.Configs.Count == 0)
                    {
                        (filePathsToRemove ??= new()).Add(path);

                        var dir = Path.GetDirectoryName(path)!;

                        // If we found fileMetadata, there must be a containing/corresponding dirMetadata
                        var dirMetadata = _metadataForDirectory[dir];

                        dirMetadata.FileWatchCount--;
                        if (dirMetadata.FileWatchCount == 0)
                        {
                            (dirPathsToRemove ??= new()).Add(dir);
                        }
                    }
                }

                if (filePathsToRemove is not null)
                {
                    foreach (var path in filePathsToRemove)
                    {
                        _metadataForFile.Remove(path, out var fileMetadata);
                        fileMetadata!.Dispose();
                        _logger.RemovedFileWatcher(path);
                    }
                }

                if (dirPathsToRemove is not null)
                {
                    foreach (var dir in dirPathsToRemove)
                    {
                        _metadataForDirectory.Remove(dir, out var dirMetadata);
                        dirMetadata!.Dispose();
                        _logger.RemovedDirectoryWatcher(dir);
                    }
                }
            }

            LogStateUnsynchronized();
        }
    }

    private void LogStateUnsynchronized()
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        foreach (var (dir, dirMetadata) in _metadataForDirectory)
        {
            _logger.FileCount(dir, dirMetadata.FileWatchCount);
        }

        foreach (var (path, fileMetadata) in _metadataForFile)
        {
            _logger.ObserverCount(path, fileMetadata.Configs.Count);
        }
    }

    /// <summary>
    /// Start watching a certificate's file path for changes.
    /// <paramref name="certificateConfig"/> must have <see cref="CertificateConfig.IsFileCert"/> set to <code>true</code>.
    /// </summary>
    /// <remarks>
    /// Internal for testing.
    /// </remarks>
    internal void AddWatchUnsynchronized(CertificateConfig certificateConfig)
    {
        Debug.Assert(certificateConfig.IsFileCert, $"{nameof(AddWatchUnsynchronized)} called on non-file cert");

        var path = Path.Combine(_contentRootDir, certificateConfig.Path);

        AddWatchWorkerUnsynchronized(certificateConfig, path);
    }

    /// <remarks>
    /// The patch of <paramref name="certificateConfig"/> may not match <paramref name="path"/> (e.g. in the presence of symlinks).
    /// </remarks>
    private void AddWatchWorkerUnsynchronized(CertificateConfig certificateConfig, string path)
    {
        var dir = Path.GetDirectoryName(path)!;

        if (!_metadataForDirectory.TryGetValue(dir, out var dirMetadata))
        {
            // If we wanted to detected deletions of this whole directory (which we don't since we ignore deletions),
            // we'd probably need to watch the whole directory hierarchy

            var parentDir = Path.GetDirectoryName(dir);
            var directoryName = parentDir is null ? null : Path.GetFileName(dir);

            var fileProvider = _fileProviderFactory(parentDir ?? dir);
            if (fileProvider is null)
            {
                _logger.DirectoryDoesNotExist(dir, path);
                return;
            }

            dirMetadata = new DirectoryWatchMetadata(fileProvider, directoryName);
            _metadataForDirectory.Add(dir, dirMetadata);

            _logger.CreatedDirectoryWatcher(dir);
        }

        if (!_metadataForFile.TryGetValue(path, out var fileMetadata))
        {
            // PhysicalFileProvider appears to be able to tolerate non-existent files, as long as the directory exists

            var disposable = ChangeToken.OnChange(
                () =>
                {
                    var fileProvider = dirMetadata.FileProvider;

                    if (dirMetadata.DirectoryName is not string dirName)
                    {
                        return fileProvider.Watch(Path.GetFileName(path));
                    }

                    return new CompositeChangeToken(new[]
                    {
                        fileProvider.Watch(dirName),
                        fileProvider.Watch(Path.Combine(dirName, Path.GetFileName(path))),
                    });
                },
                static tuple => tuple.Item1.OnChange(tuple.Item2, tuple.Item3),
                ValueTuple.Create(this, path, Path.Combine(_contentRootDir, certificateConfig.Path!)));

            fileMetadata = new FileWatchMetadata(disposable);
            _metadataForFile.Add(path, fileMetadata);
            dirMetadata.FileWatchCount++;

            // We actually don't care if the file doesn't exist - we'll watch in case it is created
            fileMetadata.LastModifiedTime = GetLastModifiedTimeAndLinkTargetPath(path, dirMetadata, out fileMetadata.LinkTargetPath);

            _logger.CreatedFileWatcher(path);
        }

        if (!fileMetadata.Configs.Add(certificateConfig))
        {
            // Note: this also prevents us from getting stuck in symlink cycles
            _logger.ReusedObserver(path);
            return;
        }

        _logger.AddedObserver(path);

        if (fileMetadata.LinkTargetPath is string linkTargetPath)
        {
            // The link target may not exist, but we'll let AddWatchWorkerUnsynchronized deal with that and log something sensible
            AddWatchWorkerUnsynchronized(certificateConfig, linkTargetPath);
        }
    }

    /// <remarks>
    /// Returns <see cref="DateTimeOffset.MinValue"/> if no last modified time is available (e.g. because the file doesn't exist).
    /// </remarks>
    private static DateTimeOffset GetLastModifiedTimeAndLinkTargetPath(string path, DirectoryWatchMetadata dirMetadata, out string? linkTargetPath)
    {
        var fileProvider = dirMetadata.FileProvider;
        var subpath = Path.GetFileName(path)!;
        var dirName = dirMetadata.DirectoryName;
        if (dirName is not null)
        {
            subpath = Path.Combine(dirName, subpath);
        }

        var fileInfo = fileProvider.GetFileInfo(subpath);
        if (!fileInfo.Exists)
        {
            linkTargetPath = null;
            return DateTimeOffset.MinValue;
        }

        // If the directory itself is a link, resolve that link.  We prefer to resolve the
        // directory link before the file link (if any) because the file will still be a link
        // after resolving the directory link but the reverse isn't true.
        var dirLinkTargetPath = dirName is not null
            ? dirMetadata.FileProvider.GetFileInfo(dirName).ResolveLinkTarget(returnFinalTarget: false)?.PhysicalPath
            : null;
        linkTargetPath = dirLinkTargetPath is not null
            ? Path.Combine(dirLinkTargetPath, Path.GetFileName(path))
            : fileInfo.ResolveLinkTarget(returnFinalTarget: false)?.PhysicalPath;

        return fileInfo.LastModified;
    }

    private void OnChange(string path, string originalPath)
    {
        _logger.FileEventReceived(path, originalPath);

        // Block until any in-progress updates are complete
        lock (_metadataLock)
        {
            if (!ShouldFireChangeUnsynchronized(path))
            {
                return;
            }
        }

        // AddWatch and RemoveWatch don't affect the token, so this doesn't need to be under the semaphore.
        // It does however need to be synchronized, since there could be multiple concurrent events.
        var previousToken = Interlocked.Exchange(ref _reloadToken, new());
        previousToken.OnReload();
    }

    private bool ShouldFireChangeUnsynchronized(string path)
    {
        if (!_metadataForFile.TryGetValue(path, out var fileMetadata))
        {
            _logger.UntrackedFileEvent(path);
            return false;
        }

        // If we found fileMetadata, there must be a containing/corresponding dirMetadata
        var dirMetadata = _metadataForDirectory[Path.GetDirectoryName(path)!];

        var lastModifiedTime = GetLastModifiedTimeAndLinkTargetPath(path, dirMetadata, out var linkTargetPath);

        // This usually indicate that we can't find the file, so we'll keep using the in-memory certificate
        // rather than trying to reload and probably crashing
        if (lastModifiedTime == DateTimeOffset.MinValue)
        {
            _logger.EventWithoutLastModifiedTime(path);
            return false;
        }

        var linkTargetPathChanged = linkTargetPath != fileMetadata.LinkTargetPath;

        // We ignore file changes that don't advance the last modified time.
        // For example, if we lose access to the network share the file is
        // stored on, we don't notify our listeners because no one wants
        // their endpoint/server to shutdown when that happens.
        // We also anticipate that a cert file might be renamed to cert.bak
        // before a new cert is introduced with the old name.
        // This also helps us in scenarios where the underlying file system
        // reports more than one change for a single logical operation.
        // We make an exception if the link target has changed.
        if (lastModifiedTime > fileMetadata.LastModifiedTime || linkTargetPathChanged)
        {
            // Note that this might move backwards if the link target changed
            fileMetadata.LastModifiedTime = lastModifiedTime;
            if (linkTargetPathChanged)
            {
                if (fileMetadata.LinkTargetPath is string oldLinkTargetPath)
                {
                    fileMetadata.OldLinkTargetPaths.Add(oldLinkTargetPath);
                }
                fileMetadata.LinkTargetPath = linkTargetPath;
            }
        }
        else
        {
            if (lastModifiedTime == fileMetadata.LastModifiedTime)
            {
                _logger.RedundantEvent(path);
            }
            else
            {
                _logger.OutOfOrderEvent(path);
            }

            return false;
        }

        foreach (var config in fileMetadata.Configs)
        {
            config.FileHasChanged = true;
        }

        return true;
    }

    /// <summary>
    /// Stop watching a certificate's file path for changes (previously started by <see cref="AddWatchUnsynchronized"/>.
    /// <paramref name="certificateConfig"/> must have <see cref="CertificateConfig.IsFileCert"/> set to <code>true</code>.
    /// </summary>
    /// <remarks>
    /// Internal for testing.
    /// </remarks>
    internal void RemoveWatchUnsynchronized(CertificateConfig certificateConfig)
    {
        Debug.Assert(certificateConfig.IsFileCert, $"{nameof(RemoveWatchUnsynchronized)} called on non-file cert");
        var path = Path.Combine(_contentRootDir, certificateConfig.Path);

        RemoveWatchWorkerUnsynchronized(certificateConfig, path);
    }

    /// <remarks>
    /// The patch of <paramref name="certificateConfig"/> may not match <paramref name="path"/> (e.g. in the presence of symlinks).
    /// </remarks>
    private void RemoveWatchWorkerUnsynchronized(CertificateConfig certificateConfig, string path)
    {
        if (!_metadataForFile.TryGetValue(path, out var fileMetadata))
        {
            _logger.UnknownFile(path);
            return;
        }

        if (!fileMetadata.Configs.Remove(certificateConfig))
        {
            _logger.UnknownObserver(path);
            return;
        }

        _logger.RemovedObserver(path);

        var oldLinkTargetPaths = fileMetadata.OldLinkTargetPaths;
        if (oldLinkTargetPaths.Count > 0)
        {
            foreach (var linkTargetPath in oldLinkTargetPaths)
            {
                RemoveWatchWorkerUnsynchronized(certificateConfig, linkTargetPath);
            }

            // If there are OldLinkTargetpaths, we don't have to clean up LinkTargetPath because it hasn't been initialized
        }
        else if (fileMetadata.LinkTargetPath is string linkTargetPath)
        {
            RemoveWatchWorkerUnsynchronized(certificateConfig, linkTargetPath);
        }

        fileMetadata.OldLinkTargetPaths.Clear();
    }

    /// <remarks>Test hook</remarks>
    internal int TestGetDirectoryWatchCountUnsynchronized() => _metadataForDirectory.Count;

    /// <remarks>Test hook</remarks>
    internal int TestGetFileWatchCountUnsynchronized(string dir) => _metadataForDirectory.TryGetValue(dir, out var metadata) ? metadata.FileWatchCount : 0;

    /// <remarks>Test hook</remarks>
    internal int TestGetObserverCountUnsynchronized(string path) => _metadataForFile.TryGetValue(path, out var metadata) ? metadata.Configs.Count : 0;

    void IDisposable.Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        foreach (var dirMetadata in _metadataForDirectory.Values)
        {
            dirMetadata.Dispose();
        }

        foreach (var fileMetadata in _metadataForFile.Values)
        {
            fileMetadata.Dispose();
        }
    }

    private sealed class DirectoryWatchMetadata(IFileProviderWithLinkInfo fileProvider, string? directoryName) : IDisposable
    {
        public readonly IFileProviderWithLinkInfo FileProvider = fileProvider;
        public readonly string? DirectoryName = directoryName; // TODO (acasey): document
        public int FileWatchCount;

        public void Dispose() => (FileProvider as IDisposable)?.Dispose();
    }

    private sealed class FileWatchMetadata(IDisposable disposable) : IDisposable
    {
        public readonly IDisposable Disposable = disposable;
        public readonly HashSet<CertificateConfig> Configs = new(ReferenceEqualityComparer.Instance);
        public readonly HashSet<string> OldLinkTargetPaths = new();
        public string? LinkTargetPath;
        public DateTimeOffset LastModifiedTime = DateTimeOffset.MinValue;

        public void Dispose() => Disposable.Dispose();
    }
}
