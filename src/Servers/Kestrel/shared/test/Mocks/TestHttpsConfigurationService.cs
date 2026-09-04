// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestHttpsConfigurationService : IHttpsConfigurationService
{
    public bool IsInitialized { get; set; }

    public void Initialize(
        IHostEnvironment hostEnvironment,
        ILogger<KestrelServer> serverLogger,
        ILogger<HttpsConnectionMiddleware> httpsLogger)
    {
        IsInitialized = true;
    }

    public void ApplyHttpsConfiguration(
        HttpsConnectionAdapterOptions httpsOptions,
        EndpointConfig endpoint,
        KestrelServerOptions serverOptions,
        CertificateConfig defaultCertificateConfig,
        ConfigurationReader configurationReader)
    {
    }

    public ListenOptions UseHttpsWithSni(ListenOptions listenOptions, HttpsConnectionAdapterOptions httpsOptions, EndpointConfig endpoint)
        => listenOptions;

    public CertificateAndConfig? LoadDefaultCertificate(ConfigurationReader configurationReader) => null;

    public void PopulateMultiplexedTransportFeatures(FeatureCollection features, ListenOptions listenOptions)
    {
    }

    public ListenOptions UseHttpsWithDefaults(ListenOptions listenOptions) => listenOptions;
}
