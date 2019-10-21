// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace http2cat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConnectionFactory, SocketConnectionFactory>();
                    services.AddSingleton<Http2CatHostedService>();
                })
                .Build();

            await host.Services.GetService<Http2CatHostedService>().RunAsync();
        }

        private class Http2CatHostedService
        {
            private readonly IConnectionFactory _connectionFactory;
            private readonly ILogger<Http2CatHostedService> _logger;

            public Http2CatHostedService(IConnectionFactory connectionFactory, ILogger<Http2CatHostedService> logger)
            {
                _connectionFactory = connectionFactory;
                _logger = logger;
            }

            public async Task RunAsync()
            {
                var endpoint = new IPEndPoint(IPAddress.Loopback, 5001);

                _logger.LogInformation($"Connecting to '{endpoint}'.");

                await using var context = await _connectionFactory.ConnectAsync(endpoint);

                _logger.LogInformation($"Connected to '{endpoint}'. Starting TLS handshake.");

                var memoryPool = context.Features.Get<IMemoryPoolFeature>()?.MemoryPool;
                var inputPipeOptions = new StreamPipeReaderOptions(memoryPool, memoryPool.GetMinimumSegmentSize(), memoryPool.GetMinimumAllocSize(), leaveOpen: true);
                var outputPipeOptions = new StreamPipeWriterOptions(pool: memoryPool, leaveOpen: true);

                await using var sslDuplexPipe = new SslDuplexPipe(context.Transport, inputPipeOptions, outputPipeOptions);
                await using var sslStream = sslDuplexPipe.Stream;

                var originalTransport = context.Transport;
                context.Transport = sslDuplexPipe;

                try
                {
                    await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = "localhost",
                        RemoteCertificateValidationCallback = (_, __, ___, ____) => true,
                        ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 },
                        EnabledSslProtocols = SslProtocols.Tls12,
                    }, CancellationToken.None);

                    _logger.LogInformation($"TLS handshake completed successfully.");

                    var http2Utilities = new Http2Utilities(context);

                    await http2Utilities.InitializeConnectionAsync();

                    _logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await http2Utilities.StartStreamAsync(1, Http2Utilities._browserRequestHeaders, endStream: true);

                    var headersFrame = await http2Utilities.ReceiveFrameAsync();

                    Trace.Assert(headersFrame.Type == Http2FrameType.HEADERS);
                    Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
                    Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) == 0);

                    _logger.LogInformation("Received headers in a single frame.");

                    var decodedHeaders = http2Utilities.DecodeHeaders(headersFrame);
                    
                    foreach (var header in decodedHeaders)
                    {
                        _logger.LogInformation($"{header.Key}: {header.Value}");
                    }

                    var dataFrame = await http2Utilities.ReceiveFrameAsync();

                    Trace.Assert(dataFrame.Type == Http2FrameType.DATA);
                    Trace.Assert((dataFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 0);

                    _logger.LogInformation("Received data in a single frame.");

                    _logger.LogInformation(Encoding.UTF8.GetString(dataFrame.Payload.ToArray()));

                    var trailersFrame = await http2Utilities.ReceiveFrameAsync();

                    Trace.Assert(trailersFrame.Type == Http2FrameType.HEADERS);
                    Trace.Assert((trailersFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 1);

                    _logger.LogInformation("Received trailers in a single frame.");

                    http2Utilities._decodedHeaders.Clear();

                    var decodedTrailers = http2Utilities.DecodeHeaders(trailersFrame);

                    foreach (var header in decodedHeaders)
                    {
                        _logger.LogInformation($"{header.Key}: {header.Value}");
                    }

                    await http2Utilities.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

                    _logger.LogInformation("Connection stopped.");
                }
                finally
                {
                    context.Transport = originalTransport;
                }
            }
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
