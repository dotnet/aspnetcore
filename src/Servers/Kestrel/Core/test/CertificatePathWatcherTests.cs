// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
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
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        using var watcher = new CertificatePathWatcher(rootDir, logger, _ => NoChangeFileProvider.Instance);

        var changeToken = watcher.GetChangeToken();

        var certificateConfig = new CertificateConfig
        {
            Path = absoluteFilePath ? filePath : fileSubpath,
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
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider(rootDir);
        var fileLastModifiedTime = DateTimeOffset.UtcNow;
        fileProvider.SetLastModifiedTime(dirName, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime);

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
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime.AddSeconds(1));
        fileProvider.FireChangeToken(fileSubpath);

        await signalTcs.Task.DefaultTimeout();

        var newChangeToken = watcher.GetChangeToken();

        Assert.NotSame(oldChangeToken, newChangeToken);
        Assert.True(oldChangeToken.HasChanged);
        Assert.False(newChangeToken.HasChanged);

        Assert.All(certificateConfigs, cc => Assert.True(cc.FileHasChanged));
    }

    [Fact]
    public async Task FileReplacedWithLink()
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);

        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

        var linkTargetSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var linkTargetPath = Path.Combine(rootDir, linkTargetSubpath);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider(rootDir);
        var fileLastModifiedTime = DateTimeOffset.UtcNow;
        var linkTargetLastModifiedTime = fileLastModifiedTime.AddSeconds(-1); // target is *older* than original file

        fileProvider.SetLastModifiedTime(dirName, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(linkTargetSubpath, linkTargetLastModifiedTime);

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var signalTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var oldChangeToken = watcher.GetChangeToken();
        oldChangeToken.RegisterChangeCallback(_ => signalTcs.SetResult(), state: null);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
        Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(linkTargetPath));

        // Simulate file change on disk
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime.AddSeconds(1));
        fileProvider.SetLinkTarget(fileSubpath, linkTargetSubpath, fileProvider);
        fileProvider.FireChangeToken(fileSubpath);

        await signalTcs.Task.DefaultTimeout();

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(2, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(linkTargetPath));

        var newChangeToken = watcher.GetChangeToken();

        Assert.NotSame(oldChangeToken, newChangeToken);
        Assert.True(oldChangeToken.HasChanged);
        Assert.False(newChangeToken.HasChanged);

        Assert.True(certificateConfig.FileHasChanged);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FileLinkChanged(bool updatedFileIsLink)
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);

        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

        var oldLinkTargetSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var oldLinkTargetPath = Path.Combine(rootDir, oldLinkTargetSubpath);

        var newLinkTargetSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var newLinkTargetPath = Path.Combine(rootDir, newLinkTargetSubpath);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider(rootDir);
        var fileLastModifiedTime = DateTimeOffset.UtcNow;

        fileProvider.SetLastModifiedTime(dirName, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(oldLinkTargetSubpath, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(newLinkTargetSubpath, fileLastModifiedTime);

        fileProvider.SetLinkTarget(fileSubpath, oldLinkTargetSubpath, fileProvider);

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var signalTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var oldChangeToken = watcher.GetChangeToken();
        oldChangeToken.RegisterChangeCallback(_ => signalTcs.SetResult(), state: null);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(2, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(oldLinkTargetPath));
        Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(newLinkTargetPath));

        // Simulate file change on disk
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime.AddSeconds(1));
        if (updatedFileIsLink)
        {
            fileProvider.SetLinkTarget(fileSubpath, newLinkTargetSubpath, fileProvider);
        }
        else
        {
            fileProvider.RemoveLinkTarget(fileSubpath);
        }
        fileProvider.FireChangeToken(fileSubpath);

        await signalTcs.Task.DefaultTimeout();

        var newChangeToken = watcher.GetChangeToken();

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(updatedFileIsLink ? 2 : 1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
        Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(oldLinkTargetPath));
        Assert.Equal(updatedFileIsLink ? 1 : 0, watcher.TestGetObserverCountUnsynchronized(newLinkTargetPath));

        Assert.NotSame(oldChangeToken, newChangeToken);
        Assert.True(oldChangeToken.HasChanged);
        Assert.False(newChangeToken.HasChanged);

        Assert.True(certificateConfig.FileHasChanged);
    }

    [Fact]
    public async Task FileLinkTargetChanged()
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);

        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

        var linkTargetSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var linkTargetPath = Path.Combine(rootDir, linkTargetSubpath);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider(rootDir);
        var fileLastModifiedTime = DateTimeOffset.UtcNow;

        fileProvider.SetLastModifiedTime(dirName, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(linkTargetSubpath, fileLastModifiedTime);

        fileProvider.SetLinkTarget(fileSubpath, linkTargetSubpath, fileProvider);

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var signalTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var oldChangeToken = watcher.GetChangeToken();
        oldChangeToken.RegisterChangeCallback(_ => signalTcs.SetResult(), state: null);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(2, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(linkTargetPath));

        // Simulate file change on disk
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime.AddSeconds(1));
        fileProvider.SetLinkTarget(fileSubpath, linkTargetSubpath, fileProvider);
        fileProvider.FireChangeToken(fileSubpath);

        await signalTcs.Task.DefaultTimeout();

        var newChangeToken = watcher.GetChangeToken();

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(2, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(linkTargetPath));

        Assert.NotSame(oldChangeToken, newChangeToken);
        Assert.True(oldChangeToken.HasChanged);
        Assert.False(newChangeToken.HasChanged);

        Assert.True(certificateConfig.FileHasChanged);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [LogLevel(LogLevel.Trace)]
    public void FileLinkCycle(int cycleLength)
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);

        var fileSubpaths = new string[cycleLength];
        var filePaths = new string[cycleLength];

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider(rootDir);
        var fileLastModifiedTime = DateTimeOffset.UtcNow;

        fileProvider.SetLastModifiedTime(dirName, fileLastModifiedTime);

        for (int i = 0; i < cycleLength; i++)
        {
            fileSubpaths[i] = Path.Combine(dirName, Path.GetRandomFileName());
            filePaths[i] = Path.Combine(rootDir, fileSubpaths[i]);
            fileProvider.SetLastModifiedTime(fileSubpaths[i], fileLastModifiedTime);
        }

        for (int i = 0; i < cycleLength; i++)
        {
            fileProvider.SetLinkTarget(fileSubpaths[i], fileSubpaths[(i + 1) % cycleLength], fileProvider);
        }

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var logTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += writeContext =>
        {
            if (writeContext.EventId.Name == "ReusedObserver")
            {
                logTcs.SetResult();
            }
        };

        var certificateConfig = new CertificateConfig
        {
            Path = filePaths[0],
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(cycleLength, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.All(filePaths, filePath => Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath)));

        Assert.True(logTcs.Task.IsCompleted);
    }

    [Fact]
    public async Task OutOfOrderLastModifiedTime()
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider(rootDir);
        var fileLastModifiedTime = DateTimeOffset.UtcNow;

        fileProvider.SetLastModifiedTime(dirName, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime);

        using var watcher = new CertificatePathWatcher(dir, logger, _ => fileProvider);

        var certificateConfig = new CertificateConfig
        {
            Path = filePath,
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        var logTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += writeContext =>
        {
            if (writeContext.EventId.Name == "OutOfOrderEvent")
            {
                logTcs.SetResult();
            }
        };

        var oldChangeToken = watcher.GetChangeToken();

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        // Simulate file change on disk
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime.AddSeconds(-1));
        fileProvider.FireChangeToken(fileSubpath);

        await logTcs.Task.DefaultTimeout();

        Assert.False(oldChangeToken.HasChanged);
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
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

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
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

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
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

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
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [LogLevel(LogLevel.Trace)]
    public async Task IgnoreDeletion(bool seeChangeForDeletion, bool restoredWithNewerLastModifiedTime)
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        var fileProvider = new MockFileProvider(rootDir);
        var fileLastModifiedTime = DateTimeOffset.UtcNow;

        fileProvider.SetLastModifiedTime(dirName, fileLastModifiedTime);
        fileProvider.SetLastModifiedTime(fileSubpath, fileLastModifiedTime);

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
        var logSameLastModifiedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += writeContext =>
        {
            if (writeContext.EventId.Name == "EventWithoutLastModifiedTime")
            {
                logNoLastModifiedTcs.SetResult();
            }
            else if (writeContext.EventId.Name == "RedundantEvent")
            {
                logSameLastModifiedTcs.SetResult();
            }
        };

        // Simulate file deletion
        fileProvider.SetLastModifiedTime(fileSubpath, null);

        // In some file systems and watch modes, there's no event when (e.g.) the directory containing the watched file is deleted
        if (seeChangeForDeletion)
        {
            fileProvider.FireChangeToken(fileSubpath);

            await logNoLastModifiedTcs.Task.DefaultTimeout();
        }

        Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
        Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
        Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

        Assert.False(changeTcs.Task.IsCompleted);

        // Restore the file
        fileProvider.SetLastModifiedTime(fileSubpath, restoredWithNewerLastModifiedTime ? fileLastModifiedTime.AddSeconds(1) : fileLastModifiedTime);
        fileProvider.FireChangeToken(fileSubpath);

        if (restoredWithNewerLastModifiedTime)
        {
            await changeTcs.Task.DefaultTimeout();
            Assert.False(logSameLastModifiedTcs.Task.IsCompleted);
        }
        else
        {
            await logSameLastModifiedTcs.Task.DefaultTimeout();
            Assert.False(changeTcs.Task.IsCompleted);
        }
    }

    [Fact]
    public void UpdateWatches()
    {
        var rootDir = Directory.GetCurrentDirectory();
        var dirName = Path.GetRandomFileName();
        var dir = Path.Combine(rootDir, dirName);
        var fileSubpath = Path.Combine(dirName, Path.GetRandomFileName());
        var filePath = Path.Combine(rootDir, fileSubpath);

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

    private sealed class NoChangeFileProvider : IFileProviderWithLinkInfo
    {
        public static readonly IFileProviderWithLinkInfo Instance = new NoChangeFileProvider();

        private NoChangeFileProvider()
        {
        }

        IDirectoryContents IFileProvider.GetDirectoryContents(string subpath) => throw new NotSupportedException();
        IFileInfo IFileProvider.GetFileInfo(string subpath) => FixedTimeFileInfoWithLinkInfo.Instance;
        IChangeToken IFileProvider.Watch(string filter) => NoChangeChangeToken.Instance;

        IFileInfoWithLinkInfo IFileProviderWithLinkInfo.GetFileInfo(string subpath) => FixedTimeFileInfoWithLinkInfo.Instance;

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

        private sealed class FixedTimeFileInfoWithLinkInfo : IFileInfoWithLinkInfo
        {
            public static readonly IFileInfoWithLinkInfo Instance = new FixedTimeFileInfoWithLinkInfo();

            private readonly DateTimeOffset _lastModified = DateTimeOffset.UtcNow;

            private FixedTimeFileInfoWithLinkInfo()
            {
            }

            DateTimeOffset IFileInfo.LastModified => _lastModified;
            bool IFileInfo.Exists => true;
            bool IFileInfo.IsDirectory => false;

            long IFileInfo.Length => throw new NotSupportedException();
            string IFileInfo.PhysicalPath => throw new NotSupportedException();
            string IFileInfo.Name => throw new NotSupportedException();
            Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();

            IFileInfoWithLinkInfo IFileInfoWithLinkInfo.ResolveLinkTarget(bool returnFinalTarget) => null;
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

    private sealed class MockFileProvider : IFileProviderWithLinkInfo
    {
        private readonly Dictionary<string, ConfigurationReloadToken> _changeTokens = new();
        private readonly Dictionary<string, DateTimeOffset?> _lastModifiedTimes = new();
        private readonly Dictionary<string, IFileInfoWithLinkInfo> _linkTargets = new();

        private readonly string _rootPath;

        public MockFileProvider(string rootPath)
        {
            _rootPath = rootPath;
        }

        public void FireChangeToken(string subpath)
        {
            var oldChangeToken = _changeTokens[subpath];
            _changeTokens[subpath] = new ConfigurationReloadToken();
            oldChangeToken.OnReload();
        }

        public void SetLastModifiedTime(string subpath, DateTimeOffset? lastModifiedTime)
        {
            _lastModifiedTimes[subpath] = lastModifiedTime;
        }

        public void RemoveLinkTarget(string subpath)
        {
            _linkTargets.Remove(subpath);
        }

        public void SetLinkTarget(string fileSubpath, string linkTargetSubpath, IFileProviderWithLinkInfo linkTargetFileProvider)
        {
            var targetInfo = linkTargetFileProvider.GetFileInfo(linkTargetSubpath);
            _linkTargets[fileSubpath] = targetInfo;
        }

        IDirectoryContents IFileProvider.GetDirectoryContents(string subpath)
        {
            throw new NotSupportedException();
        }

        IChangeToken IFileProvider.Watch(string subpath)
        {
            if (!_changeTokens.TryGetValue(subpath, out var changeToken))
            {
                _changeTokens[subpath] = changeToken = new ConfigurationReloadToken();
            }

            return changeToken;
        }

        IFileInfo IFileProvider.GetFileInfo(string subpath)
        {
            return ((IFileProviderWithLinkInfo)this).GetFileInfo(subpath);
        }

        IFileInfoWithLinkInfo IFileProviderWithLinkInfo.GetFileInfo(string subpath)
        {
            return new MockFileInfoWithLinkInfo(Path.Combine(_rootPath, subpath), _lastModifiedTimes[subpath], _linkTargets.TryGetValue(subpath, out var target) ? target : null);
        }
    }

    private sealed class MockFileInfoWithLinkInfo : IFileInfoWithLinkInfo
    {
        private readonly string _path;
        private readonly DateTimeOffset? _lastModifiedTime;
        private readonly IFileInfoWithLinkInfo _linkTarget;

        public MockFileInfoWithLinkInfo(string path, DateTimeOffset? lastModifiedTime, IFileInfoWithLinkInfo linkTarget)
        {
            _path = path;
            _lastModifiedTime = lastModifiedTime;
            _linkTarget = linkTarget;
        }

        bool IFileInfo.Exists => _lastModifiedTime.HasValue;
        DateTimeOffset IFileInfo.LastModified => _lastModifiedTime.GetValueOrDefault();
        string IFileInfo.PhysicalPath => _path;

        long IFileInfo.Length => throw new NotSupportedException();
        string IFileInfo.Name => throw new NotSupportedException();
        bool IFileInfo.IsDirectory => throw new NotSupportedException();
        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();

        IFileInfoWithLinkInfo IFileInfoWithLinkInfo.ResolveLinkTarget(bool returnFinalTarget) => _linkTarget;
    }
}
