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
    public class AppWithDepsTests : IDisposable
    {
        private readonly AppWithDeps _app;

        public AppWithDepsTests(ITestOutputHelper logger)
        {
            _app = new AppWithDeps(logger);
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/23994")]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/23360", Queues = "Windows.10.Arm64;Windows.10.Arm64.Open;Debian.9.Arm64;Debian.9.Arm64.Open;(Debian.9.Arm64.Open)Ubuntu.1804.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;(Debian.9.Arm64)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-9-helix-arm64v8-a12566d-20190807161036;Windows.10.Arm64v8;Windows.10.Arm64v8.Open")]
        public async Task ChangeFileInDependency()
        {
            await _app.StartWatcherAsync();

            var fileToChange = Path.Combine(_app.DependencyFolder, "Foo.cs");
            var programCs = File.ReadAllText(fileToChange);
            File.WriteAllText(fileToChange, programCs);

            await _app.HasRestarted();
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        private class AppWithDeps : WatchableApp
        {
            private const string Dependency = "Dependency";

            public AppWithDeps(ITestOutputHelper logger)
                : base("AppWithDeps", logger)
            {
                Scenario.AddTestProjectFolder(Dependency);

                DependencyFolder = Path.Combine(Scenario.WorkFolder, Dependency);
            }

            public string DependencyFolder { get; private set; }
        }
    }
}
