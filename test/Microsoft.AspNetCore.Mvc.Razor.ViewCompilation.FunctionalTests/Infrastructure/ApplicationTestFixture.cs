// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        public const string DotnetCLITelemetryOptOut = "DOTNET_CLI_TELEMETRY_OPTOUT";
        private readonly object _deploymentLock = new object();
        private IApplicationDeployer _deployer;
        private DeploymentResult _deploymentResult;

        protected ApplicationTestFixture(string applicationName)
        {
            ApplicationName = applicationName;
        }

        public string ApplicationName { get; }

        public string ApplicationPath => ApplicationPaths.GetTestAppDirectory(ApplicationName);

        public HttpClient HttpClient { get; } = new HttpClient();

        public ILogger Logger { get; private set; }

        public DeploymentResult CreateDeployment()
        {
            lock (_deploymentLock)
            {
                if (_deployer != null)
                {
                    return _deploymentResult;
                }

                Logger = CreateLogger();
                var deploymentParameters = GetDeploymentParameters();
                var deployer = ApplicationDeployerFactory.Create(deploymentParameters, Logger);
                _deploymentResult = deployer.Deploy();

                _deployer = deployer;

                return _deploymentResult;
            }
        }

        public virtual DeploymentParameters GetDeploymentParameters()
        {
            var telemetryOptOut = new KeyValuePair<string, string>(
                DotnetCLITelemetryOptOut,
                "1");

            var deploymentParameters = new DeploymentParameters(
                ApplicationPath,
                ServerType.Kestrel,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
                PublishApplicationBeforeDeployment = true,
#if NETCOREAPP1_1
                TargetFramework = "netcoreapp1.1",
#else
#error the target framework needs to be updated.
#endif
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

        public virtual ILogger CreateLogger()
        {
            return new LoggerFactory()
                .AddConsole()
                .CreateLogger($"{ApplicationName}");
        }

        public void Dispose()
        {
            HttpClient?.Dispose();
            _deployer?.Dispose();
        }

        protected static void TryDeleteDirectory(string directory)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // Ignore delete failures.
            }
        }
    }
}
