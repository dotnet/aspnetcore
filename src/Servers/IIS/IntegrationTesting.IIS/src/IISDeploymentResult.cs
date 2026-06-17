// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS;

public class IISDeploymentResult : DeploymentResult, IDisposable
{
    public ILogger Logger { get; set; }

    public Process HostProcess { get; }

    public string AppPoolName { get; }

    public IISDeploymentResult(ILoggerFactory loggerFactory,
        IISDeploymentParameters deploymentParameters,
        string applicationBaseUri,
        string contentRoot,
        string appPoolName,
        CancellationToken hostShutdownToken,
        Process hostProcess)
        : base(loggerFactory,
              deploymentParameters,
              applicationBaseUri,
              contentRoot,
              hostShutdownToken)
    {
        AppPoolName = appPoolName;
        HostProcess = hostProcess;
        Logger = loggerFactory.CreateLogger(deploymentParameters.SiteName);
        HttpClient = CreateClient(new HttpClientHandler());
    }

    public HttpClient CreateClient(HttpMessageHandler messageHandler)
    {
        return new HttpClient(new LoggingHandler(messageHandler, Logger))
        {
            BaseAddress = base.HttpClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(200),
        };
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }

    public new HttpClient HttpClient { get; set; }
}
