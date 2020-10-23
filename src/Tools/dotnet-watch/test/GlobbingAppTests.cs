// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.Watcher.Tools.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class GlobbingAppTests : IDisposable
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        private readonly GlobbingApp _app;

        public GlobbingAppTests(ITestOutputHelper logger)
        {
            _app = new GlobbingApp(logger);
        }

        [ConditionalTheory]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/27856")]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/23360", Queues = "Debian.9.Arm64;Debian.9.Arm64.Open;(Debian.9.Arm64.Open)Ubuntu.1804.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;(Debian.9.Arm64)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;(Fedora.28.Amd64.Open)Ubuntu.1604.Amd64.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:fedora-28-helix-09ca40b-20190508143249")]
        public async Task ChangeCompiledFile(bool usePollingWatcher)
        {
            _app.UsePollingWatcher = usePollingWatcher;
            await _app.StartWatcherAsync().TimeoutAfter(DefaultTimeout);

            var types = await _app.GetCompiledAppDefinedTypes().TimeoutAfter(DefaultTimeout);
            Assert.Equal(2, types);

            var fileToChange = Path.Combine(_app.SourceDirectory, "include", "Foo.cs");
            var programCs = File.ReadAllText(fileToChange);
            File.WriteAllText(fileToChange, programCs);

            await _app.HasRestarted().TimeoutAfter(DefaultTimeout);
            types = await _app.GetCompiledAppDefinedTypes().TimeoutAfter(DefaultTimeout);
            Assert.Equal(2, types);
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/25967")]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/23360", Queues = "Debian.9.Arm64;Debian.9.Arm64.Open;(Debian.9.Arm64.Open)Ubuntu.1804.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;(Debian.9.Arm64)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036")]
        public async Task DeleteCompiledFile()
        {
            await _app.StartWatcherAsync().TimeoutAfter(DefaultTimeout);

            var types = await _app.GetCompiledAppDefinedTypes().TimeoutAfter(DefaultTimeout);
            Assert.Equal(2, types);

            var fileToChange = Path.Combine(_app.SourceDirectory, "include", "Foo.cs");
            File.Delete(fileToChange);

            await _app.HasRestarted().TimeoutAfter(DefaultTimeout);
            types = await _app.GetCompiledAppDefinedTypes().TimeoutAfter(DefaultTimeout);
            Assert.Equal(1, types);
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/25967")]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/23360", Queues = "Debian.9.Arm64;Debian.9.Arm64.Open;(Debian.9.Arm64.Open)Ubuntu.1804.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;(Debian.9.Arm64)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036")]
        public async Task DeleteSourceFolder()
        {
            await _app.StartWatcherAsync().TimeoutAfter(DefaultTimeout);

            var types = await _app.GetCompiledAppDefinedTypes().TimeoutAfter(DefaultTimeout);
            Assert.Equal(2, types);

            var folderToDelete = Path.Combine(_app.SourceDirectory, "include");
            Directory.Delete(folderToDelete, recursive: true);

            await _app.HasRestarted().TimeoutAfter(DefaultTimeout);
            types = await _app.GetCompiledAppDefinedTypes().TimeoutAfter(DefaultTimeout);
            Assert.Equal(1, types);
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/25967")]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/23360", Queues = "Debian.9.Arm64;Debian.9.Arm64.Open;(Debian.9.Arm64.Open)Ubuntu.1804.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;(Debian.9.Arm64)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036")]
        public async Task RenameCompiledFile()
        {
            await _app.StartWatcherAsync().TimeoutAfter(DefaultTimeout);

            var oldFile = Path.Combine(_app.SourceDirectory, "include", "Foo.cs");
            var newFile = Path.Combine(_app.SourceDirectory, "include", "Foo_new.cs");
            File.Move(oldFile, newFile);

            await _app.HasRestarted().TimeoutAfter(DefaultTimeout);
        }

        [Fact]
        public async Task ChangeExcludedFile()
        {
            await _app.StartWatcherAsync().TimeoutAfter(DefaultTimeout);

            var changedFile = Path.Combine(_app.SourceDirectory, "exclude", "Baz.cs");
            File.WriteAllText(changedFile, "");

            var restart = _app.HasRestarted();
            var finished = await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(10)), restart);
            Assert.NotSame(restart, finished);
        }

        [Fact]
        public async Task ListsFiles()
        {
            await _app.PrepareAsync().TimeoutAfter(DefaultTimeout);
            _app.Start(new[] { "--list" });
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            var lines = await _app.Process.GetAllOutputLinesAsync(cts.Token).TimeoutAfter(DefaultTimeout);
            var files = lines.Where(l => !l.StartsWith("watch :", StringComparison.Ordinal));

            AssertEx.EqualFileList(
                _app.Scenario.WorkFolder,
                new[]
                {
                    "GlobbingApp/Program.cs",
                    "GlobbingApp/include/Foo.cs",
                    "GlobbingApp/GlobbingApp.csproj",
                },
                files);
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        private class GlobbingApp : WatchableApp
        {
            public GlobbingApp(ITestOutputHelper logger)
                : base("GlobbingApp", logger)
            {
            }

            public async Task<int> GetCompiledAppDefinedTypes()
            {
                var definedTypesMessage = await Process!.GetOutputLineStartsWithAsync("Defined types = ", TimeSpan.FromSeconds(30));
                return int.Parse(definedTypesMessage.Split('=').Last(), CultureInfo.InvariantCulture);
            }
        }
    }
}
