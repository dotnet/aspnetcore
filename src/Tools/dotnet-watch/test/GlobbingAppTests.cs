// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private GlobbingApp _app;
        public GlobbingAppTests(ITestOutputHelper logger)
        {
            _app = new GlobbingApp(logger);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [QuarantinedTest]
        public async Task ChangeCompiledFile(bool usePollingWatcher)
        {
            _app.UsePollingWatcher = usePollingWatcher;
            await _app.StartWatcherAsync();

            var types = await _app.GetCompiledAppDefinedTypes();
            Assert.Equal(2, types);

            var fileToChange = Path.Combine(_app.SourceDirectory, "include", "Foo.cs");
            var programCs = File.ReadAllText(fileToChange);
            File.WriteAllText(fileToChange, programCs);

            await _app.HasRestarted();
            types = await _app.GetCompiledAppDefinedTypes();
            Assert.Equal(2, types);
        }

        [Fact]
        [QuarantinedTest]
        public async Task DeleteCompiledFile()
        {
            await _app.StartWatcherAsync();

            var types = await _app.GetCompiledAppDefinedTypes();
            Assert.Equal(2, types);

            var fileToChange = Path.Combine(_app.SourceDirectory, "include", "Foo.cs");
            File.Delete(fileToChange);

            await _app.HasRestarted();
            types = await _app.GetCompiledAppDefinedTypes();
            Assert.Equal(1, types);
        }

        [Fact]
        [QuarantinedTest]
        public async Task DeleteSourceFolder()
        {
            await _app.StartWatcherAsync();

            var types = await _app.GetCompiledAppDefinedTypes();
            Assert.Equal(2, types);

            var folderToDelete = Path.Combine(_app.SourceDirectory, "include");
            Directory.Delete(folderToDelete, recursive: true);

            await _app.HasRestarted();
            types = await _app.GetCompiledAppDefinedTypes();
            Assert.Equal(1, types);
        }

        [Fact]
        [QuarantinedTest]
        public async Task RenameCompiledFile()
        {
            await _app.StartWatcherAsync();

            var oldFile = Path.Combine(_app.SourceDirectory, "include", "Foo.cs");
            var newFile = Path.Combine(_app.SourceDirectory, "include", "Foo_new.cs");
            File.Move(oldFile, newFile);

            await _app.HasRestarted();
        }

        [Fact]
        [QuarantinedTest]
        public async Task ChangeExcludedFile()
        {
            await _app.StartWatcherAsync();

            var changedFile = Path.Combine(_app.SourceDirectory, "exclude", "Baz.cs");
            File.WriteAllText(changedFile, "");

            var restart = _app.HasRestarted();
            var finished = await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(10)), restart);
            Assert.NotSame(restart, finished);
        }

        [Fact]
        public async Task ListsFiles()
        {
            await _app.PrepareAsync();
            _app.Start(new[] { "--list" });
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            var lines = await _app.Process.GetAllOutputLinesAsync(cts.Token);
            var files = lines.Where(l => !l.StartsWith("watch :"));

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
                var definedTypesMessage = await Process.GetOutputLineStartsWithAsync("Defined types = ", TimeSpan.FromSeconds(30));
                return int.Parse(definedTypesMessage.Split('=').Last());
            }
        }
    }
}
