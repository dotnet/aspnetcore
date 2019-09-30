// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
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
                var endpoint = new IPEndPoint(IPAddress.Loopback, 5005);

                _logger.LogInformation($"Connecting to '{endpoint}'.");

                await using var connectionContext = await _connectionFactory.ConnectAsync(endpoint);

                _logger.LogInformation($"Connected to '{endpoint}'.");

                var http2Utilities = new Http2Utilities(connectionContext);

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
        }
    }
}
