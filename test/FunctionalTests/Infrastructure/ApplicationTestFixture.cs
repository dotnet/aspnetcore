// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;

namespace FunctionalTests
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        public const string DotnetCLITelemetryOptOut = "DOTNET_CLI_TELEMETRY_OPTOUT";

        protected ApplicationTestFixture(string applicationName)
        {
            ApplicationName = applicationName;
            LoggerFactory = CreateLoggerFactory();
            Logger = LoggerFactory.CreateLogger($"{ApplicationName}");
        }

        public string ApplicationName { get; }

        public string ApplicationPath => ApplicationPaths.GetTestAppDirectory(ApplicationName);

        public ILogger Logger { get; private set; }

        public ILoggerFactory LoggerFactory { get; private set; }

        public virtual DeploymentParameters GetDeploymentParameters(RuntimeFlavor flavor)
        {
            return GetDeploymentParameters(ApplicationPath, flavor);
        }

        public static DeploymentParameters GetDeploymentParameters(string applicationPath, RuntimeFlavor flavor)
        {
            var telemetryOptOut = new KeyValuePair<string, string>(
                DotnetCLITelemetryOptOut,
                "1");

            var deploymentParameters = new DeploymentParameters(
                applicationPath,
                ServerType.Kestrel,
                flavor,
                RuntimeArchitecture.x64)
            {
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

        public virtual ILoggerFactory CreateLoggerFactory()
        {
            return new LoggerFactory().AddConsole();
        }

        public void Dispose()
        {
        }

        private static void TryDeleteDirectory(string directory)
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

        public async Task<Deployment> CreateDeploymentAsync(RuntimeFlavor flavor)
        {
            var deploymentParameters = GetDeploymentParameters(flavor);
            var deployer = ApplicationDeployerFactory.Create(deploymentParameters, LoggerFactory);
            var deploymentResult = await deployer.DeployAsync();

            return new Deployment(deployer, deploymentResult);
        }

        public class Deployment : IDisposable
        {
            public Deployment(IApplicationDeployer deployer, DeploymentResult deploymentResult)
            {
                Deployer = deployer;
                DeploymentResult = deploymentResult;
                HttpClient = deploymentResult.HttpClient;
            }

            public IApplicationDeployer Deployer { get; }

            public HttpClient HttpClient { get; }

            public DeploymentResult DeploymentResult { get; }

            public void Dispose()
            {
                Deployer.Dispose();
                HttpClient.Dispose();
            }
        }
    }
}
