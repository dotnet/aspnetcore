// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
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

        private readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
        private readonly TimeSpan NegativeTimeout = TimeSpan.FromSeconds(5);
        private readonly ITestOutputHelper _output;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NewFile(bool usePolling)
        {
            await UsingTempDirectory(async dir =>
            {
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var filesChanged = new HashSet<string>();

                    watcher.OnFileChange += (_, f) =>
                    {
                        filesChanged.Add(f);
                        changedEv.TrySetResult(0);
                    };
                    watcher.EnableRaisingEvents = true;

                    var testFileFullPath = Path.Combine(dir, "foo");
                    File.WriteAllText(testFileFullPath, string.Empty);

                    await changedEv.Task.TimeoutAfter(DefaultTimeout);
                    Assert.Equal(testFileFullPath, filesChanged.Single());
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ChangeFile(bool usePolling)
        {
            await UsingTempDirectory(async dir =>
            {
                var testFileFullPath = Path.Combine(dir, "foo");
                File.WriteAllText(testFileFullPath, string.Empty);

                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.OnFileChange -= handler;

                        filesChanged.Add(f);
                        changedEv.TrySetResult(0);
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    await Task.Delay(1000);
                    File.WriteAllText(testFileFullPath, string.Empty);

                    await changedEv.Task.TimeoutAfter(DefaultTimeout);
                    Assert.Equal(testFileFullPath, filesChanged.Single());
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MoveFile(bool usePolling)
        {
            await UsingTempDirectory(async dir =>
            {
                var srcFile = Path.Combine(dir, "foo");
                var dstFile = Path.Combine(dir, "foo2");

                File.WriteAllText(srcFile, string.Empty);

                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        filesChanged.Add(f);

                        if (filesChanged.Count >= 2)
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.OnFileChange -= handler;

                            changedEv.TrySetResult(0);
                        }
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    File.Move(srcFile, dstFile);

                    await changedEv.Task.TimeoutAfter(DefaultTimeout);
                    Assert.Contains(srcFile, filesChanged);
                    Assert.Contains(dstFile, filesChanged);
                }
            });
        }

        [Fact]
        public async Task FileInSubdirectory()
        {
            await UsingTempDirectory(async dir =>
            {
                var subdir = Path.Combine(dir, "subdir");
                Directory.CreateDirectory(subdir);

                var testFileFullPath = Path.Combine(subdir, "foo");
                File.WriteAllText(testFileFullPath, string.Empty);

                using (var watcher = FileWatcherFactory.CreateWatcher(dir, true))
                {
                    var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        filesChanged.Add(f);

                        if (filesChanged.Count >= 2)
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.OnFileChange -= handler;
                            changedEv.TrySetResult(0);
                        }
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    await Task.Delay(1000);
                    File.WriteAllText(testFileFullPath, string.Empty);

                    await changedEv.Task.TimeoutAfter(DefaultTimeout);
                    Assert.Contains(subdir, filesChanged);
                    Assert.Contains(testFileFullPath, filesChanged);
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NoNotificationIfDisabled(bool usePolling)
        {
            await UsingTempDirectory(async dir =>
            {
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    watcher.OnFileChange += (_, f) => changedEv.TrySetResult(0);

                    // Disable
                    watcher.EnableRaisingEvents = false;

                    var testFileFullPath = Path.Combine(dir, "foo");

                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    await Task.Delay(1000);
                    File.WriteAllText(testFileFullPath, string.Empty);

                    await Assert.ThrowsAsync<TimeoutException>(() => changedEv.Task.TimeoutAfter(NegativeTimeout));
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DisposedNoEvents(bool usePolling)
        {
            await UsingTempDirectory(async dir =>
            {
                var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    watcher.OnFileChange += (_, f) => changedEv.TrySetResult(0);
                    watcher.EnableRaisingEvents = true;
                }

                var testFileFullPath = Path.Combine(dir, "foo");

                // On Unix the file write time is in 1s increments;
                // if we don't wait, there's a chance that the polling
                // watcher will not detect the change
                await Task.Delay(1000);
                File.WriteAllText(testFileFullPath, string.Empty);

                await Assert.ThrowsAsync<TimeoutException>(() => changedEv.Task.TimeoutAfter(NegativeTimeout));
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MultipleFiles(bool usePolling)
        {
            await UsingTempDirectory(async dir =>
            {
                File.WriteAllText(Path.Combine(dir, "foo1"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "foo2"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "foo3"), string.Empty);
                File.WriteAllText(Path.Combine(dir, "foo4"), string.Empty);

                // On Unix the native file watcher may surface events from
                // the recent past. Delay to avoid those.
                // On Unix the file write time is in 1s increments;
                // if we don't wait, there's a chance that the polling
                // watcher will not detect the change
                await Task.Delay(1250);

                var testFileFullPath = Path.Combine(dir, "foo3");

                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.OnFileChange -= handler;
                        filesChanged.Add(f);
                        changedEv.TrySetResult(0);
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    File.WriteAllText(testFileFullPath, string.Empty);

                    await changedEv.Task.TimeoutAfter(DefaultTimeout);
                    Assert.Equal(testFileFullPath, filesChanged.Single());
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MultipleTriggers(bool usePolling)
        {
            var filesChanged = new HashSet<string>();

            await UsingTempDirectory(async dir =>
            {
                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    watcher.EnableRaisingEvents = true;

                    for (var i = 0; i < 5; i++)
                    {
                        await AssertFileChangeRaisesEvent(dir, watcher);
                    }

                    watcher.EnableRaisingEvents = false;
                }
            });
        }

        private async Task AssertFileChangeRaisesEvent(string directory, IFileSystemWatcher watcher)
        {
            var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var expectedPath = Path.Combine(directory, Path.GetRandomFileName());
            EventHandler<string> handler = (object _, string f) =>
            {
                _output.WriteLine("File changed: " + f);
                try
                {
                    if (string.Equals(f, expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        changedEv.TrySetResult(0);
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
                await Task.Delay(1000);
                File.AppendAllText(expectedPath, " ");
                await changedEv.Task.TimeoutAfter(DefaultTimeout);
            }
            finally
            {
                watcher.OnFileChange -= handler;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DeleteSubfolder(bool usePolling)
        {
            await UsingTempDirectory(async dir =>
            {
                var subdir = Path.Combine(dir, "subdir");
                Directory.CreateDirectory(subdir);

                var f1 = Path.Combine(subdir, "foo1");
                var f2 = Path.Combine(subdir, "foo2");
                var f3 = Path.Combine(subdir, "foo3");

                File.WriteAllText(f1, string.Empty);
                File.WriteAllText(f2, string.Empty);
                File.WriteAllText(f3, string.Empty);

                using (var watcher = FileWatcherFactory.CreateWatcher(dir, usePolling))
                {
                    var changedEv = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var filesChanged = new HashSet<string>();

                    EventHandler<string> handler = null;
                    handler = (_, f) =>
                    {
                        filesChanged.Add(f);

                        if (filesChanged.Count >= 4)
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.OnFileChange -= handler;
                            changedEv.TrySetResult(0);
                        }
                    };

                    watcher.OnFileChange += handler;
                    watcher.EnableRaisingEvents = true;

                    Directory.Delete(subdir, recursive: true);

                    await changedEv.Task.TimeoutAfter(DefaultTimeout);

                    Assert.Contains(f1, filesChanged);
                    Assert.Contains(f2, filesChanged);
                    Assert.Contains(f3, filesChanged);
                    Assert.Contains(subdir, filesChanged);
                }
            });
        }

        private static async Task UsingTempDirectory(Func<string, Task> func)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), $"{nameof(FileWatcherTests)}-{Guid.NewGuid().ToString("N")}");
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }

            Directory.CreateDirectory(tempFolder);

            try
            {
                await func(tempFolder);
            }
            finally
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }
}
