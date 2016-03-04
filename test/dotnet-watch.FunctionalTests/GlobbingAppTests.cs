// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace Microsoft.DotNet.Watcher.FunctionalTests
{
    public class GlobbingAppTests
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan _negativeTestWaitTime = TimeSpan.FromSeconds(10);

        // Change a file included in compilation
        [Fact(Skip = "Disabled temporary")]
        public void ChangeCompiledFile()
        {
            using (var scenario = new GlobbingAppScenario())
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                var fileToChange = Path.Combine(scenario.TestAppFolder, "include", "Foo.cs");
                var programCs = File.ReadAllText(fileToChange);
                File.WriteAllText(fileToChange, programCs);

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }

        }

        // Add a file to a folder included in compilation
        [Fact(Skip = "Disabled temporary")]
        public void AddCompiledFile()
        {
            // Add a file in a folder that's included in compilation
            using (var scenario = new GlobbingAppScenario())
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                var fileToChange = Path.Combine(scenario.TestAppFolder, "include", "Bar.cs");
                File.WriteAllText(fileToChange, "");

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }
        }

        // Delete a file included in compilation
        [Fact(Skip = "Disabled temporary")]
        public void DeleteCompiledFile()
        {
            using (var scenario = new GlobbingAppScenario())
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                var fileToChange = Path.Combine(scenario.TestAppFolder, "include", "Foo.cs");
                File.Delete(fileToChange);

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }
        }

        // Rename a file included in compilation
        [Fact(Skip = "Disabled temporary")]
        public void RenameCompiledFile()
        {
            using (var scenario = new GlobbingAppScenario())

            using (var wait = new WaitForFileToChange(scenario.StatusFile))
            {
                var oldFile = Path.Combine(scenario.TestAppFolder, "include", "Foo.cs");
                var newFile = Path.Combine(scenario.TestAppFolder, "include", "Foo_new.cs");
                File.Move(oldFile, newFile);

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }
        }

        // Add a file that's in a included folder but not matching the globbing pattern
        [Fact(Skip = "Disabled temporary")]
        public void ChangeNonCompiledFile()
        {
            using (var scenario = new GlobbingAppScenario())
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                var ids = File.ReadAllLines(scenario.StatusFile);
                var procId = int.Parse(ids[0]);

                var changedFile = Path.Combine(scenario.TestAppFolder, "include", "not_compiled.css");
                File.WriteAllText(changedFile, "");

                Console.WriteLine($"Waiting {_negativeTestWaitTime} seconds to see if the app restarts");
                Waiters.WaitForProcessToStop(
                    procId,
                    _negativeTestWaitTime,
                    expectedToStop: false,
                    errorMessage: "Test app restarted");
            }
        }

        // Change a file that's in an excluded folder
        [Fact(Skip = "Disabled temporary")]
        public void ChangeExcludedFile()
        {
            using (var scenario = new GlobbingAppScenario())
            {
                // Then wait for it to restart when we change a file that's included in the compilation files
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    var ids = File.ReadAllLines(scenario.StatusFile);
                    var procId = int.Parse(ids[0]);
                  
                    var changedFile = Path.Combine(scenario.TestAppFolder, "exclude", "Baz.cs");
                    File.WriteAllText(changedFile, "");

                    Console.WriteLine($"Waiting {_negativeTestWaitTime} seconds to see if the app restarts");
                    Waiters.WaitForProcessToStop(
                        procId,
                        _negativeTestWaitTime,
                        expectedToStop: false,
                        errorMessage: "Test app restarted");
                }
            }
        }

        private class GlobbingAppScenario : DotNetWatchScenario
        {
            private const string TestAppName = "GlobbingApp";
            private static readonly string _testAppFolder = Path.Combine(_repositoryRoot, "test", "TestApps", TestAppName);

            public GlobbingAppScenario()
            {
                StatusFile = Path.Combine(_scenario.TempFolder, "status");
                StartedFile = StatusFile + ".started";

                _scenario.AddProject(_testAppFolder);
                _scenario.AddToolToProject(TestAppName, DotnetWatch);
                _scenario.Restore();

                TestAppFolder = Path.Combine(_scenario.WorkFolder, TestAppName);

                // Wait for the process to start
                using (var wait = new WaitForFileToChange(StartedFile))
                {
                    RunDotNetWatch(StatusFile, Path.Combine(_scenario.WorkFolder, TestAppName));

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"File not created: {StartedFile}");
                }

                Waiters.WaitForFileToBeReadable(StartedFile, _defaultTimeout);
            }

            public string StatusFile { get; private set; }
            public string StartedFile { get; private set; }
            public string TestAppFolder { get; private set; }
        }
    }
}
