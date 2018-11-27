// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;

namespace FunctionalTests
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        private const string DotnetCLITelemetryOptOut = "DOTNET_CLI_TELEMETRY_OPTOUT";
        private static readonly SemaphoreSlim _deploymentLock = new SemaphoreSlim(initialCount: 1);
        private Task<DeploymentResult> _deploymentTask;
        private IApplicationDeployer _deployer;

        protected ApplicationTestFixture(string applicationName, string applicationPath)
        {
            ApplicationName = applicationName;
            ApplicationPath = applicationPath ?? ApplicationPaths.GetTestAppDirectory(applicationName);
        }

        public string ApplicationName { get; }

        public string ApplicationPath { get; }

        public bool PublishOnly { get; set; }

        protected abstract DeploymentParameters GetDeploymentParameters();

        protected DeploymentParameters GetDeploymentParameters(RuntimeFlavor flavor, string targetFramework)
            => GetDeploymentParameters(ApplicationPath, ApplicationName, flavor, targetFramework);

        public static DeploymentParameters GetDeploymentParameters(string applicationPath, string applicationName, RuntimeFlavor flavor, string targetFramework)
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
                PublishApplicationBeforeDeployment = true,
                Configuration = "Release",
                EnvironmentVariables =
                {
                    new KeyValuePair<string, string>(DotnetCLITelemetryOptOut, "1"),
                    new KeyValuePair<string, string>("SolutionConfiguration", projectConfiguration),
                },
                PublishEnvironmentVariables =
                {
                    new KeyValuePair<string, string>(DotnetCLITelemetryOptOut, "1"),
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

            _deployer?.Dispose();
        }

        public async Task<DeploymentResult> CreateDeploymentAsync(ILoggerFactory loggerFactory)
        {
            try
            {
                await _deploymentLock.WaitAsync(TimeSpan.FromSeconds(10));
                if (_deploymentTask == null)
                {
                    var deploymentParameters = GetDeploymentParameters();
                    if (PublishOnly)
                    {
                        _deployer = new PublishOnlyDeployer(deploymentParameters, loggerFactory);
                    }
                    else
                    {
                        _deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory);
                    }

                    _deploymentTask = _deployer.DeployAsync();
                }
            }
            finally
            {
                _deploymentLock.Release();
            }

            return await _deploymentTask;
        }
    }
}
