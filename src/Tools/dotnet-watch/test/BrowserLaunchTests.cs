// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class BrowserLaunchTests
    {
        private readonly WatchableApp _app;

        public BrowserLaunchTests(ITestOutputHelper logger)
        {
            _app = new WatchableApp("AppWithLaunchSettings", logger);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public async Task LaunchesBrowserOnStart()
        {
            var expected = "watch : Launching browser: https://localhost:5001/";
            _app.DotnetWatchArgs.Add("--verbose");

            await _app.StartWatcherAsync();

            // Verify we launched the browser.
            await _app.Process.GetOutputLineStartsWithAsync(expected, TimeSpan.FromMinutes(2));
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public async Task RefreshesBrowserOnChange()
        {
            var launchBrowserMessage = "watch : Launching browser: https://localhost:5001/";
            var refreshBrowserMessage = "watch : Reloading browser";
            _app.DotnetWatchArgs.Add("--verbose");
            var source = Path.Combine(_app.SourceDirectory, "Program.cs");

            await _app.StartWatcherAsync();

            // Verify we launched the browser.
            await _app.Process.GetOutputLineStartsWithAsync(launchBrowserMessage, TimeSpan.FromMinutes(2));

            // Make a file change and verify we reloaded the browser.
            File.SetLastWriteTime(source, DateTime.Now);
            await _app.Process.GetOutputLineStartsWithAsync(refreshBrowserMessage, TimeSpan.FromMinutes(2));
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public async Task UsesBrowserSpecifiedInEnvironment()
        {
            var launchBrowserMessage = "watch : Launching browser: mycustombrowser.bat https://localhost:5001/";
            _app.EnvironmentVariables.Add("DOTNET_WATCH_BROWSER_PATH", "mycustombrowser.bat");

            _app.DotnetWatchArgs.Add("--verbose");

            await _app.StartWatcherAsync();

            // Verify we launched the browser.
            await _app.Process.GetOutputLineStartsWithAsync(launchBrowserMessage, TimeSpan.FromMinutes(2));
        }
    }
}
