// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal class FakeTlsConnectionFeature : ITlsConnectionFeature
    {
        public FakeTlsConnectionFeature()
        {
        }

        public X509Certificate2? ClientCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
