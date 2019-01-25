// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class PublishedApplicationPublisher: ApplicationPublisher
    {
        private readonly string _applicationPath;

        public PublishedApplicationPublisher(string applicationPath) : base(applicationPath)
        {
            _applicationPath = applicationPath;
        }

        public override Task<PublishedApplication> Publish(DeploymentParameters deploymentParameters, ILogger logger)
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

            var configuration = this.GetType().GetTypeInfo().Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration;

            var path = Path.Combine(_applicationPath, "bin", configuration, deploymentParameters.TargetFramework, "publish", profileName);
            logger.LogInformation("Using prepublished application from {PublishDir}", path);

            var target = CreateTempDirectory();

            var source = new DirectoryInfo(path);
            CachingApplicationPublisher.CopyFiles(source, target, logger);
            return Task.FromResult(new PublishedApplication(target.FullName, logger));
        }
    }
}
