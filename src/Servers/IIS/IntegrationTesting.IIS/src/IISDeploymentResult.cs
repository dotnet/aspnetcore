// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    public class IISDeploymentResult : DeploymentResult
    {
        public ILogger Logger { get; set; }
        public Process HostProcess { get; }

        public IISDeploymentResult(ILoggerFactory loggerFactory,
            IISDeploymentParameters deploymentParameters,
            string applicationBaseUri,
            string contentRoot,
            CancellationToken hostShutdownToken,
            Process hostProcess)
            : base(loggerFactory,
                  deploymentParameters,
                  applicationBaseUri,
                  contentRoot,
                  hostShutdownToken)
        {
            HostProcess = hostProcess;
            Logger = loggerFactory.CreateLogger(deploymentParameters.SiteName);
            HttpClient = CreateClient(new HttpClientHandler());
        }

        public HttpClient CreateClient(HttpMessageHandler messageHandler)
        {
            return new HttpClient(new LoggingHandler(messageHandler, Logger))
            {
                BaseAddress = base.HttpClient.BaseAddress
            };
        }

        public new HttpClient HttpClient { get; set; }
    }
}
