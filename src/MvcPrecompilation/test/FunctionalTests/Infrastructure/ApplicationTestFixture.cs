// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace FunctionalTests
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        private const string DotnetCLITelemetryOptOut = "DOTNET_CLI_TELEMETRY_OPTOUT";
        private static readonly string SolutionDirectory;

        private Task<DeploymentResult> _deploymentTask;
        private ApplicationDeployer _deployer;

        static ApplicationTestFixture()
        {
            SolutionDirectory = TestPathUtilities.GetSolutionRootDirectory("RazorViewCompilation");
            if (!SolutionDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                SolutionDirectory += Path.DirectorySeparatorChar;
            }
        }

        protected ApplicationTestFixture(string applicationName, string applicationPath)
        {
            ApplicationName = applicationName;
            ApplicationPath = applicationPath ?? ApplicationPaths.GetTestAppDirectory(applicationName);
            WorkingDirectory = Path.Combine(Path.GetTempPath(), "PrecompilationTool", Path.GetRandomFileName());
            TestProjectDirectory = Path.Combine(WorkingDirectory, ApplicationName);
        }

        public string ApplicationName { get; }

        public string ApplicationPath { get; }

        public string WorkingDirectory { get; }

        public string TestProjectDirectory { get; }

        public bool PublishOnly { get; set; }

        protected abstract DeploymentParameters GetDeploymentParameters();

        protected DeploymentParameters GetDeploymentParameters(RuntimeFlavor flavor, string targetFramework)
            => GetDeploymentParameters(TestProjectDirectory, ApplicationName, flavor, targetFramework);

        private static DeploymentParameters GetDeploymentParameters(string applicationPath, string applicationName, RuntimeFlavor flavor, string targetFramework)
        {
            // This determines the configuration of the the test project and consequently the configuration the src projects are most likely built in.
            var projectConfiguration =
#if DEBUG
                "Debug";
#elif RELEASE
                "Release";
#else
#error Unknown configuration
#endif

            var deploymentParameters = new DeploymentParameters(
                applicationPath,
                ServerType.Kestrel,
                flavor,
                RuntimeArchitecture.x64)
            {
                ApplicationName = applicationName,
                ApplicationType = flavor == RuntimeFlavor.Clr ? ApplicationType.Standalone : ApplicationType.Portable,
                PublishApplicationBeforeDeployment = true,
                Configuration = projectConfiguration,
                EnvironmentVariables =
                {
                    new KeyValuePair<string, string>(DotnetCLITelemetryOptOut, "1"),
                    new KeyValuePair<string, string>("SolutionDirectory", SolutionDirectory),
                    new KeyValuePair<string, string>("SolutionConfiguration", projectConfiguration),
                },
                PublishEnvironmentVariables =
                {
                    new KeyValuePair<string, string>(DotnetCLITelemetryOptOut, "1"),
                    new KeyValuePair<string, string>("SolutionDirectory", SolutionDirectory),
                    new KeyValuePair<string, string>("SolutionConfiguration", projectConfiguration),
                },
                TargetFramework = targetFramework,
            };

            return deploymentParameters;
        }

        public void Dispose()
        {
            if (_deploymentTask?.Status == TaskStatus.RanToCompletion)
            {
                _deploymentTask.Result.HttpClient?.Dispose();
            }

            CleanupWorkingDirectory();

            _deployer?.Dispose();
        }

        public Task<DeploymentResult> CreateDeploymentAsync(ILoggerFactory loggerFactory)
        {
            if (_deploymentTask == null)
            {
                _deploymentTask = CreateDeploymentAsyncCore(loggerFactory);
            }

            return _deploymentTask;
        }

        protected virtual Task<DeploymentResult> CreateDeploymentAsyncCore(ILoggerFactory loggerFactory)
        {
            CopyDirectory(new DirectoryInfo(ApplicationPath), new DirectoryInfo(TestProjectDirectory));

            File.Copy(Path.Combine(SolutionDirectory, "global.json"), Path.Combine(TestProjectDirectory, "global.json"));
            File.Copy(Path.Combine(ApplicationPath, "..", "Directory.Build.props"), Path.Combine(TestProjectDirectory, "Directory.Build.props"));
            File.Copy(Path.Combine(ApplicationPath, "..", "Directory.Build.targets"), Path.Combine(TestProjectDirectory, "Directory.Build.targets"));

            var deploymentParameters = GetDeploymentParameters();
            if (PublishOnly)
            {
                _deployer = new PublishOnlyDeployer(deploymentParameters, loggerFactory);
            }
            else
            {
                _deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory);
            }

            return _deployer.DeployAsync();
        }

        public void CopyDirectory(DirectoryInfo source, DirectoryInfo destination, bool recursive = true)
        {
            // Recurse into subdirectories
            foreach (var directory in source.EnumerateDirectories())
            {
                if (directory.Name == "bin")
                {
                    continue;
                }

                var created = destination.CreateSubdirectory(directory.Name);

                // We only want to copy the restore artifacts from obj directory while ignoring in any configuration specific directories
                CopyDirectory(directory, created, recursive: directory.Name != "obj");
            }

            foreach (var file in source.EnumerateFiles())
            {
                file.CopyTo(Path.Combine(destination.FullName, file.Name));
            }
        }

        private void CleanupWorkingDirectory()
        {
            var tries = 5;
            var sleep = TimeSpan.FromSeconds(3);

            for (var i = 0; i < tries; i++)
            {
                try
                {
                    if (Directory.Exists(WorkingDirectory))
                    {
                        Directory.Delete(WorkingDirectory, recursive: true);
                    }
                    return;
                }
                catch when (i < tries - 1)
                {
                    Console.WriteLine($"Failed to delete directory {TestProjectDirectory}, trying again.");
                    Thread.Sleep(sleep);
                }
                catch
                {
                    // Do nothing
                }
            }
        }
    }
}
