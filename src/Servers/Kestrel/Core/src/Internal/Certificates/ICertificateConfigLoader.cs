// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Certificates;

internal interface ICertificateConfigLoader
{
    bool IsTestMock { get; }

    (X509Certificate2?, X509Certificate2Collection?) LoadCertificate(CertificateConfig? certInfo, string endpointName);
}
