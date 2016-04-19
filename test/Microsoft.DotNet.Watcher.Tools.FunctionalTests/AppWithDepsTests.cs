// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Watcher.FunctionalTests
{
    public class AppWithDepsTests
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        // Change a file included in compilation
        [Fact]
        public void ChangeFileInDependency()
        {
            using (var scenario = new AppWithDepsScenario())
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

            private static readonly string _appWithDepsFolder = Path.Combine(_repositoryRoot, "test", "TestApps", AppWithDeps);
            private static readonly string _dependencyFolder = Path.Combine(_repositoryRoot, "test", "TestApps", Dependency);

            public AppWithDepsScenario()
            {
                StatusFile = Path.Combine(_scenario.TempFolder, "status");
                StartedFile = StatusFile + ".started";
                
                _scenario.AddProject(_appWithDepsFolder);
                _scenario.AddProject(_dependencyFolder);

                _scenario.AddToolToProject(AppWithDeps, DotnetWatch);
                _scenario.Restore();

                AppWithDepsFolder = Path.Combine(_scenario.WorkFolder, AppWithDeps);
                DependencyFolder = Path.Combine(_scenario.WorkFolder, Dependency);
            }

            public void Start()
            {
                // Wait for the process to start
                using (var wait = new WaitForFileToChange(StatusFile))
                {
                    RunDotNetWatch(StatusFile, Path.Combine(_scenario.WorkFolder, AppWithDeps));

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
