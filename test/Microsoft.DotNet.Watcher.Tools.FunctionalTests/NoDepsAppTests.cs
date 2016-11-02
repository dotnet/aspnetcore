// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class NoDepsAppTests
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
        private readonly ITestOutputHelper _logger;

        public NoDepsAppTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        [Fact]
        public void RestartProcessOnFileChange()
        {
            using (var scenario = new NoDepsAppScenario(_logger))
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    scenario.RunDotNetWatch(new[] { "run", scenario.StatusFile, "--no-exit" });

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"File not created: {scenario.StartedFile}");
                }

                // Then wait for it to restart when we change a file
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    var fileToChange = Path.Combine(scenario.TestAppFolder, "Program.cs");
                    var programCs = File.ReadAllText(fileToChange);
                    File.WriteAllText(fileToChange, programCs);

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
                }

                // Check that the first child process is no longer running
                Waiters.WaitForFileToBeReadable(scenario.StatusFile, _defaultTimeout);
                var ids = File.ReadAllLines(scenario.StatusFile);
                var firstProcessId = int.Parse(ids[0]);
                Waiters.WaitForProcessToStop(
                    firstProcessId,
                    TimeSpan.FromSeconds(1),
                    expectedToStop: true,
                    errorMessage: $"PID: {firstProcessId} is still alive");
            }
        }

        [Fact]
        public void RestartProcessThatTerminatesAfterFileChange()
        {
            using (var scenario = new NoDepsAppScenario(_logger))
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    scenario.RunDotNetWatch(new[] { "run", scenario.StatusFile });

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"File not created: {scenario.StartedFile}");
                }

                // Then wait for the app to exit
                Waiters.WaitForFileToBeReadable(scenario.StartedFile, _defaultTimeout);
                var ids = File.ReadAllLines(scenario.StatusFile);
                var procId = int.Parse(ids[0]);
                Waiters.WaitForProcessToStop(
                    procId,
                    _defaultTimeout,
                    expectedToStop: true,
                    errorMessage: "Test app did not exit");

                // Then wait for it to restart when we change a file
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    // On Unix the file write time is in 1s increments;
                    // if we don't wait, there's a chance that the polling
                    // watcher will not detect the change
                    Thread.Sleep(1000);

                    var fileToChange = Path.Combine(scenario.TestAppFolder, "Program.cs");
                    var programCs = File.ReadAllText(fileToChange);
                    File.WriteAllText(fileToChange, programCs);

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
                }
            }
        }

        private class NoDepsAppScenario : DotNetWatchScenario
        {
            private const string TestAppName = "NoDepsApp";

            public NoDepsAppScenario(ITestOutputHelper logger)
                : base(logger)
            {
                StatusFile = Path.Combine(Scenario.TempFolder, "status");
                StartedFile = StatusFile + ".started";

                Scenario.AddTestProjectFolder(TestAppName);
                Scenario.Restore(TestAppName);

                TestAppFolder = Path.Combine(Scenario.WorkFolder, TestAppName);
            }

            public string StatusFile { get; private set; }
            public string StartedFile { get; private set; }
            public string TestAppFolder { get; private set; }

            public void RunDotNetWatch(IEnumerable<string> args)
            {
                RunDotNetWatch(args, Path.Combine(Scenario.WorkFolder, TestAppName));
            }
        }
    }
}
