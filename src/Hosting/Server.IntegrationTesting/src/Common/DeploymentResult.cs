// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

/// <summary>
/// Result of a deployment.
/// </summary>
public class DeploymentResult
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Base Uri of the deployment application.
    /// </summary>
    public string ApplicationBaseUri { get; }

    /// <summary>
    /// The folder where the application is hosted. This path can be different from the
    /// original application source location if published before deployment.
    /// </summary>
    public string ContentRoot { get; }

    /// <summary>
    /// Original deployment parameters used for this deployment.
    /// </summary>
    public DeploymentParameters DeploymentParameters { get; }

    /// <summary>
    /// Triggered when the host process dies or pulled down.
    /// </summary>
    public CancellationToken HostShutdownToken { get; }

    /// <summary>
    /// An <see cref="HttpClient"/> with <see cref="LoggingHandler"/> configured and the <see cref="HttpClient.BaseAddress"/> set to the <see cref="ApplicationBaseUri"/>
    /// </summary>
    public HttpClient HttpClient { get; }

    public DeploymentResult(ILoggerFactory loggerFactory, DeploymentParameters deploymentParameters, string applicationBaseUri)
        : this(loggerFactory, deploymentParameters: deploymentParameters, applicationBaseUri: applicationBaseUri, contentRoot: string.Empty, hostShutdownToken: CancellationToken.None)
    { }

    public DeploymentResult(ILoggerFactory loggerFactory, DeploymentParameters deploymentParameters, string applicationBaseUri, string contentRoot, CancellationToken hostShutdownToken)
    {
        _loggerFactory = loggerFactory;

        ApplicationBaseUri = applicationBaseUri;
        ContentRoot = contentRoot;
        DeploymentParameters = deploymentParameters;
        HostShutdownToken = hostShutdownToken;

        HttpClient = CreateHttpClient(new HttpClientHandler());
    }

    /// <summary>
    /// Create an <see cref="HttpClient"/> with <see cref="LoggingHandler"/> configured and the <see cref="HttpClient.BaseAddress"/> set to the <see cref="ApplicationBaseUri"/>,
    /// but using the provided <see cref="HttpMessageHandler"/> and the underlying handler.
    /// </summary>
    /// <param name="baseHandler"></param>
    /// <returns></returns>
    public HttpClient CreateHttpClient(HttpMessageHandler baseHandler) =>
        new HttpClient(new LoggingHandler(_loggerFactory, baseHandler))
        {
            BaseAddress = new Uri(ApplicationBaseUri),
            Timeout = TimeSpan.FromSeconds(200),
        };
}
