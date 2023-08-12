// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed partial class CertificatePathWatcher : IDisposable
{
    private readonly Func<string, IFileProvider?> _fileProviderFactory;
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
              dir => Directory.Exists(dir) ? new PhysicalFileProvider(dir, ExclusionFilters.None) : null)
    {
    }

    /// <remarks>
    /// For testing.
    /// </remarks>
    internal CertificatePathWatcher(string contentRootPath, ILogger<CertificatePathWatcher> logger, Func<string, IFileProvider?> fileProviderFactory)
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
            // Adds before removes to increase the chances of watcher reuse.
            // Since removeSet doesn't contain any of these configs, this won't change the semantics.
            foreach (var certificateConfig in addSet)
            {
                AddWatchUnsynchronized(certificateConfig);
            }

            foreach (var certificateConfig in removeSet)
            {
                RemoveWatchUnsynchronized(certificateConfig);
            }
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
        Debug.Assert(certificateConfig.IsFileCert, "AddWatch called on non-file cert");

        var path = Path.Combine(_contentRootDir, certificateConfig.Path);
        var dir = Path.GetDirectoryName(path)!;

        if (!_metadataForDirectory.TryGetValue(dir, out var dirMetadata))
        {
            // If we wanted to detected deletions of this whole directory (which we don't since we ignore deletions),
            // we'd probably need to watch the whole directory hierarchy

            var fileProvider = _fileProviderFactory(dir);
            if (fileProvider is null)
            {
                _logger.DirectoryDoesNotExist(dir, path);
                return;
            }

            dirMetadata = new DirectoryWatchMetadata(fileProvider);
            _metadataForDirectory.Add(dir, dirMetadata);

            _logger.CreatedDirectoryWatcher(dir);
        }

        if (!_metadataForFile.TryGetValue(path, out var fileMetadata))
        {
            // PhysicalFileProvider appears to be able to tolerate non-existent files, as long as the directory exists

            var disposable = ChangeToken.OnChange(
                () => dirMetadata.FileProvider.Watch(Path.GetFileName(path)),
                static tuple => tuple.Item1.OnChange(tuple.Item2),
                ValueTuple.Create(this, path));

            fileMetadata = new FileWatchMetadata(disposable);
            _metadataForFile.Add(path, fileMetadata);
            dirMetadata.FileWatchCount++;

            // We actually don't care if the file doesn't exist - we'll watch in case it is created
            fileMetadata.LastModifiedTime = GetLastModifiedTimeOrMinimum(path, dirMetadata.FileProvider);

            _logger.CreatedFileWatcher(path);
        }

        if (!fileMetadata.Configs.Add(certificateConfig))
        {
            _logger.ReusedObserver(path);
            return;
        }

        _logger.AddedObserver(path);

        _logger.ObserverCount(path, fileMetadata.Configs.Count);
        _logger.FileCount(dir, dirMetadata.FileWatchCount);
    }

    private DateTimeOffset GetLastModifiedTimeOrMinimum(string path, IFileProvider fileProvider)
    {
        try
        {
            return fileProvider.GetFileInfo(Path.GetFileName(path)).LastModified;
        }
        catch (Exception e)
        {
            _logger.LastModifiedTimeError(path, e);
        }

        return DateTimeOffset.MinValue;
    }

    private void OnChange(string path)
    {
        // Block until any in-progress updates are complete
        lock (_metadataLock)
        {
            if (!_metadataForFile.TryGetValue(path, out var fileMetadata))
            {
                _logger.UntrackedFileEvent(path);
                return;
            }

            // Existence implied by the fact that we're tracking the file
            var dirMetadata = _metadataForDirectory[Path.GetDirectoryName(path)!];

            // We ignore file changes that don't advance the last modified time.
            // For example, if we lose access to the network share the file is
            // stored on, we don't notify our listeners because no one wants
            // their endpoint/server to shutdown when that happens.
            // We also anticipate that a cert file might be renamed to cert.bak
            // before a new cert is introduced with the old name.
            // This also helps us in scenarios where the underlying file system
            // reports more than one change for a single logical operation.
            var lastModifiedTime = GetLastModifiedTimeOrMinimum(path, dirMetadata.FileProvider);
            if (lastModifiedTime > fileMetadata.LastModifiedTime)
            {
                fileMetadata.LastModifiedTime = lastModifiedTime;
            }
            else
            {
                if (lastModifiedTime == DateTimeOffset.MinValue)
                {
                    _logger.EventWithoutLastModifiedTime(path);
                }
                else if (lastModifiedTime == fileMetadata.LastModifiedTime)
                {
                    _logger.RedundantEvent(path);
                }
                else
                {
                    _logger.OutOfOrderEvent(path);
                }
                return;
            }

            var configs = fileMetadata.Configs;
            foreach (var config in configs)
            {
                config.FileHasChanged = true;
            }
        }

        // AddWatch and RemoveWatch don't affect the token, so this doesn't need to be under the semaphore.
        // It does however need to be synchronized, since there could be multiple concurrent events.
        var previousToken = Interlocked.Exchange(ref _reloadToken, new());
        previousToken.OnReload();
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
        Debug.Assert(certificateConfig.IsFileCert, "RemoveWatch called on non-file cert");

        var path = Path.Combine(_contentRootDir, certificateConfig.Path);
        var dir = Path.GetDirectoryName(path)!;

        if (!_metadataForFile.TryGetValue(path, out var fileMetadata))
        {
            _logger.UnknownFile(path);
            return;
        }

        var configs = fileMetadata.Configs;

        if (!configs.Remove(certificateConfig))
        {
            _logger.UnknownObserver(path);
            return;
        }

        _logger.RemovedObserver(path);

        // If we found fileMetadata, there must be a containing/corresponding dirMetadata
        var dirMetadata = _metadataForDirectory[dir];

        if (configs.Count == 0)
        {
            fileMetadata.Dispose();
            _metadataForFile.Remove(path);
            dirMetadata.FileWatchCount--;

            _logger.RemovedFileWatcher(path);

            if (dirMetadata.FileWatchCount == 0)
            {
                dirMetadata.Dispose();
                _metadataForDirectory.Remove(dir);

                _logger.RemovedDirectoryWatcher(dir);
            }
        }

        _logger.ObserverCount(path, configs.Count);
        _logger.FileCount(dir, dirMetadata.FileWatchCount);
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

    private sealed class DirectoryWatchMetadata(IFileProvider fileProvider) : IDisposable
    {
        public readonly IFileProvider FileProvider = fileProvider;
        public int FileWatchCount;

        public void Dispose() => (FileProvider as IDisposable)?.Dispose();
    }

    private sealed class FileWatchMetadata(IDisposable disposable) : IDisposable
    {
        public readonly IDisposable Disposable = disposable;
        public readonly HashSet<CertificateConfig> Configs = new(ReferenceEqualityComparer.Instance);
        public DateTimeOffset LastModifiedTime = DateTimeOffset.MinValue;

        public void Dispose() => Disposable.Dispose();
    }
}
