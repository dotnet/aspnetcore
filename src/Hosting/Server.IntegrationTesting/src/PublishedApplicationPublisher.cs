// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class PublishedApplicationPublisher : ApplicationPublisher
    {
        public PublishedApplicationPublisher(string applicationName)
            : base(applicationName)
        {
        }

        public override Task<PublishedApplication> Publish(DeploymentParameters deploymentParameters, ILogger logger)
        {
            var path = ResolvePublishedApplicationPath(deploymentParameters, logger);

            logger.LogInformation("Using prepublished application from {PublishDir}", path);

            var target = CreateTempDirectory();

            var source = new DirectoryInfo(path);
            CachingApplicationPublisher.CopyFiles(source, target, logger);
            return Task.FromResult(new PublishedApplication(target.FullName, logger));
        }

        protected virtual string ResolvePublishedApplicationPath(DeploymentParameters deploymentParameters, ILogger logger)
        {
            var path = deploymentParameters.PublishedApplicationRootPath;
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"{nameof(DeploymentParameters)}.{nameof(DeploymentParameters.PublishedApplicationRootPath)} not set.");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Published application not found at '{path}'.");
            }

            return deploymentParameters.PublishedApplicationRootPath;
        }
    }
}
