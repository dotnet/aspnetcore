// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class GlobbingAppTests
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan _negativeTestWaitTime = TimeSpan.FromSeconds(10);

        private readonly ITestOutputHelper _logger;

        public GlobbingAppTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        [Fact]
        public void ChangeCompiledFile_PollingWatcher()
        {
            ChangeCompiledFile(usePollingWatcher: true);
        }

        [Fact]
        public void ChangeCompiledFile_DotNetWatcher()
        {
            ChangeCompiledFile(usePollingWatcher: false);
        }

        // Change a file included in compilation
        private void ChangeCompiledFile(bool usePollingWatcher)
        {
            using (var scenario = new GlobbingAppScenario(_logger))
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                scenario.UsePollingWatcher = usePollingWatcher;

                scenario.Start();

                var fileToChange = Path.Combine(scenario.TestAppFolder, "include", "Foo.cs");
                var programCs = File.ReadAllText(fileToChange);
                File.WriteAllText(fileToChange, programCs);

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }

        }

        // Add a file to a folder included in compilation
        [Fact]
        public void AddCompiledFile()
        {
            // Add a file in a folder that's included in compilation
            using (var scenario = new GlobbingAppScenario(_logger))
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                scenario.Start();

                var fileToChange = Path.Combine(scenario.TestAppFolder, "include", "Bar.cs");
                File.WriteAllText(fileToChange, "");

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }
        }

        // Delete a file included in compilation
        [Fact]
        public void DeleteCompiledFile()
        {
            using (var scenario = new GlobbingAppScenario(_logger))
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                scenario.Start();

                var fileToChange = Path.Combine(scenario.TestAppFolder, "include", "Foo.cs");
                File.Delete(fileToChange);

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }
        }

        // Delete an entire folder
        [Fact]
        public void DeleteSourceFolder()
        {
            using (var scenario = new GlobbingAppScenario(_logger))
            using (var wait = new WaitForFileToChange(scenario.StartedFile))
            {
                scenario.Start();

                var folderToDelete = Path.Combine(scenario.TestAppFolder, "include");
                Directory.Delete(folderToDelete, recursive: true);

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }
        }

        // Rename a file included in compilation
        [Fact]
        public void RenameCompiledFile()
        {
            using (var scenario = new GlobbingAppScenario(_logger))
            using (var wait = new WaitForFileToChange(scenario.StatusFile))
            {
                scenario.Start();

                var oldFile = Path.Combine(scenario.TestAppFolder, "include", "Foo.cs");
                var newFile = Path.Combine(scenario.TestAppFolder, "include", "Foo_new.cs");
                File.Move(oldFile, newFile);

                wait.Wait(_defaultTimeout,
                    expectedToChange: true,
                    errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
            }
        }

        [Fact]
        public void ChangeNonCompiledFile_PollingWatcher()
        {
            ChangeNonCompiledFile(usePollingWatcher: true);
        }

        [Fact]
        public void ChangeNonCompiledFile_DotNetWatcher()
        {
            ChangeNonCompiledFile(usePollingWatcher: false);
        }

        // Add a file that's in a included folder but not matching the globbing pattern
        private void ChangeNonCompiledFile(bool usePollingWatcher)
        {
            using (var scenario = new GlobbingAppScenario(_logger))
            {
                scenario.UsePollingWatcher = usePollingWatcher;

                scenario.Start();

                var ids = File.ReadAllLines(scenario.StatusFile);
                var procId = int.Parse(ids[0]);

                var changedFile = Path.Combine(scenario.TestAppFolder, "include", "not_compiled.css");
                File.WriteAllText(changedFile, "");

                Console.WriteLine($"Waiting {_negativeTestWaitTime.TotalSeconds} seconds to see if the app restarts");
                Waiters.WaitForProcessToStop(
                    procId,
                    _negativeTestWaitTime,
                    expectedToStop: false,
                    errorMessage: "Test app restarted");
            }
        }

        // Change a file that's in an excluded folder
        [Fact]
        public void ChangeExcludedFile()
        {
            using (var scenario = new GlobbingAppScenario(_logger))
            {
                scenario.Start();

                var ids = File.ReadAllLines(scenario.StatusFile);
                var procId = int.Parse(ids[0]);

                var changedFile = Path.Combine(scenario.TestAppFolder, "exclude", "Baz.cs");
                File.WriteAllText(changedFile, "");

                Console.WriteLine($"Waiting {_negativeTestWaitTime.TotalSeconds} seconds to see if the app restarts");
                Waiters.WaitForProcessToStop(
                    procId,
                    _negativeTestWaitTime,
                    expectedToStop: false,
                    errorMessage: "Test app restarted");
            }
        }

        private class GlobbingAppScenario : DotNetWatchScenario
        {
            private const string TestAppName = "GlobbingApp";

            public GlobbingAppScenario(ITestOutputHelper logger)
                : base(logger)
            {
                StatusFile = Path.Combine(Scenario.TempFolder, "status");
                StartedFile = StatusFile + ".started";

                Scenario.AddTestProjectFolder(TestAppName);
                Scenario.Restore3(TestAppName);

                TestAppFolder = Path.Combine(Scenario.WorkFolder, TestAppName);
            }

            public void Start()
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(StartedFile))
                {
                    RunDotNetWatch(new[] { "run3", StatusFile }, Path.Combine(Scenario.WorkFolder, TestAppName));

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
