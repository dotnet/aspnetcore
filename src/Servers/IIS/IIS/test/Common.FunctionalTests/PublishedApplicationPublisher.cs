// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class IISPublishedApplicationPublisher: PublishedApplicationPublisher
    {
        private readonly string _applicationName;

        public IISPublishedApplicationPublisher(string applicationName) : base(applicationName)
        {
            _applicationName = applicationName;
        }

        protected override string ResolvePublishedApplicationPath(DeploymentParameters deploymentParameters, ILogger logger)
        {
            var path = Path.Combine(AppContext.BaseDirectory, GetProfileName(deploymentParameters));

            if (!Directory.Exists(path))
            {
                var solutionPath = GetProjectReferencePublishLocation(deploymentParameters);
                logger.LogInformation("{PublishDir} doesn't exist falling back to solution based path {SolutionBasedDir}", solutionPath, solutionPath);
                path = solutionPath;
            }

            return path;
        }

        private string GetProjectReferencePublishLocation(DeploymentParameters deploymentParameters)
        {
            var testAssetsBasePath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"), "IIS", "test", "testassets", _applicationName);
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
