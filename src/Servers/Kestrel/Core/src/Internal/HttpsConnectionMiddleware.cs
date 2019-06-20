// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal
{
    internal class HttpsConnectionMiddleware
    {
        private readonly ConnectionDelegate _next;

        private readonly HttpsConnectionAdapterOptions _options;
        private readonly ILogger _logger;
        private readonly X509Certificate2 _serverCertificate;
        private readonly Func<ConnectionContext, string, X509Certificate2> _serverCertificateSelector;

        public HttpsConnectionMiddleware(ConnectionDelegate next, HttpsConnectionAdapterOptions options)
          : this(next, options, loggerFactory: NullLoggerFactory.Instance)
        {
        }

        public HttpsConnectionMiddleware(ConnectionDelegate next, HttpsConnectionAdapterOptions options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // This configuration will always fail per-request, preemptively fail it here. See HttpConnection.SelectProtocol().
            if (options.HttpProtocols == HttpProtocols.Http2 && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                throw new NotSupportedException(CoreStrings.HTTP2NoTlsOsx);
            }

            _next = next;
            // capture the certificate now so it can't be switched after validation
            _serverCertificate = options.ServerCertificate;
            _serverCertificateSelector = options.ServerCertificateSelector;
            if (_serverCertificate == null && _serverCertificateSelector == null)
            {
                throw new ArgumentException(CoreStrings.ServerCertificateRequired, nameof(options));
            }

            // If a selector is provided then ignore the cert, it may be a default cert.
            if (_serverCertificateSelector != null)
            {
                // SslStream doesn't allow both.
                _serverCertificate = null;
            }
            else
            {
                EnsureCertificateIsAllowedForServerAuth(_serverCertificate);
            }

            _options = options;
            _logger = loggerFactory?.CreateLogger<HttpsConnectionMiddleware>();
        }
        public Task OnConnectionAsync(ConnectionContext context)
        {
            return Task.Run(() => InnerOnConnectionAsync(context));
        }

        private async Task InnerOnConnectionAsync(ConnectionContext context)
        {
            bool certificateRequired;
            var feature = new Core.Internal.TlsConnectionFeature();
            context.Features.Set<ITlsConnectionFeature>(feature);
            context.Features.Set<ITlsHandshakeFeature>(feature);

            var memoryPoolFeature = context.Features.Get<IMemoryPoolFeature>();

            var inputPipeOptions = new StreamPipeReaderOptions
            (
                pool: memoryPoolFeature.MemoryPool,
                bufferSize: memoryPoolFeature.MemoryPool.GetMinimumSegmentSize(),
                minimumReadSize: memoryPoolFeature.MemoryPool.GetMinimumAllocSize(),
                leaveOpen: true
            );

            var outputPipeOptions = new StreamPipeWriterOptions
            (
                pool: memoryPoolFeature.MemoryPool,
                leaveOpen: true
            );

            SslDuplexPipe sslDuplexPipe = null;

            if (_options.ClientCertificateMode == ClientCertificateMode.NoCertificate)
            {
                sslDuplexPipe = new SslDuplexPipe(context.Transport, inputPipeOptions, outputPipeOptions);
                certificateRequired = false;
            }
            else
            {
                sslDuplexPipe = new SslDuplexPipe(context.Transport, inputPipeOptions, outputPipeOptions, s => new SslStream(s,
                    leaveInnerStreamOpen: false,
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
                    }));

                certificateRequired = true;
            }

            using (var cancellationTokeSource = new CancellationTokenSource(_options.HandshakeTimeout))
            using (cancellationTokeSource.Token.UnsafeRegister(state => ((ConnectionContext)state).Abort(), context))
            {
                try
                {
                    // Adapt to the SslStream signature
                    ServerCertificateSelectionCallback selector = null;
                    if (_serverCertificateSelector != null)
                    {
                        selector = (sender, name) =>
                        {
                            context.Features.Set(sslDuplexPipe.Stream);
                            var cert = _serverCertificateSelector(context, name);
                            if (cert != null)
                            {
                                EnsureCertificateIsAllowedForServerAuth(cert);
                            }
                            return cert;
                        };
                    }

                    var sslOptions = new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _serverCertificate,
                        ServerCertificateSelectionCallback = selector,
                        ClientCertificateRequired = certificateRequired,
                        EnabledSslProtocols = _options.SslProtocols,
                        CertificateRevocationCheckMode = _options.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                        ApplicationProtocols = new List<SslApplicationProtocol>()
                    };

                    // This is order sensitive
                    if ((_options.HttpProtocols & HttpProtocols.Http2) != 0)
                    {
                        sslOptions.ApplicationProtocols.Add(SslApplicationProtocol.Http2);
                        // https://tools.ietf.org/html/rfc7540#section-9.2.1
                        sslOptions.AllowRenegotiation = false;
                    }

                    if ((_options.HttpProtocols & HttpProtocols.Http1) != 0)
                    {
                        sslOptions.ApplicationProtocols.Add(SslApplicationProtocol.Http11);
                    }

                    _options.OnAuthenticate?.Invoke(context, sslOptions);

                    await sslDuplexPipe.Stream.AuthenticateAsServerAsync(sslOptions, CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug(2, CoreStrings.AuthenticationTimedOut);
                    await sslDuplexPipe.Stream.DisposeAsync();
                    return;
                }
                catch (Exception ex) when (ex is IOException || ex is AuthenticationException)
                {
                    _logger?.LogDebug(1, ex, CoreStrings.AuthenticationFailed);
                    await sslDuplexPipe.Stream.DisposeAsync();
                    return;
                }
            }

            feature.ApplicationProtocol = sslDuplexPipe.Stream.NegotiatedApplicationProtocol.Protocol;
            context.Features.Set<ITlsApplicationProtocolFeature>(feature);
            feature.ClientCertificate = ConvertToX509Certificate2(sslDuplexPipe.Stream.RemoteCertificate);
            feature.CipherAlgorithm = sslDuplexPipe.Stream.CipherAlgorithm;
            feature.CipherStrength = sslDuplexPipe.Stream.CipherStrength;
            feature.HashAlgorithm = sslDuplexPipe.Stream.HashAlgorithm;
            feature.HashStrength = sslDuplexPipe.Stream.HashStrength;
            feature.KeyExchangeAlgorithm = sslDuplexPipe.Stream.KeyExchangeAlgorithm;
            feature.KeyExchangeStrength = sslDuplexPipe.Stream.KeyExchangeStrength;
            feature.Protocol = sslDuplexPipe.Stream.SslProtocol;

            var original = context.Transport;

            try
            {
                context.Transport = sslDuplexPipe;

                // Disposing the stream will dispose the sslDuplexPipe
                await using (sslDuplexPipe.Stream)
                {
                    await _next(context);
                }
            }
            finally
            {
                // Restore the original so that it gets closed appropriately
                context.Transport = original;
            }
        }

        private static void EnsureCertificateIsAllowedForServerAuth(X509Certificate2 certificate)
        {
            if (!CertificateLoader.IsCertificateAllowedForServerAuth(certificate))
            {
                throw new InvalidOperationException(CoreStrings.FormatInvalidServerCertificateEku(certificate.Thumbprint));
            }
        }

        private static X509Certificate2 ConvertToX509Certificate2(X509Certificate certificate)
        {
            if (certificate == null)
            {
                return null;
            }

            if (certificate is X509Certificate2 cert2)
            {
                return cert2;
            }

            return new X509Certificate2(certificate);
        }

        private class SslDuplexPipe : DuplexPipeStreamAdapter<SslStream>
        {
            public SslDuplexPipe(IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions)
                : this(transport, readerOptions, writerOptions, s => new SslStream(s))
            {

            }

            public SslDuplexPipe(IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, SslStream> factory) :
                base(transport, readerOptions, writerOptions, factory)
            {
            }
        }
    }
}
