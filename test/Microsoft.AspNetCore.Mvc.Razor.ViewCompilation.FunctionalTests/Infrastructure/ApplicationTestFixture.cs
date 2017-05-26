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
        private HttpClient _httpClient;

        protected ApplicationTestFixture(string applicationName)
        {
            ApplicationName = applicationName;
        }

        public string ApplicationName { get; }

        public string ApplicationPath => ApplicationPaths.GetTestAppDirectory(ApplicationName);

        public HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    if (DeploymentResult == null)
                    {
                        throw new InvalidOperationException($"{nameof(CreateDeployment)} must be called prior to accessing the {nameof(HttpClient)} property.");
                    }

                    _httpClient = new HttpClient
                    {
                        BaseAddress = new Uri(DeploymentResult.ApplicationBaseUri),
                    };
                }

                return _httpClient;
            }
        }

        public ILogger Logger { get; private set; }

        public ILoggerFactory LoggerFactory { get; private set; }

        public DeploymentResult DeploymentResult { get; private set; }

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
            _httpClient?.Dispose();
            _deployer?.Dispose();
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

        public void CreateDeployment(RuntimeFlavor flavor)
        {
            LoggerFactory = CreateLoggerFactory();
            Logger = LoggerFactory.CreateLogger($"{ApplicationName}:{flavor}");

            var deploymentParameters = GetDeploymentParameters(flavor);
            _deployer = ApplicationDeployerFactory.Create(deploymentParameters, LoggerFactory);
            DeploymentResult = _deployer.DeployAsync().Result;
        }
    }
}
