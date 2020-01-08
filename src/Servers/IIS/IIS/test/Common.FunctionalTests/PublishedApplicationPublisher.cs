// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class PublishedApplicationPublisher: ApplicationPublisher
    {
        private readonly string _applicationName;

        public PublishedApplicationPublisher(string applicationName) : base(applicationName)
        {
            _applicationName = applicationName;
        }

        public override Task<PublishedApplication> Publish(DeploymentParameters deploymentParameters, ILogger logger)
        {
            var path = Path.Combine(AppContext.BaseDirectory, GetProfileName(deploymentParameters));

            if (!Directory.Exists(path))
            {
                var solutionPath = GetProjectReferencePublishLocation(deploymentParameters);
                logger.LogInformation("{PublishDir} doesn't exist falling back to solution based path {SolutionBasedDir}", solutionPath, solutionPath);
                path = solutionPath;
            }

            logger.LogInformation("Using prepublished application from {PublishDir}", path);

            var target = CreateTempDirectory();

            var source = new DirectoryInfo(path);
            CachingApplicationPublisher.CopyFiles(source, target, logger);
            return Task.FromResult(new PublishedApplication(target.FullName, logger));
        }

        private string GetProjectReferencePublishLocation(DeploymentParameters deploymentParameters)
        {
// Deployers do not work in distributed environments
// see https://github.com/dotnet/aspnetcore/issues/10268 and https://github.com/dotnet/extensions/issues/1697
#pragma warning disable 0618
            var testAssetsBasePath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"), "IIS", "test", "testassets", _applicationName);
#pragma warning restore 0618
            var configuration = this.GetType().GetTypeInfo().Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration;
            var path = Path.Combine(testAssetsBasePath, "bin", configuration, deploymentParameters.TargetFramework, "publish", GetProfileName(deploymentParameters));
            return path;
        }

        private string GetProfileName(DeploymentParameters deploymentParameters)
        {
            // Treat AdditionalPublishParameters as profile name if defined
            string profileName;
            if (!string.IsNullOrEmpty(deploymentParameters.AdditionalPublishParameters))
            {
                profileName = deploymentParameters.AdditionalPublishParameters;
            }
            else if (deploymentParameters.ApplicationType == ApplicationType.Portable)
            {
                profileName = "Portable";
            }
            else
            {
                profileName = "Standalone-" + deploymentParameters.RuntimeArchitecture;
            }

            return Path.GetFileNameWithoutExtension(_applicationName) + "-" + profileName;
        }
    }
}
