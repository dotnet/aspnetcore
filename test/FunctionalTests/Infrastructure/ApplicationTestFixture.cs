// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;

namespace FunctionalTests
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        private const string DotnetCLITelemetryOptOut = "DOTNET_CLI_TELEMETRY_OPTOUT";
        private readonly object _deploymentLock = new object();
        private Task<DeploymentResult> _deploymentTask;
        private IApplicationDeployer _deployer;

        protected ApplicationTestFixture(string applicationName, string applicationPath)
        {
            ApplicationName = applicationName;
            ApplicationPath = applicationPath ?? ApplicationPaths.GetTestAppDirectory(applicationName);
        }

        public string ApplicationName { get; }

        public string ApplicationPath { get; }

        protected abstract DeploymentParameters GetDeploymentParameters();

        protected DeploymentParameters GetDeploymentParameters(RuntimeFlavor flavor)
        {
            var telemetryOptOut = new KeyValuePair<string, string>(
                DotnetCLITelemetryOptOut,
                "1");

            var deploymentParameters = new DeploymentParameters(
                ApplicationPath,
                ServerType.Kestrel,
                flavor,
                RuntimeArchitecture.x64)
            {
                ApplicationName = ApplicationName,
                PublishApplicationBeforeDeployment = true,
                TargetFramework = flavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.0",
#if DEBUG
                Configuration = "Debug",
#else
                Configuration = "Release",
#endif
                EnvironmentVariables =
                {
                    telemetryOptOut,
                },
                PublishEnvironmentVariables =
                {
                    telemetryOptOut,
                },
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
            lock (_deploymentLock)
            {
                if (_deploymentTask == null)
                {
                    var deploymentParameters = GetDeploymentParameters();
                    _deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory);
                    _deploymentTask = _deployer.DeployAsync();
                }
            }

            return await _deploymentTask;
        }
    }
}
