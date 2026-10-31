// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class CertificatePathWatcherTests : LoggedTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAndRemoveWatch(bool absoluteFilePath)
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        using var watcher = new CertificatePathWatcher(dir, logger, _ => NoChangeFileProvider.Instance);

        var changeToken = watcher.GetChangeToken();

        var certificateConfig = new CertificateConfig
        {
            Path = absoluteFilePath ? filePath : fileName,
        };

        IDictionary<string, object> messageProps;

        watcher.AddWatchUnsynchronized(certificateConfig);

        messageProps = GetLogMessageProperties(TestSink, "CreatedDirectoryWatcher");
        Assert.Equal(dir, messageProps["Directory"]);

        messageProps = GetLogMessageProperties(TestSink, "CreatedFileWatcher");
        Assert.Equal(filePath, messageProps["Path"]);

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        watcher.RemoveWatchUnsynchronized(certificateConfig);

        messageProps = GetLogMessageProperties(TestSink, "RemovedFileWatcher");
        Assert.Equal(filePath, messageProps["Path"]);

        messageProps = GetLogMessageProperties(TestSink, "RemovedDirectoryWatcher");
        Assert.Equal(dir, messageProps["Directory"]);

        Assert.Equal(0, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(0, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(filePath));

        Assert.Same(changeToken, watcher.GetChangeToken());
        Assert.False(changeToken.HasChanged);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(5, 3)]
    [InlineData(5, 13)]
    public void WatchMultipleDirectories(int dirCount, int fileCount)
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirs = new string[dirCount];

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        for (int i = 0; i < dirCount; i++)
        {
            dirs[i] = Path.Combine(rootDir, $"dir{i}");
        }

        using var watcher = new CertificatePathWatcher(rootDir, logger, _ => NoChangeFileProvider.Instance);

        var certificateConfigs = new CertificateConfig[fileCount];
        var filesInDir = new int[dirCount];
        for (int i = 0; i < fileCount; i++)
        {
            certificateConfigs[i] = new CertificateConfig
            {
                Path = $"dir{i % dirCount}/file{i % fileCount}",
            };
            filesInDir[i % dirCount]++;
        }

        foreach (var certificateConfig in certificateConfigs)
        {
            watcher.AddWatchUnsynchronized(certificateConfig);
        }

        Assert.Equal(Math.Min(dirCount, fileCount), watcher.TestGetDirectoryWatchCountUnsynchronized());

        for (int i = 0; i < dirCount; i++)
        {
            Assert.Equal(filesInDir[i], watcher.TestGetFileWatchCountUnsynchronized(dirs[i]));
        }

        foreach (var certificateConfig in certificateConfigs)
        {
            watcher.RemoveWatchUnsynchronized(certificateConfig);
        }

        Assert.Equal(0, watcher.TestGetDirectoryWatchCountUnsynchronized());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task FileChanged(int observerCount)
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider();
        var fileLastModifiedTime = DateTimeOffset.UtcNow;
        fileProvider.SetLastModifiedTime(fileName, fileLastModifiedTime);

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var signalTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var oldChangeToken = watcher.GetChangeToken();
        oldChangeToken.RegisterChangeCallback(_ => signalTcs.SetResult(), state: null);

        var certificateConfigs = new CertificateConfig[observerCount];
        for (int i = 0; i < observerCount; i++)
        {
            certificateConfigs[i] = new CertificateConfig
            {
                Path = filePath,
            };

            watcher.AddWatchUnsynchronized(certificateConfigs[i]);
        }

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(observerCount, watcher.TestGetObserverCountUnsynchronized(filePath));

        // Simulate file change on disk
        fileProvider.SetLastModifiedTime(fileName, fileLastModifiedTime.AddSeconds(1));
        fileProvider.FireChangeToken(fileName);

        await signalTcs.Task.DefaultTimeout();

        var newChangeToken = watcher.GetChangeToken();

        Assert.NotSame(oldChangeToken, newChangeToken);
        Assert.True(oldChangeToken.HasChanged);
        Assert.False(newChangeToken.HasChanged);

        Assert.All(certificateConfigs, cc => Assert.True(cc.FileHasChanged));
    }

    [Fact]
    public async Task OutOfOrderLastModifiedTime()
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider();
        var fileLastModifiedTime = DateTimeOffset.UtcNow;
        fileProvider.SetLastModifiedTime(fileName, fileLastModifiedTime);

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        var signalTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var oldChangeToken = watcher.GetChangeToken();
        oldChangeToken.RegisterChangeCallback(_ => signalTcs.SetResult(), state: null);

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        // Simulate file change on disk
        fileProvider.SetLastModifiedTime(fileName, fileLastModifiedTime.AddSeconds(-1));
        fileProvider.FireChangeToken(fileName);

        await signalTcs.Task.DefaultTimeout();

        Assert.True(oldChangeToken.HasChanged);
    }

    [Fact]
    public void DirectoryDoesNotExist()
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

        Assert.False(Directory.Exists(dir));

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        // Returning null indicates that the directory does not exist
        using var watcher = new CertificatePathWatcher(dir, logger, _ => null);

        var certificateConfig = new CertificateConfig
        {
            Path = Path.Combine(dir, "test.pfx"),
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        var messageProps = GetLogMessageProperties(TestSink, "DirectoryDoesNotExist");
        Assert.Equal(dir, messageProps["Directory"]);

        Assert.Equal(0, watcher.TestGetDirectoryWatchCountUnsynchronized());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemoveUnknownFileWatch(bool previouslyAdded)
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        using var watcher = new CertificatePathWatcher(dir, logger, _ => NoChangeFileProvider.Instance);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        if (previouslyAdded)
        {
            watcher.AddWatchUnsynchronized(certificateConfig);
            watcher.RemoveWatchUnsynchronized(certificateConfig);
        }

        Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(filePath));

        watcher.RemoveWatchUnsynchronized(certificateConfig);

        var messageProps = GetLogMessageProperties(TestSink, "UnknownFile");
        Assert.Equal(filePath, messageProps["Path"]);

        Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(filePath));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemoveUnknownFileObserver(bool previouslyAdded)
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        using var watcher = new CertificatePathWatcher(dir, logger, _ => NoChangeFileProvider.Instance);

        var certificateConfig1 = new CertificateConfig
        {
            Path = filePath,
        };

        var certificateConfig2 = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig1);

        if (previouslyAdded)
        {
            watcher.AddWatchUnsynchronized(certificateConfig2);
            watcher.RemoveWatchUnsynchronized(certificateConfig2);
        }

        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        watcher.RemoveWatchUnsynchronized(certificateConfig2);

        var messageProps = GetLogMessageProperties(TestSink, "UnknownObserver");
        Assert.Equal(filePath, messageProps["Path"]);

        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public void ReuseFileObserver()
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        using var watcher = new CertificatePathWatcher(dir, logger, _ => NoChangeFileProvider.Instance);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        watcher.AddWatchUnsynchronized(certificateConfig);

        var messageProps = GetLogMessageProperties(TestSink, "ReusedObserver");
        Assert.Equal(filePath, messageProps["Path"]);

        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [LogLevel(LogLevel.Trace)]
    public async Task IgnoreDeletion(bool restoredWithNewerLastModifiedTime)
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider();
        var fileLastModifiedTime = DateTimeOffset.UtcNow;
        fileProvider.SetLastModifiedTime(fileName, fileLastModifiedTime);

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        var changeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        watcher.GetChangeToken().RegisterChangeCallback(_ => changeTcs.SetResult(), state: null);

        var logNoLastModifiedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += writeContext =>
        {
            if (writeContext.EventId.Name == "EventWithoutFile")
            {
                logNoLastModifiedTcs.SetResult();
            }
        };

        // Simulate file deletion
        fileProvider.SetLastModifiedTime(fileName, null);

        fileProvider.FireChangeToken(fileName);

        await logNoLastModifiedTcs.Task.DefaultTimeout();

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        Assert.False(changeTcs.Task.IsCompleted);

        // Restore the file
        fileProvider.SetLastModifiedTime(fileName, restoredWithNewerLastModifiedTime ? fileLastModifiedTime.AddSeconds(1) : fileLastModifiedTime);
        fileProvider.FireChangeToken(fileName);

        await changeTcs.Task.DefaultTimeout();
    }

    [Fact]
    public void UpdateWatches()
    {
        var dir = Directory.GetCurrentDirectory();
        var fileName = Path.GetRandomFileName();
        var filePath = Path.Combine(dir, fileName);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        using var watcher = new CertificatePathWatcher(dir, logger, _ => NoChangeFileProvider.Instance);

        var changeToken = watcher.GetChangeToken();

        var certificateConfig1 = new CertificateConfig
        {
            Path = filePath,
        };

        var certificateConfig2 = new CertificateConfig
        {
            Path = filePath,
        };

        var certificateConfig3 = new CertificateConfig
        {
            Path = filePath,
        };

        // Add certificateConfig1
        watcher.UpdateWatches(new List<CertificateConfig> { }, new List<CertificateConfig> { certificateConfig1 });

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        // Remove certificateConfig1
        watcher.UpdateWatches(new List<CertificateConfig> { certificateConfig1 }, new List<CertificateConfig> { });

        Assert.Equal(0, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(0, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(filePath));

        // Re-add certificateConfig1
        watcher.UpdateWatches(new List<CertificateConfig> { }, new List<CertificateConfig> { certificateConfig1 });

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        watcher.UpdateWatches(
            new List<CertificateConfig>
            {
                certificateConfig1, // Delete something present
                certificateConfig1, // Delete it again
                certificateConfig2, // Delete something never added
                certificateConfig2, // Delete it again
            },
            new List<CertificateConfig>
            {
                certificateConfig1, // Re-add something removed above
                certificateConfig1, // Re-add it again
                certificateConfig2, // Add something vacuously removed above
                certificateConfig2, // Add it again
                certificateConfig3, // Add something new
                certificateConfig3, // Add it again
            });

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(3, watcher.TestGetObserverCountUnsynchronized(filePath));
    }

    private static IDictionary<string, object> GetLogMessageProperties(ITestSink testSink, string eventName)
    {
        var writeContext = Assert.Single(testSink.Writes.Where(wc => wc.EventId.Name == eventName));
        var pairs = (IReadOnlyList<KeyValuePair<string, object>>)writeContext.State;
        var dict = new Dictionary<string, object>(pairs);
        return dict;
    }

    private sealed class NoChangeFileProvider : IFileProvider
    {
        public static readonly IFileProvider Instance = new NoChangeFileProvider();

        private NoChangeFileProvider()
        {
        }

        IDirectoryContents IFileProvider.GetDirectoryContents(string subpath) => throw new NotSupportedException();
        IFileInfo IFileProvider.GetFileInfo(string subpath) => throw new NotSupportedException();
        IChangeToken IFileProvider.Watch(string filter) => NoChangeChangeToken.Instance;

        private sealed class NoChangeChangeToken : IChangeToken
        {
            public static readonly IChangeToken Instance = new NoChangeChangeToken();

            private NoChangeChangeToken()
            {
            }

            bool IChangeToken.HasChanged => false;
            bool IChangeToken.ActiveChangeCallbacks => true;
            IDisposable IChangeToken.RegisterChangeCallback(Action<object> callback, object state) => DummyDisposable.Instance;
        }
    }

    private sealed class DummyDisposable : IDisposable
    {
        public static readonly IDisposable Instance = new DummyDisposable();

        private DummyDisposable()
        {
        }

        void IDisposable.Dispose()
        {
        }
    }

    private sealed class MockFileProvider : IFileProvider
    {
        private readonly Dictionary<string, ConfigurationReloadToken> _changeTokens = new();
        private readonly Dictionary<string, DateTimeOffset?> _lastModifiedTimes = new();

        public void FireChangeToken(string path)
        {
            var oldChangeToken = _changeTokens[path];
            _changeTokens[path] = new ConfigurationReloadToken();
            oldChangeToken.OnReload();
        }

        public void SetLastModifiedTime(string path, DateTimeOffset? lastModifiedTime)
        {
            _lastModifiedTimes[path] = lastModifiedTime;
        }

        IDirectoryContents IFileProvider.GetDirectoryContents(string subpath)
        {
            throw new NotSupportedException();
        }

        IFileInfo IFileProvider.GetFileInfo(string subpath)
        {
            return new MockFileInfo(_lastModifiedTimes[subpath]);
        }

        IChangeToken IFileProvider.Watch(string path)
        {
            if (!_changeTokens.TryGetValue(path, out var changeToken))
            {
                _changeTokens[path] = changeToken = new ConfigurationReloadToken();
            }

            return changeToken;
        }

        private sealed class MockFileInfo : IFileInfo
        {
            private readonly DateTimeOffset? _lastModifiedTime;

            public MockFileInfo(DateTimeOffset? lastModifiedTime)
            {
                _lastModifiedTime = lastModifiedTime;
            }

            bool IFileInfo.Exists => _lastModifiedTime.HasValue;
            DateTimeOffset IFileInfo.LastModified => _lastModifiedTime.GetValueOrDefault();

            long IFileInfo.Length => throw new NotSupportedException();
            string IFileInfo.PhysicalPath => throw new NotSupportedException();
            string IFileInfo.Name => throw new NotSupportedException();
            bool IFileInfo.IsDirectory => throw new NotSupportedException();
            Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();
        }
    }
}
