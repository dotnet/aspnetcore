// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.DotNet.Watcher.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class FileWatcherTests
    {
        public FileWatcherTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private const int DefaultTimeout = 10 * 1000; // 10 sec
        private readonly ITestOutputHelper _output;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NewFile(bool usePolling)
        {
            UsingTempDirectory(dir =>
            {
                using (var changedEv = new ManualResetEvent(false))
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var filesChanged = new HashSet<string>();

                    watcher.OnFileChange += (_, f) =>
                    {
                        filesChanged.Add(f);
                        changedEv.Set();
                    };
                    watcher.EnableRaisingEvents = true;

                    var testFileFullPath = Path.Combine(dir, "foo");
                    File.WriteAllText(testFileFullPath, string.Empty);

                    Assert.True(changedEv.WaitOne(DefaultTimeout));
                    Assert.Equal(testFileFullPath, filesChanged.Single());
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ChangeFile(bool usePolling)
        {
            UsingTempDirectory(dir =>
            {
                var testFileFullPath = Path.Combine(dir, "foo");
                File.WriteAllText(testFileFullPath, string.Empty);

                using (var changedEv = new ManualResetEvent(false))
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.OnFileChange -= handler;

                        filesChanged.Add(f);
                        changedEv.Set();
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    Thread.Sleep(1000);
                    File.WriteAllText(testFileFullPath, string.Empty);

                    Assert.True(changedEv.WaitOne(DefaultTimeout));
                    Assert.Equal(testFileFullPath, filesChanged.Single());
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MoveFile(bool usePolling)
        {
            UsingTempDirectory(dir =>
            {
                var srcFile = Path.Combine(dir, "foo");
                var dstFile = Path.Combine(dir, "foo2");

                File.WriteAllText(srcFile, string.Empty);

                using (var changedEv = new ManualResetEvent(false))
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        filesChanged.Add(f);

                        if (filesChanged.Count >= 2)
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.OnFileChange -= handler;

                            changedEv.Set();
                        }
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    File.Move(srcFile, dstFile);

                    Assert.True(changedEv.WaitOne(DefaultTimeout));
                    Assert.Contains(srcFile, filesChanged);
                    Assert.Contains(dstFile, filesChanged);
                }
            });
        }

        [Fact]
        public void FileInSubdirectory()
        {
            UsingTempDirectory(dir =>
            {
                var subdir = Path.Combine(dir, "subdir");
                Directory.CreateDirectory(subdir);

                var testFileFullPath = Path.Combine(subdir, "foo");
                File.WriteAllText(testFileFullPath, string.Empty);

                using (var changedEv = new ManualResetEvent(false))
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, true))
                {
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        filesChanged.Add(f);

                        if (filesChanged.Count >= 2)
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.OnFileChange -= handler;
                            changedEv.Set();
                        }
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    Thread.Sleep(1000);
                    File.WriteAllText(testFileFullPath, string.Empty);

                    Assert.True(changedEv.WaitOne(DefaultTimeout));
                    Assert.Contains(subdir, filesChanged);
                    Assert.Contains(testFileFullPath, filesChanged);
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NoNotificationIfDisabled(bool usePolling)
        {
            UsingTempDirectory(dir =>
            {
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                using (var changedEv = new ManualResetEvent(false))
                {
                    watcher.OnFileChange += (_, f) => changedEv.Set();

                    // Disable
                    watcher.EnableRaisingEvents = false;

                    var testFileFullPath = Path.Combine(dir, "foo");

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    Thread.Sleep(1000);
                    File.WriteAllText(testFileFullPath, string.Empty);

                    Assert.False(changedEv.WaitOne(DefaultTimeout / 2));
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DisposedNoEvents(bool usePolling)
        {
            UsingTempDirectory(dir =>
            {
                using (var changedEv = new ManualResetEvent(false))
                {
                    using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                    {
                        watcher.OnFileChange += (_, f) => changedEv.Set();
                        watcher.EnableRaisingEvents = true;
                    }

                    var testFileFullPath = Path.Combine(dir, "foo");

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    Thread.Sleep(1000);
                    File.WriteAllText(testFileFullPath, string.Empty);

                    Assert.False(changedEv.WaitOne(DefaultTimeout / 2));
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MultipleFiles(bool usePolling)
        {
            UsingTempDirectory(dir =>
            {
                File.WriteAllText(Path.Combine(dir, "foo1"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "foo2"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "foo3"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "foo4"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "foo4"), string.Empty);

                var testFileFullPath = Path.Combine(dir, "foo3");

                using (var changedEv = new ManualResetEvent(false))
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.OnFileChange -= handler;
                        filesChanged.Add(f);
                        changedEv.Set();
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    Thread.Sleep(1000);

                    File.WriteAllText(testFileFullPath, string.Empty);

                    Assert.True(changedEv.WaitOne(DefaultTimeout));
                    Assert.Equal(testFileFullPath, filesChanged.Single());
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MultipleTriggers(bool usePolling)
        {
            var filesChanged = new HashSet<string>();

            UsingTempDirectory(dir =>
            {
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    watcher.EnableRaisingEvents = true;

                    for (var i = 0; i < 5; i++)
                    {
                        AssertFileChangeRaisesEvent(dir, watcher);
                    }

                    watcher.EnableRaisingEvents = false;
                }
            });
        }

        private void AssertFileChangeRaisesEvent(string directory, IFileSystemWatcher watcher)
        {
            using (var semaphoreSlim = new SemaphoreSlim(0))
            {
                var expectedPath = Path.Combine(directory, Path.GetRandomFileName());
                EventHandler<string> handler = (object _, string f) =>
                {
                    _output.WriteLine("File changed: " + f);
                    try
                    {
                        if (string.Equals(f, expectedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            semaphoreSlim.Release();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // There's a known race condition here:
                        // even though we tell the watcher to stop raising events and we unsubscribe the handler
                        // there might be in-flight events that will still process. Since we dispose the reset
                        // event, this code will fail if the handler executes after Dispose happens.
                    }
                };

                File.AppendAllText(expectedPath, " ");

                watcher.OnFileChange += handler;
                try
                {
                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    Thread.Sleep(1000);
                    File.AppendAllText(expectedPath, " ");
                    Assert.True(semaphoreSlim.Wait(DefaultTimeout), "Expected a file change event for " + expectedPath);
                }
                finally
                {
                    watcher.OnFileChange -= handler;
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DeleteSubfolder(bool usePolling)
        {
            UsingTempDirectory(dir =>
            {
                var subdir = Path.Combine(dir, "subdir");
                Directory.CreateDirectory(subdir);

                var f1 = Path.Combine(subdir, "foo1");
                var f2 = Path.Combine(subdir, "foo2");
                var f3 = Path.Combine(subdir, "foo3");

                File.WriteAllText(f1, string.Empty);
                File.WriteAllText(f2, string.Empty);
                File.WriteAllText(f3, string.Empty);

                using (var changedEv = new AutoResetEvent(false))
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        filesChanged.Add(f);

                        if (filesChanged.Count >= 4)
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.OnFileChange -= handler;
                            changedEv.Set();
                        }
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    Directory.Delete(subdir, recursive: true);

                    Assert.True(changedEv.WaitOne(DefaultTimeout));

                    Assert.Contains(f1, filesChanged);
                    Assert.Contains(f2, filesChanged);
                    Assert.Contains(f3, filesChanged);
                    Assert.Contains(subdir, filesChanged);
                }
            });
        }

        private static void UsingTempDirectory(Action<string> action)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"{nameof(FileWatcherTests)}-{Guid.NewGuid().ToString("N")}");
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }

            Directory.CreateDirectory(tempFolder);

            try
            {
                action(tempFolder);
            }
            finally
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }
}
