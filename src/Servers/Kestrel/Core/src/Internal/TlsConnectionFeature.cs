// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal
{
    internal class TlsConnectionFeature : ITlsConnectionFeature, ITlsApplicationProtocolFeature
    {
        public X509Certificate2 ClientCertificate { get; set; }

        public ReadOnlyMemory<byte> ApplicationProtocol { get; set; }

        public Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ClientCertificate);
        }
    }
}
