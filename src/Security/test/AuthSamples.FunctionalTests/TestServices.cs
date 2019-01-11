// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace AuthSamples.FunctionalTests
{
    public static class TestServices
    {
        public static string WorkingDirectory { get; } = AppContext.BaseDirectory;

        public static void LogResponseOnFailedAssert(this ILogger logger, HttpResponseMessage response, string responseText, Action assert)
        {
            try
            {
                assert();
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                if (!string.IsNullOrEmpty(responseText))
                {
                    logger.LogWarning(responseText);
                }
                throw;
            }
        }

        public static async Task RunSiteTest(
            string siteName,
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            ILoggerFactory loggerFactory,
            Func<HttpClient, ILogger, CancellationToken, Task> validator)
        {
            var logger = loggerFactory.CreateLogger(siteName);
            var targetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" :
                "netcoreapp2.1";

            var deploymentParameters = new DeploymentParameters(GetApplicationDirectory(siteName), serverType, runtimeFlavor, architecture)
            {
                SiteName = "HttpTestSite",
                ServerConfigTemplateContent = serverType == ServerType.Nginx ? File.ReadAllText(Path.Combine(WorkingDirectory, "nginx.conf")) : string.Empty,
                PublishApplicationBeforeDeployment = true,
                TargetFramework = targetFramework,
                ApplicationType = applicationType
            };

            using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
            {
                logger.LogInformation($"Running deployment for {siteName}:{serverType}:{runtimeFlavor}:{architecture}");
                var deploymentResult = await deployer.DeployAsync();
                deploymentResult.HttpClient.Timeout = TimeSpan.FromSeconds(10);

                logger.LogInformation($"Running validation for {siteName}:{serverType}:{runtimeFlavor}:{architecture}");
                await validator(deploymentResult.HttpClient, logger, deploymentResult.HostShutdownToken);
            }
        }

        public static string GetApplicationDirectory(string applicationName)
        {
            var solutionRoot = GetSolutionDirectory();
            return Path.Combine(solutionRoot, "samples", applicationName);
        }

        public static string GetSolutionDirectory()
        {
            var applicationBasePath = AppContext.BaseDirectory;
            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFile = new FileInfo(Path.Combine(directoryInfo.FullName, "AuthSamples.sln"));
                if (solutionFile.Exists)
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution root could not be located using application root {applicationBasePath}.");
        }
    }
}
