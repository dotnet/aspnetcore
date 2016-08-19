// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        public const string NuGetPackagesEnvironmentKey = "NUGET_PACKAGES";
        private readonly string _oldRestoreDirectory;
        private bool _isRestored;

        protected ApplicationTestFixture(string applicationName)
        {
            ApplicationName = applicationName;
            _oldRestoreDirectory = Environment.GetEnvironmentVariable(NuGetPackagesEnvironmentKey);
        }

        public string ApplicationName { get; }

        public string ApplicationPath => ApplicationPaths.GetTestAppDirectory(ApplicationName);

        public string TempRestoreDirectory { get; } = CreateTempRestoreDirectory();

        public IApplicationDeployer CreateDeployment(RuntimeFlavor flavor)
        {
            if (!_isRestored)
            {
                Restore();
                _isRestored = true;
            }

            var tempRestoreDirectoryEnvironment = new KeyValuePair<string, string>(
                NuGetPackagesEnvironmentKey,
                TempRestoreDirectory);

            var deploymentParameters = new DeploymentParameters(
                ApplicationPath,
                ServerType.Kestrel,
                flavor,
                RuntimeArchitecture.x64)
            {
                PublishApplicationBeforeDeployment = true,
                TargetFramework = flavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                Configuration = "Release",
                EnvironmentVariables =
                {
                    tempRestoreDirectoryEnvironment
                },
                PublishEnvironmentVariables =
                {
                    tempRestoreDirectoryEnvironment
                },
            };

            var logger = new LoggerFactory()
                .AddConsole()
                .CreateLogger($"{ApplicationName}:{flavor}");

            return ApplicationDeployerFactory.Create(deploymentParameters, logger);
        }

        protected virtual void Restore()
        {
            RestoreProject(ApplicationPath);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(TempRestoreDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Ignore delete failures.
            }
        }

        protected void RestoreProject(string applicationDirectory)
        {
            var packagesDirectory = GetNuGetPackagesDirectory();
            var args = new[]
            {
                Path.Combine(applicationDirectory, "project.json"),
                "--packages",
                TempRestoreDirectory,
            };

            var commandResult = Command
                .CreateDotNet("restore", args)
                .ForwardStdErr(Console.Error)
                .ForwardStdOut(Console.Out)
                .Execute();

            Assert.True(commandResult.ExitCode == 0,
                string.Join(Environment.NewLine,
                    $"dotnet {commandResult.StartInfo.Arguments} exited with {commandResult.ExitCode}.",
                    commandResult.StdOut,
                    commandResult.StdErr));

            Console.WriteLine(commandResult.StdOut);
        }

        private static string CreateTempRestoreDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return Directory.CreateDirectory(path).FullName;
        }

        private static string GetNuGetPackagesDirectory()
        {
            var nugetFeed = Environment.GetEnvironmentVariable(NuGetPackagesEnvironmentKey);
            if (!string.IsNullOrEmpty(nugetFeed))
            {
                return nugetFeed;
            }

            string basePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = Environment.GetEnvironmentVariable("HOME");
            }

            return Path.Combine(basePath, ".nuget", "packages");
        }
    }
}
