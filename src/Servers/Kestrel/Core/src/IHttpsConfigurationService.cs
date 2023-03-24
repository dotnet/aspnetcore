// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// An abstraction over various things that would prevent us from trimming TLS support in `CreateSlimBuilder`
/// scenarios.  In normal usage, it will *always* be registered by only be <see cref="IsInitialized"/> if the
/// consumer explicitly opts into having HTTPS/TLS support.
/// </summary>
internal interface IHttpsConfigurationService
{
    /// <summary>
    /// If this property returns false, then methods other than <see cref="Initialize"/> will throw.
    /// The most obvious way to make this true is to call <see cref="Initialize"/>, but some implementations
    /// may offer alternative mechanisms.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Replaces the implementations off all other methods with functioning (as opposed to throwing) versions.
    /// </summary>
    void Initialize(
        IHostEnvironment hostEnvironment,
        ILogger<KestrelServer> serverLogger,
        ILogger<HttpsConnectionMiddleware> httpsLogger);

    /// <summary>
    /// Applies various configuration settings to <paramref name="httpsOptions"/> and <paramref name="endpoint"/>.
    /// </summary>
    /// <remarks>
    /// For use during configuration loading (esp in <see cref="KestrelConfigurationLoader"/>).
    /// </remarks>
    void ApplyHttpsConfiguration(
        HttpsConnectionAdapterOptions httpsOptions,
        EndpointConfig endpoint,
        KestrelServerOptions serverOptions,
        CertificateConfig? defaultCertificateConfig,
        ConfigurationReader configurationReader);

    /// <summary>
    /// Calls an appropriate overload of <see cref="Microsoft.AspNetCore.Hosting.ListenOptionsHttpsExtensions.UseHttps(ListenOptions)"/>
    /// on <paramref name="listenOptions"/>, with or without SNI, according to how <paramref name="endpoint"/> is configured.
    /// </summary>
    /// <returns>Updated <see cref="ListenOptions"/> for convenient chaining.</returns>
    /// <remarks>
    /// For use during configuration loading (esp in <see cref="KestrelConfigurationLoader"/>).
    /// </remarks>
    ListenOptions UseHttpsWithSni(ListenOptions listenOptions, HttpsConnectionAdapterOptions httpsOptions, EndpointConfig endpoint);

    /// <summary>
    /// Retrieves the default or, failing that, developer certificate from <paramref name="configurationReader"/>.
    /// </summary>
    /// <remarks>
    /// For use during configuration loading (esp in <see cref="KestrelConfigurationLoader"/>).
    /// </remarks>
    CertificateAndConfig? LoadDefaultCertificate(ConfigurationReader configurationReader);

    /// <summary>
    /// Updates <paramref name="features"/> with multiplexed transport (i.e. HTTP/3) features based on
    /// the configuration of <paramref name="listenOptions"/>.
    /// </summary>
    /// <remarks>
    /// For use during endpoint binding (esp in <see cref="Internal.Infrastructure.TransportManager"/>).
    /// </remarks>
    void PopulateMultiplexedTransportFeatures(FeatureCollection features, ListenOptions listenOptions);

    /// <summary>
    /// Calls <see cref="Microsoft.AspNetCore.Hosting.ListenOptionsHttpsExtensions.UseHttps(ListenOptions)"/>
    /// on <paramref name="listenOptions"/>.
    /// </summary>
    /// <returns>Updated <see cref="ListenOptions"/> for convenient chaining.</returns>
    /// <remarks>
    /// For use during address binding (esp in <see cref="AddressBinder"/>).
    /// </remarks>
    ListenOptions UseHttpsWithDefaults(ListenOptions listenOptions);
}

/// <summary>
/// A <see cref="Certificate"/>-<see cref="CertificateConfig"/> pair.
/// </summary>
internal readonly struct CertificateAndConfig
{
    public readonly X509Certificate2 Certificate;
    public readonly CertificateConfig CertificateConfig;

    public CertificateAndConfig(X509Certificate2 certificate, CertificateConfig certificateConfig)
    {
        Certificate = certificate;
        CertificateConfig = certificateConfig;
    }
}
