// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.AspNet.Server.Kestrel.Filter;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public class HttpsConnectionFilter : IConnectionFilter
    {
        private readonly X509Certificate2 _serverCert;
        private readonly ClientCertificateMode _clientCertMode;
        private readonly ClientCertificateValidationCallback _clientValidationCallback;
        private readonly IConnectionFilter _previous;
        private X509Certificate2 _clientCert;

        public HttpsConnectionFilter(HttpsConnectionFilterOptions options, IConnectionFilter previous)
        {
            if (options.ServerCertificate == null)
            {
                throw new ArgumentNullException(nameof(options.ServerCertificate));
            }
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            _serverCert = options.ServerCertificate;
            _clientCertMode = options.ClientCertificateMode;
            _clientValidationCallback = options.ClientCertificateValidation;
            _previous = previous;
        }

        public async Task OnConnection(ConnectionFilterContext context)
        {
            await _previous.OnConnection(context);

            if (string.Equals(context.Address.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                SslStream sslStream;
                if (_clientCertMode == ClientCertificateMode.NoCertificate)
                {
                    sslStream = new SslStream(context.Connection);
                    await sslStream.AuthenticateAsServerAsync(_serverCert);
                }
                else
                {
                    sslStream = new SslStream(context.Connection, leaveInnerStreamOpen: false,
                        userCertificateValidationCallback: (sender, certificate, chain, sslPolicyErrors) =>
                        {
                            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
                            {
                                return _clientCertMode != ClientCertificateMode.RequireCertificate;
                            }


                            if (_clientValidationCallback == null)
                            {
                                if (sslPolicyErrors != SslPolicyErrors.None)
                                {
                                    return false;
                                }
                            }
#if DOTNET5_4
                            // conversion X509Certificate to X509Certificate2 not supported
                            // https://github.com/dotnet/corefx/issues/4510
                            X509Certificate2 certificate2 = null;
                            return false;
#else
                            X509Certificate2 certificate2 = certificate as X509Certificate2 ??
                                                            new X509Certificate2(certificate);

#endif
                            if (_clientValidationCallback != null)
                            {
                                if (!_clientValidationCallback(certificate2, chain, sslPolicyErrors))
                                {
                                    return false;
                                }
                            }

                            _clientCert = certificate2;
                            return true;
                        });
                    await sslStream.AuthenticateAsServerAsync(_serverCert, clientCertificateRequired: true,
                        enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        checkCertificateRevocation: false);
                }
                context.Connection = sslStream;
            }
        }

        public void PrepareRequest(IFeatureCollection features)
        {
            _previous.PrepareRequest(features);

            if (_clientCert != null)
            {
                features.Set<ITlsConnectionFeature>(
                    new TlsConnectionFeature { ClientCertificate = _clientCert });
            }
        }
    }
}
