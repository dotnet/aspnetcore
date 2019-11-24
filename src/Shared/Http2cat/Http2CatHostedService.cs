// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace http2cat
{
    internal class Http2CatHostedService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<Http2CatHostedService> _logger;

        public Http2CatHostedService(IConnectionFactory connectionFactory, ILogger<Http2CatHostedService> logger,
            IOptions<Http2CatOptions> options)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            Options = options.Value;
        }

        private Http2CatOptions Options { get; }

        public async Task RunAsync()
        {
            var address = BindingAddress.Parse(Options.Url);

            if (!IPAddress.TryParse(address.Host, out var ip))
            {
                ip = Dns.GetHostEntry(address.Host).AddressList.First();
            }

            var endpoint = new IPEndPoint(ip, address.Port);

            _logger.LogInformation($"Connecting to '{endpoint}'.");

            await using var context = await _connectionFactory.ConnectAsync(endpoint);

            _logger.LogInformation($"Connected to '{endpoint}'.");

            var originalTransport = context.Transport;
            if (address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Starting TLS handshake.");

                var memoryPool = context.Features.Get<IMemoryPoolFeature>()?.MemoryPool;
                var inputPipeOptions = new StreamPipeReaderOptions(memoryPool, memoryPool.GetMinimumSegmentSize(), memoryPool.GetMinimumAllocSize(), leaveOpen: true);
                var outputPipeOptions = new StreamPipeWriterOptions(pool: memoryPool, leaveOpen: true);

                var sslDuplexPipe = new SslDuplexPipe(context.Transport, inputPipeOptions, outputPipeOptions);
                var sslStream = sslDuplexPipe.Stream;

                context.Transport = sslDuplexPipe;

                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = address.Host,
                    RemoteCertificateValidationCallback = (_, __, ___, ____) => true,
                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 },
                    EnabledSslProtocols = SslProtocols.Tls12,
                }, CancellationToken.None);

                _logger.LogInformation($"TLS handshake completed successfully.");
            }

            var http2Utilities = new Http2Utilities(context);

            try
            {
                await Options.Scenaro(http2Utilities, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "App error");
                throw;
            }
            finally
            {
                // Unwind Https for shutdown. This must happen before context goes ot of scope or else DisposeAsync will hang
                context.Transport = originalTransport;
            }
        }
    }
}
