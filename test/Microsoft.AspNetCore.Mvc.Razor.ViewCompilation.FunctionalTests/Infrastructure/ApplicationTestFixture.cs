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
        private IApplicationDeployer _deployer;

        protected ApplicationTestFixture(string applicationName)
        {
            ApplicationName = applicationName;
            DeploymentResult = CreateDeployment();
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri(DeploymentResult.ApplicationBaseUri),
            };
        }

        public string ApplicationName { get; }

        public string ApplicationPath => ApplicationPaths.GetTestAppDirectory(ApplicationName);

        public HttpClient HttpClient { get; private set; }

        public ILogger Logger { get; private set; }

        public ILoggerFactory LoggerFactory { get; private set; }

        public DeploymentResult DeploymentResult { get; private set; }

        public virtual DeploymentParameters GetDeploymentParameters()
        {
            return GetDeploymentParameters(ApplicationPath);
        }

        public static DeploymentParameters GetDeploymentParameters(string applicationPath)
        {
            var telemetryOptOut = new KeyValuePair<string, string>(
                DotnetCLITelemetryOptOut,
                "1");

            var deploymentParameters = new DeploymentParameters(
                applicationPath,
                ServerType.Kestrel,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
                PublishApplicationBeforeDeployment = true,
                TargetFramework = "netcoreapp2.0",
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

        private DeploymentResult CreateDeployment()
        {
            LoggerFactory = CreateLoggerFactory();
            Logger = LoggerFactory.CreateLogger(ApplicationName);

            var deploymentParameters = GetDeploymentParameters();
            _deployer = ApplicationDeployerFactory.Create(deploymentParameters, LoggerFactory);
            return _deployer.DeployAsync().Result;
        }
    }
}
