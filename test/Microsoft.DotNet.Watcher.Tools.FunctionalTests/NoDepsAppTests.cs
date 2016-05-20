// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class NoDepsAppTests
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        [Fact]
        public void RestartProcessOnFileChange()
        {
            using (var scenario = new NoDepsAppScenario())
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    scenario.RunDotNetWatch($"{scenario.StatusFile} --no-exit");

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
            using (var scenario = new NoDepsAppScenario())
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    scenario.RunDotNetWatch(scenario.StatusFile);

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
                    var fileToChange = Path.Combine(scenario.TestAppFolder, "Program.cs");
                    var programCs = File.ReadAllText(fileToChange);
                    File.WriteAllText(fileToChange, programCs);

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
                }
            }
        }


        [Fact]
        public void ExitOnChange()
        {
            using (var scenario = new NoDepsAppScenario())
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    scenario.RunDotNetWatch($"--exit-on-change -- {scenario.StatusFile} --no-exit");

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"File not created: {scenario.StartedFile}");
                }

                // Change a file
                var fileToChange = Path.Combine(scenario.TestAppFolder, "Program.cs");
                var programCs = File.ReadAllText(fileToChange);
                File.WriteAllText(fileToChange, programCs);

                Waiters.WaitForProcessToStop(
                    scenario.WatcherProcess.Id,
                    _defaultTimeout,
                    expectedToStop: true,
                    errorMessage: "The watcher did not stop");

                // Check that the first child process is no longer running
                var ids = File.ReadAllLines(scenario.StatusFile);
                var firstProcessId = int.Parse(ids[0]);
                Waiters.WaitForProcessToStop(
                    firstProcessId,
                    TimeSpan.FromSeconds(1),
                    expectedToStop: true,
                    errorMessage: $"PID: {firstProcessId} is still alive");
            }
        }

        private class NoDepsAppScenario : DotNetWatchScenario
        {
            private const string TestAppName = "NoDepsApp";
            private static readonly string _testAppFolder = Path.Combine(_repositoryRoot, "test", "TestApps", TestAppName);

            public NoDepsAppScenario()
            {
                StatusFile = Path.Combine(_scenario.TempFolder, "status");
                StartedFile = StatusFile + ".started";

                _scenario.AddProject(_testAppFolder);
                _scenario.AddToolToProject(TestAppName, DotnetWatch);
                _scenario.Restore();

                TestAppFolder = Path.Combine(_scenario.WorkFolder, TestAppName);
            }

            public string StatusFile { get; private set; }
            public string StartedFile { get; private set; }
            public string TestAppFolder { get; private set; }

            public void RunDotNetWatch(string args)
            {
                RunDotNetWatch(args, Path.Combine(_scenario.WorkFolder, TestAppName));
            }
        }
    }
}
