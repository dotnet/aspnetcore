// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Filter;

namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    public class HttpsConnectionFilter : IConnectionFilter
    {
        private readonly HttpsConnectionFilterOptions _options;
        private readonly IConnectionFilter _previous;

        public HttpsConnectionFilter(HttpsConnectionFilterOptions options, IConnectionFilter previous)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }
            if (options.ServerCertificate == null)
            {
                throw new ArgumentException("The server certificate parameter is required.");
            }

            _options = options;
            _previous = previous;
        }

        public async Task OnConnectionAsync(ConnectionFilterContext context)
        {
            await _previous.OnConnectionAsync(context);

            if (string.Equals(context.Address.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                X509Certificate2 clientCertificate = null;
                SslStream sslStream;
                if (_options.ClientCertificateMode == ClientCertificateMode.NoCertificate)
                {
                    sslStream = new SslStream(context.Connection);
                    await sslStream.AuthenticateAsServerAsync(_options.ServerCertificate, clientCertificateRequired: false,
                        enabledSslProtocols: _options.SslProtocols, checkCertificateRevocation: _options.CheckCertificateRevocation);
                }
                else
                {
                    sslStream = new SslStream(context.Connection, leaveInnerStreamOpen: false,
                        userCertificateValidationCallback: (sender, certificate, chain, sslPolicyErrors) =>
                        {
                            if (certificate == null)
                            {
                                return _options.ClientCertificateMode != ClientCertificateMode.RequireCertificate;
                            }

                            if (_options.ClientCertificateValidation == null)
                            {
                                if (sslPolicyErrors != SslPolicyErrors.None)
                                {
                                    return false;
                                }
                            }

                            X509Certificate2 certificate2 = certificate as X509Certificate2;
                            if (certificate2 == null)
                            {
#if DOTNET5_4
                                // conversion X509Certificate to X509Certificate2 not supported
                                // https://github.com/dotnet/corefx/issues/4510
                                return false;
#else
                                certificate2 = new X509Certificate2(certificate);
#endif
                            }

                            if (_options.ClientCertificateValidation != null)
                            {
                                if (!_options.ClientCertificateValidation(certificate2, chain, sslPolicyErrors))
                                {
                                    return false;
                                }
                            }

                            clientCertificate = certificate2;
                            return true;
                        });
                    await sslStream.AuthenticateAsServerAsync(_options.ServerCertificate, clientCertificateRequired: true,
                        enabledSslProtocols: _options.SslProtocols, checkCertificateRevocation: _options.CheckCertificateRevocation);
                }

                var previousPrepareRequest = context.PrepareRequest;
                context.PrepareRequest = features =>
                {
                    previousPrepareRequest?.Invoke(features);

                    if (clientCertificate != null)
                    {
                        features.Set<ITlsConnectionFeature>(new TlsConnectionFeature { ClientCertificate = clientCertificate });
                    }

                    features.Get<IHttpRequestFeature>().Scheme = "https";
                };
                context.Connection = sslStream;
            }
        }
    }
}
