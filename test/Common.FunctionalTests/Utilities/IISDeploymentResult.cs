// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class IISDeploymentResult
    {
        public DeploymentResult DeploymentResult { get; }
        public ILogger Logger { get; }

        public IISDeploymentResult(DeploymentResult deploymentResult, ILogger logger)
        {
            DeploymentResult = deploymentResult;
            Logger = logger;

            RetryingHttpClient = CreateRetryClient(new SocketsHttpHandler());
            HttpClient = CreateClient(new SocketsHttpHandler());
        }

        public HttpClient CreateRetryClient(HttpMessageHandler messageHandler)
        {
            return new HttpClient(new RetryHandler(new LoggingHandler(messageHandler, Logger), Logger))
            {
                BaseAddress = DeploymentResult.HttpClient.BaseAddress
            };
        }

        public HttpClient CreateClient(HttpMessageHandler messageHandler)
        {
            return new HttpClient(new LoggingHandler(messageHandler, Logger))
            {
                BaseAddress = DeploymentResult.HttpClient.BaseAddress
            };
        }

        public HttpClient HttpClient { get; set; }
        public HttpClient RetryingHttpClient { get; set; }
    }
}
