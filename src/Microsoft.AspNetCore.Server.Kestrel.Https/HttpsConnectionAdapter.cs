// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    public class HttpsConnectionAdapter : IConnectionAdapter
    {
        private static readonly ClosedAdaptedConnection _closedAdaptedConnection = new ClosedAdaptedConnection();

        private readonly HttpsConnectionAdapterOptions _options;
        private readonly ILogger _logger;

        public HttpsConnectionAdapter(HttpsConnectionAdapterOptions options)
            : this(options, loggerFactory: null)
        {
        }

        public HttpsConnectionAdapter(HttpsConnectionAdapterOptions options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.ServerCertificate == null)
            {
                throw new ArgumentException("The server certificate parameter is required.");
            }

            _options = options;
            _logger = loggerFactory?.CreateLogger(nameof(HttpsConnectionAdapter));
        }

        public async Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
        {
            SslStream sslStream;
            bool certificateRequired;

            if (_options.ClientCertificateMode == ClientCertificateMode.NoCertificate)
            {
                sslStream = new SslStream(context.ConnectionStream);
                certificateRequired = false;
            }
            else
            {
                sslStream = new SslStream(context.ConnectionStream, leaveInnerStreamOpen: false,
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

                        var certificate2 = ConvertToX509Certificate2(certificate);
                        if (certificate2 == null)
                        {
                            return false;
                        }

                        if (_options.ClientCertificateValidation != null)
                        {
                            if (!_options.ClientCertificateValidation(certificate2, chain, sslPolicyErrors))
                            {
                                return false;
                            }
                        }

                        return true;
                    });

                certificateRequired = true;
            }

            try
            {
                await sslStream.AuthenticateAsServerAsync(_options.ServerCertificate, certificateRequired,
                        _options.SslProtocols, _options.CheckCertificateRevocation);
            }
            catch (IOException ex)
            {
                _logger?.LogInformation(1, ex, "Failed to authenticate HTTPS connection.");
                sslStream.Dispose();
                return _closedAdaptedConnection;
            }

            return new HttpsAdaptedConnection(sslStream);
        }

        private static X509Certificate2 ConvertToX509Certificate2(X509Certificate certificate)
        {
            if (certificate == null)
            {
                return null;
            }

            X509Certificate2 certificate2 = certificate as X509Certificate2;
            if (certificate2 != null)
            {
                return certificate2;
            }

#if NETSTANDARD1_3
            // conversion X509Certificate to X509Certificate2 not supported
            // https://github.com/dotnet/corefx/issues/4510
            return null;
#else
            return new X509Certificate2(certificate);
#endif
        }

        private class HttpsAdaptedConnection : IAdaptedConnection
        {
            private readonly SslStream _sslStream;

            public HttpsAdaptedConnection(SslStream sslStream)
            {
                _sslStream = sslStream;
            }

            public Stream ConnectionStream => _sslStream;

            public void PrepareRequest(IFeatureCollection requestFeatures)
            {
                var clientCertificate = ConvertToX509Certificate2(_sslStream.RemoteCertificate);
                if (clientCertificate != null)
                {
                    requestFeatures.Set<ITlsConnectionFeature>(new TlsConnectionFeature { ClientCertificate = clientCertificate });
                }

                requestFeatures.Get<IHttpRequestFeature>().Scheme = "https";
            }
        }

        private class ClosedAdaptedConnection : IAdaptedConnection
        {
            public Stream ConnectionStream { get; } = new ClosedStream();

            public void PrepareRequest(IFeatureCollection requestFeatures)
            {
            }
        }
    }
}
