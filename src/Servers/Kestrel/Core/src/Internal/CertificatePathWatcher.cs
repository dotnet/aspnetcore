// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed partial class CertificatePathWatcher : IDisposable
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<CertificatePathWatcher> _logger;

    private readonly object _metadataLock = new();

    /// <remarks>Acquire <see cref="_metadataLock"/> before accessing.</remarks>
    private readonly Dictionary<string, FileWatchMetadata> _metadataForFile = new();

    private ConfigurationReloadToken _reloadToken = new();
    private bool _disposed;

    public CertificatePathWatcher(IHostEnvironment hostEnvironment, ILogger<CertificatePathWatcher> logger)
    {
        _hostEnvironment = hostEnvironment;
        _logger = logger;
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

        var contentRootPath = _hostEnvironment.ContentRootPath;
        var path = Path.Combine(contentRootPath, certificateConfig.Path);
        var relativePath = Path.GetRelativePath(contentRootPath, path);

        if (!_metadataForFile.TryGetValue(path, out var fileMetadata))
        {
            // PhysicalFileProvider appears to be able to tolerate non-existent files, as long as the directory exists

            var disposable = ChangeToken.OnChange(
                () =>
                {
                    var changeToken = _hostEnvironment.ContentRootFileProvider.Watch(relativePath);
                    if (ReferenceEquals(changeToken, NullChangeToken.Singleton))
                    {
                        _logger.NullChangeToken(path);
                    }
                    return changeToken;
                },
                static tuple => tuple.Item1.OnChange(tuple.Item2),
                ValueTuple.Create(this, path));

            fileMetadata = new FileWatchMetadata(disposable);
            _metadataForFile.Add(path, fileMetadata);

            // We actually don't care if the file doesn't exist - we'll watch in case it is created
            fileMetadata.LastModifiedTime = GetLastModifiedTimeOrMinimum(path, _hostEnvironment.ContentRootFileProvider);

            _logger.CreatedFileWatcher(path);
        }

        if (!fileMetadata.Configs.Add(certificateConfig))
        {
            _logger.ReusedObserver(path);
            return;
        }

        _logger.AddedObserver(path);

        _logger.ObserverCount(path, fileMetadata.Configs.Count);
        _logger.FileCount(contentRootPath, _metadataForFile.Count);
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

            // We ignore file changes that don't advance the last modified time.
            // For example, if we lose access to the network share the file is
            // stored on, we don't notify our listeners because no one wants
            // their endpoint/server to shutdown when that happens.
            // We also anticipate that a cert file might be renamed to cert.bak
            // before a new cert is introduced with the old name.
            // This also helps us in scenarios where the underlying file system
            // reports more than one change for a single logical operation.
            var lastModifiedTime = GetLastModifiedTimeOrMinimum(path, _hostEnvironment.ContentRootFileProvider);
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

            _logger.FlaggedObservers(path, configs.Count);
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

        var contentRootPath = _hostEnvironment.ContentRootPath;
        var path = Path.Combine(contentRootPath, certificateConfig.Path);

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

        if (configs.Count == 0)
        {
            fileMetadata.Dispose();
            _metadataForFile.Remove(path);

            _logger.RemovedFileWatcher(path);
        }

        _logger.ObserverCount(path, configs.Count);
        _logger.FileCount(contentRootPath, _metadataForFile.Count);
    }

    /// <remarks>Test hook</remarks>
    internal int TestGetFileWatchCountUnsynchronized() => _metadataForFile.Count;

    /// <remarks>Test hook</remarks>
    internal int TestGetObserverCountUnsynchronized(string path) => _metadataForFile.TryGetValue(path, out var metadata) ? metadata.Configs.Count : 0;

    void IDisposable.Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        foreach (var fileMetadata in _metadataForFile.Values)
        {
            fileMetadata.Dispose();
        }
    }

    private sealed class FileWatchMetadata(IDisposable disposable) : IDisposable
    {
        public readonly IDisposable Disposable = disposable;
        public readonly HashSet<CertificateConfig> Configs = new(ReferenceEqualityComparer.Instance);
        public DateTimeOffset LastModifiedTime = DateTimeOffset.MinValue;

        public void Dispose() => Disposable.Dispose();
    }
}
