// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class NoDepsAppTests : IDisposable
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly WatchableApp _app;
        private readonly ITestOutputHelper _output;

        public NoDepsAppTests(ITestOutputHelper logger)
        {
            _app = new WatchableApp("NoDepsApp", logger);
            _output = logger;
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/23360", Queues = "Debian.9.Arm64;Debian.9.Arm64.Open;(Debian.9.Arm64.Open)Ubuntu.1804.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;(Debian.9.Arm64)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036")]
        public async Task RestartProcessOnFileChange()
        {
            await _app.StartWatcherAsync(new[] { "--no-exit" });
            var processIdentifier = await _app.GetProcessIdentifier();

            // Then wait for it to restart when we change a file
            var fileToChange = Path.Combine(_app.SourceDirectory, "Program.cs");
            var programCs = File.ReadAllText(fileToChange);
            File.WriteAllText(fileToChange, programCs);

            await _app.HasRestarted();
            Assert.DoesNotContain(_app.Process.Output, l => l.StartsWith("Exited with error code", StringComparison.Ordinal));

            var processIdentifier2 = await _app.GetProcessIdentifier();
            Assert.NotEqual(processIdentifier, processIdentifier2);
        }

        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/24841", Queues = "Windows.10.Arm64;Windows.10.Arm64.Open;Windows.10.Arm64v8;Windows.10.Arm64v8.Open")]
        public async Task RestartProcessThatTerminatesAfterFileChange()
        {
            await _app.StartWatcherAsync();
            var processIdentifier = await _app.GetProcessIdentifier();
            await _app.HasExited(); // process should exit after run
            await _app.IsWaitingForFileChange();

            var fileToChange = Path.Combine(_app.SourceDirectory, "Program.cs");

            try
            {
                File.SetLastWriteTime(fileToChange, DateTime.Now);
                await _app.HasRestarted();
            }
            catch
            {
                // retry
                File.SetLastWriteTime(fileToChange, DateTime.Now);
                await _app.HasRestarted();
            }

            var processIdentifier2 = await _app.GetProcessIdentifier();
            Assert.NotEqual(processIdentifier, processIdentifier2);
            await _app.HasExited(); // process should exit after run
        }

        public void Dispose()
        {
            _app.Dispose();
        }
    }
}
