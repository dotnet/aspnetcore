// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class CertificatePathWatcherTests : LoggedTest
{
    private static readonly TimeSpan FileChangeTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void AddAndRemoveWatch(bool fileExists, bool absoluteFilePath)
    {
        var dirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var dir = dirInfo.FullName;
            var fileName = Path.GetRandomFileName();
            var filePath = Path.Combine(dir, fileName);

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

            var changeToken = watcher.GetChangeToken();

            var certificateConfig = new CertificateConfig
            {
                Path = absoluteFilePath ? filePath : fileName,
            };

            if (fileExists)
            {
                Touch(filePath);
            }

            Assert.Equal(fileExists, File.Exists(filePath));

            IDictionary<string, object> messageProps;

            watcher.AddWatchUnsynchronized(certificateConfig);

            messageProps = GetLogMessageProperties(TestSink, "CreatedDirectoryWatcher");
            Assert.Equal(dir, messageProps["Dir"]);

            messageProps = GetLogMessageProperties(TestSink, "CreatedFileWatcher");
            Assert.Equal(filePath, messageProps["Path"]);

            Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
            Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
            Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

            watcher.RemoveWatchUnsynchronized(certificateConfig);

            messageProps = GetLogMessageProperties(TestSink, "RemovedFileWatcher");
            Assert.Equal(filePath, messageProps["Path"]);

            messageProps = GetLogMessageProperties(TestSink, "RemovedDirectoryWatcher");
            Assert.Equal(dir, messageProps["Dir"]);

            Assert.Equal(0, watcher.TestGetDirectoryWatchCountUnsynchronized());
            Assert.Equal(0, watcher.TestGetFileWatchCountUnsynchronized(dir));
            Assert.Equal(0, watcher.TestGetObserverCountUnsynchronized(filePath));

            Assert.Same(changeToken, watcher.GetChangeToken());
            Assert.False(changeToken.HasChanged);
        }
        finally
        {
            dirInfo.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(5, 3)]
    [InlineData(5, 13)]
    public void WatchMultipleDirectories(int dirCount, int fileCount)
    {
        var rootDirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var rootDir = rootDirInfo.FullName;
            var dirs = new string[dirCount];

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(rootDir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            for (int i = 0; i < dirCount; i++)
            {
                dirs[i] = Path.Combine(rootDir, $"dir{i}");
                Directory.CreateDirectory(dirs[i]);
            }

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

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
        finally
        {
            rootDirInfo.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task FileChanged(int observerCount)
    {
        var dirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var dir = dirInfo.FullName;
            var fileName = Path.GetRandomFileName();
            var filePath = Path.Combine(dir, fileName);

            Touch(filePath);

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

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

            Touch(filePath);

            await signalTcs.Task.TimeoutAfter(FileChangeTimeout);

            var newChangeToken = watcher.GetChangeToken();

            Assert.NotSame(oldChangeToken, newChangeToken);
            Assert.True(oldChangeToken.HasChanged);
            Assert.False(newChangeToken.HasChanged);

            Assert.All(certificateConfigs, cc => Assert.True(cc.FileHasChanged));
        }
        finally
        {
            dirInfo.Delete(recursive: true);
        }
    }

    [Fact]
    public void DirectoryDoesNotExist()
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

        Assert.False(Directory.Exists(dir));

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

        var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

        using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

        var certificateConfig = new CertificateConfig
        {
            Path = Path.Combine(dir, "test.pfx"),
        };

        watcher.AddWatchUnsynchronized(certificateConfig);

        var messageProps = GetLogMessageProperties(TestSink, "DirectoryDoesNotExist");
        Assert.Equal(dir, messageProps["Dir"]);

        Assert.Equal(0, watcher.TestGetDirectoryWatchCountUnsynchronized());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemoveUnknownFileWatch(bool previouslyAdded)
    {
        var dirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var dir = dirInfo.FullName;
            var fileName = Path.GetRandomFileName();
            var filePath = Path.Combine(dir, fileName);

            Touch(filePath);

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

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
        finally
        {
            dirInfo.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RemoveUnknownFileObserver(bool previouslyAdded)
    {
        var dirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var dir = dirInfo.FullName;
            var fileName = Path.GetRandomFileName();
            var filePath = Path.Combine(dir, fileName);

            Touch(filePath);

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

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
        finally
        {
            dirInfo.Delete(recursive: true);
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public void ReuseFileObserver()
    {
        var dirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var dir = dirInfo.FullName;
            var fileName = Path.GetRandomFileName();
            var filePath = Path.Combine(dir, fileName);

            Touch(filePath);

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

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
        finally
        {
            dirInfo.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [LogLevel(LogLevel.Trace)]
    public async Task IgnoreDeletion(bool deleteDirectory)
    {
        var dirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var dir = dirInfo.FullName;
            var fileName = Path.GetRandomFileName();
            var filePath = Path.Combine(dir, fileName);

            Touch(filePath);

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

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

            var logTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            TestSink.MessageLogged += writeContext =>
            {
                if (writeContext.EventId.Name == "EventWithoutLastModifiedTime")
                {
                    logTcs.SetResult();
                }
            };

            // "Delete" the file or directory
            if (deleteDirectory)
            {
                dirInfo.MoveTo(dir + ".bak");
            }
            else
            {
                File.Move(filePath, filePath + ".bak");
            }

            Assert.False(File.Exists(filePath));

            try
            {
                await logTcs.Task.TimeoutAfter(FileChangeTimeout);
            }
            catch (TimeoutException)
            {
                // In some scenarios, the directory deletion won't trigger an event.
                // For example, a `FileSystemWatcher` on Windows won't fire one,
                // whereas the polling watcher will.
                Assert.True(deleteDirectory);
            }

            Assert.Equal(1, watcher.TestGetDirectoryWatchCountUnsynchronized());
            Assert.Equal(1, watcher.TestGetFileWatchCountUnsynchronized(dir));
            Assert.Equal(1, watcher.TestGetObserverCountUnsynchronized(filePath));

            Assert.False(changeTcs.Task.IsCompleted);

            // Restore the file or directory
            if (deleteDirectory)
            {
                dirInfo.MoveTo(dir);
            }
            else
            {
                File.Move(filePath + ".bak", filePath);
            }

            Assert.True(File.Exists(filePath));

            Touch(filePath); // In some scenarios, renaming the file back doesn't change the last modified time

            await changeTcs.Task.TimeoutAfter(FileChangeTimeout);
        }
        finally
        {
            dirInfo.Delete(recursive: true);
        }
    }

    [Fact]
    public void UpdateWatches()
    {
        var dirInfo = Directory.CreateTempSubdirectory();
        try
        {
            var dir = dirInfo.FullName;
            var fileName = Path.GetRandomFileName();
            var filePath = Path.Combine(dir, fileName);

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.SetupGet(h => h.ContentRootPath).Returns(dir);

            var logger = LoggerFactory.CreateLogger<CertificatePathWatcher>();

            using var watcher = new CertificatePathWatcher(hostEnvironment.Object, logger);

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
        finally
        {
            dirInfo.Delete(recursive: true);
        }
    }

    private static void Touch(string filePath)
    {
        File.Create(filePath).Dispose();
    }

    private static IDictionary<string, object> GetLogMessageProperties(ITestSink testSink, string eventName)
    {
        var writeContext = Assert.Single(testSink.Writes.Where(wc => wc.EventId.Name == eventName));
        var pairs = (IReadOnlyList<KeyValuePair<string, object>>)writeContext.State;
        var dict = new Dictionary<string, object>(pairs);
        return dict;
    }
}
