// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Microsoft.AspNetCore.Server.Kestrel;

internal interface ITlsConfigurationLoader
{
    void ApplyHttpsDefaults(
        KestrelServerOptions serverOptions,
        EndpointConfig endpoint,
        HttpsConnectionAdapterOptions httpsOptions,
        CertificateConfig? defaultCertificateConfig,
        ConfigurationReader configurationReader);

    CertificateAndConfig? LoadDefaultCertificate(ConfigurationReader configurationReader);

    void UseHttps(ListenOptions listenOptions, EndpointConfig endpoint, HttpsConnectionAdapterOptions httpsOptions);
}

internal record CertificateAndConfig(X509Certificate2 Certificate, CertificateConfig CertificateConfig);
