// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class DotNetWatcherTests : IDisposable
    {
        private readonly ITestOutputHelper _logger;
        private readonly KitchenSinkApp _app;

        public DotNetWatcherTests(ITestOutputHelper logger)
        {
            _logger = logger;
            _app = new KitchenSinkApp(logger);
        }

        [Fact]
        public async Task RunsWithDotnetWatchEnvVariable()
        {
            Assert.True(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_WATCH")), "DOTNET_WATCH cannot be set already when this test is running");

            await _app.StartWatcherAsync();
            const string messagePrefix = "DOTNET_WATCH = ";
            var message = await _app.Process.GetOutputLineStartsWithAsync(messagePrefix, TimeSpan.FromMinutes(2));
            var envValue = message.Substring(messagePrefix.Length);
            Assert.Equal("1", envValue);
        }

        [Fact]
        public async Task RunsWithIterationEnvVariable()
        {
            await _app.StartWatcherAsync();
            var source = Path.Combine(_app.SourceDirectory, "Program.cs");
            var contents = File.ReadAllText(source);
            const string messagePrefix = "DOTNET_WATCH_ITERATION = ";
            for (var i = 1; i <= 3; i++)
            {
                var message = await _app.Process.GetOutputLineStartsWithAsync(messagePrefix, TimeSpan.FromMinutes(2));
                var count = int.Parse(message.Substring(messagePrefix.Length), CultureInfo.InvariantCulture);
                Assert.Equal(i, count);

                await _app.IsWaitingForFileChange();

                File.SetLastWriteTime(source, DateTime.Now);
                await _app.HasRestarted(TimeSpan.FromMinutes(1));
            }
        }

        [Fact]
        public async Task RunsWithNoRestoreOnOrdinaryFileChanges()
        {
            _app.DotnetWatchArgs.Add("--verbose");

            await _app.StartWatcherAsync(arguments: new[] { "wait" });
            var source = Path.Combine(_app.SourceDirectory, "Program.cs");
            const string messagePrefix = "watch : Running dotnet with the following arguments: run";

            // Verify that the first run does not use --no-restore
            Assert.Contains(_app.Process.Output, p => string.Equals(messagePrefix + " -- wait", p.Trim()));

            for (var i = 0; i < 3; i++)
            {
                File.SetLastWriteTime(source, DateTime.Now);
                var message = await _app.Process.GetOutputLineStartsWithAsync(messagePrefix, TimeSpan.FromMinutes(2));

                Assert.Equal(messagePrefix + " --no-restore -- wait", message.Trim());
            }
        }

        [Fact]
        public async Task RunsWithRestoreIfCsprojChanges()
        {
            _app.DotnetWatchArgs.Add("--verbose");

            await _app.StartWatcherAsync(arguments: new[] { "wait" });
            var source = Path.Combine(_app.SourceDirectory, "KitchenSink.csproj");
            const string messagePrefix = "watch : Running dotnet with the following arguments: run";

            // Verify that the first run does not use --no-restore
            Assert.Contains(_app.Process.Output, p => string.Equals(messagePrefix + " -- wait", p.Trim()));

            File.SetLastWriteTime(source, DateTime.Now);
            var message = await _app.Process.GetOutputLineStartsWithAsync(messagePrefix, TimeSpan.FromMinutes(2));

            // csproj changed. Do not expect a --no-restore
            Assert.Equal(messagePrefix + " -- wait", message.Trim());

            // regular file changed after csproj changes. Should use --no-restore
            File.SetLastWriteTime(Path.Combine(_app.SourceDirectory, "Program.cs"), DateTime.Now);
            message = await _app.Process.GetOutputLineStartsWithAsync(messagePrefix, TimeSpan.FromMinutes(2));
            Assert.Equal(messagePrefix + " --no-restore -- wait", message.Trim());
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        private class KitchenSinkApp : WatchableApp
        {
            public KitchenSinkApp(ITestOutputHelper logger)
                : base("KitchenSink", logger)
            {
            }
        }
    }
}
