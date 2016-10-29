// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class AppWithDepsTests
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        private readonly ITestOutputHelper _logger;

        public AppWithDepsTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        // Change a file included in compilation
        [Fact]
        public void ChangeFileInDependency()
        {
            using (var scenario = new AppWithDepsScenario(_logger))
            {
                scenario.Start();
                using (var wait = new WaitForFileToChange(scenario.StartedFile))
                {
                    var fileToChange = Path.Combine(scenario.DependencyFolder, "Foo.cs");
                    var programCs = File.ReadAllText(fileToChange);
                    File.WriteAllText(fileToChange, programCs);

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"Process did not restart because {scenario.StartedFile} was not changed");
                }
            }
        }

        private class AppWithDepsScenario : DotNetWatchScenario
        {
            private const string AppWithDeps = "AppWithDeps";
            private const string Dependency = "Dependency";

            public AppWithDepsScenario(ITestOutputHelper logger)
                : base(logger)
            {
                StatusFile = Path.Combine(Scenario.TempFolder, "status");
                StartedFile = StatusFile + ".started";

                Scenario.AddTestProjectFolder(AppWithDeps);
                Scenario.AddTestProjectFolder(Dependency);

                Scenario.Restore3(AppWithDeps); // restore3 should be transitive

                AppWithDepsFolder = Path.Combine(Scenario.WorkFolder, AppWithDeps);
                DependencyFolder = Path.Combine(Scenario.WorkFolder, Dependency);
            }

            public void Start()
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(StatusFile))
                {
                    RunDotNetWatch(new[] { "run3", StatusFile }, Path.Combine(Scenario.WorkFolder, AppWithDeps));

                    wait.Wait(_defaultTimeout,
                        expectedToChange: true,
                        errorMessage: $"File not created: {StatusFile}");
                }

                Waiters.WaitForFileToBeReadable(StatusFile, _defaultTimeout);
            }

            public string StatusFile { get; private set; }
            public string StartedFile { get; private set; }
            public string AppWithDepsFolder { get; private set; }
            public string DependencyFolder { get; private set; }
        }
    }
}
