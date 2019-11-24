// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace http2cat
{
    internal class Http2CatHostedService : BackgroundService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<Http2CatHostedService> _logger;

        public Http2CatHostedService(IConnectionFactory connectionFactory, ILogger<Http2CatHostedService> logger,
            IHostApplicationLifetime applicationLifetime, IOptions<Http2CatOptions> options)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            ApplicationLifetime = applicationLifetime;
            Options = options.Value;
        }

        public IHostApplicationLifetime ApplicationLifetime { get; }

        private Http2CatOptions Options { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
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

                if (address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Starting TLS handshake.");

                    var memoryPool = context.Features.Get<IMemoryPoolFeature>()?.MemoryPool;
                    var inputPipeOptions = new StreamPipeReaderOptions(memoryPool, memoryPool.GetMinimumSegmentSize(), memoryPool.GetMinimumAllocSize(), leaveOpen: true);
                    var outputPipeOptions = new StreamPipeWriterOptions(pool: memoryPool, leaveOpen: true);

                    var sslDuplexPipe = new SslDuplexPipe(context.Transport, inputPipeOptions, outputPipeOptions);
                    var sslStream = sslDuplexPipe.Stream;

                    var originalTransport = context.Transport;
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

                await Options.Scenaro(http2Utilities, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "App error");
            }
            finally
            {
                // Exit
                ApplicationLifetime.StopApplication();
            }
        }
    }
}
