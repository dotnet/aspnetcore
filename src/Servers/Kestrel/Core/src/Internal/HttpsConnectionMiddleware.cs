// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
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
            SslStream sslStream;
            bool certificateRequired;
            var feature = new Core.Internal.TlsConnectionFeature();
            context.Features.Set<ITlsConnectionFeature>(feature);
            context.Features.Set<ITlsHandshakeFeature>(feature);

            // TODO: Handle the cases where this can be null
            var memoryPoolFeature = context.Features.Get<IMemoryPoolFeature>();

            var inputPipeOptions = new PipeOptions
            (
                pool: memoryPoolFeature.MemoryPool,
                readerScheduler: _options.Scheduler,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: _options.MaxInputBufferSize ?? 0,
                resumeWriterThreshold: _options.MaxInputBufferSize / 2 ?? 0,
                useSynchronizationContext: false,
                minimumSegmentSize: memoryPoolFeature.MemoryPool.GetMinimumSegmentSize()
            );

            var outputPipeOptions = new PipeOptions
            (
                pool: memoryPoolFeature.MemoryPool,
                readerScheduler: PipeScheduler.Inline,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: _options.MaxOutputBufferSize ?? 0,
                resumeWriterThreshold: _options.MaxOutputBufferSize / 2 ?? 0,
                useSynchronizationContext: false,
                minimumSegmentSize: memoryPoolFeature.MemoryPool.GetMinimumSegmentSize()
            );

            // TODO: eventually make SslDuplexStream : Stream, IDuplexPipe to avoid RawStream allocation and pipe allocations
            var adaptedPipeline = new AdaptedPipeline(context.Transport, new Pipe(inputPipeOptions), new Pipe(outputPipeOptions), _logger, memoryPoolFeature.MemoryPool.GetMinimumAllocSize());
            var transportStream = adaptedPipeline.TransportStream;

            if (_options.ClientCertificateMode == ClientCertificateMode.NoCertificate)
            {
                sslStream = new SslStream(transportStream);
                certificateRequired = false;
            }
            else
            {
                sslStream = new SslStream(transportStream,
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
                    });

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
                            context.Features.Set(sslStream);
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

                    await sslStream.AuthenticateAsServerAsync(sslOptions, CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug(2, CoreStrings.AuthenticationTimedOut);
                    sslStream.Dispose();
                    return;
                }
                catch (Exception ex) when (ex is IOException || ex is AuthenticationException)
                {
                    _logger?.LogDebug(1, ex, CoreStrings.AuthenticationFailed);
                    sslStream.Dispose();
                    return;
                }
            }

            feature.ApplicationProtocol = sslStream.NegotiatedApplicationProtocol.Protocol;
            context.Features.Set<ITlsApplicationProtocolFeature>(feature);
            feature.ClientCertificate = ConvertToX509Certificate2(sslStream.RemoteCertificate);
            feature.CipherAlgorithm = sslStream.CipherAlgorithm;
            feature.CipherStrength = sslStream.CipherStrength;
            feature.HashAlgorithm = sslStream.HashAlgorithm;
            feature.HashStrength = sslStream.HashStrength;
            feature.KeyExchangeAlgorithm = sslStream.KeyExchangeAlgorithm;
            feature.KeyExchangeStrength = sslStream.KeyExchangeStrength;
            feature.Protocol = sslStream.SslProtocol;

            var original = context.Transport;

            try
            {
                context.Transport = adaptedPipeline;

                using (sslStream)
                {
                    try
                    {
                        adaptedPipeline.RunAsync(sslStream);

                        await _next(context);
                    }
                    finally
                    {
                        await adaptedPipeline.CompleteAsync();
                    }
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
    }
}
