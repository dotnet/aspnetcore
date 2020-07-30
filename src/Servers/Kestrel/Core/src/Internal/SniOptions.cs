// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Security;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class SniOptions
    {
        public SslServerAuthenticationOptions SslOptions { get; set; }
        public HttpProtocols HttpProtocols { get; set; }

        // TODO: Reflection based test to ensure we clone everything!
        public SniOptions Clone()
        {
            return new SniOptions
            {
                SslOptions = new SslServerAuthenticationOptions
                {
                    AllowRenegotiation = SslOptions.AllowRenegotiation,
                    ApplicationProtocols = SslOptions.ApplicationProtocols,
                    CertificateRevocationCheckMode = SslOptions.CertificateRevocationCheckMode,
                    CipherSuitesPolicy = SslOptions.CipherSuitesPolicy,
                    ClientCertificateRequired = SslOptions.ClientCertificateRequired,
                    EnabledSslProtocols = SslOptions.EnabledSslProtocols,
                    EncryptionPolicy = SslOptions.EncryptionPolicy,
                    RemoteCertificateValidationCallback = SslOptions.RemoteCertificateValidationCallback,
                    ServerCertificate = SslOptions.ServerCertificate,
                    ServerCertificateContext = SslOptions.ServerCertificateContext,
                    ServerCertificateSelectionCallback = SslOptions.ServerCertificateSelectionCallback,
                },
                HttpProtocols = HttpProtocols,
            };
        }
    }
}
