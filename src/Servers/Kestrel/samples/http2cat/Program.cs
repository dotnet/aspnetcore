// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace http2cat
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            using var host = new HostBuilder()
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                })
                .ConfigureServices(services =>
                {
                    services.UseHttp2Cat(options =>
                    {
                        options.Url = "https://localhost:5001";
                        options.Scenaro = RunTestCase;
                    });
                })
                .Build();

            await host.RunAsync();
        }

        public static async Task RunTestCase(Http2Utilities http2Utilities, ILogger logger)
        {
            await http2Utilities.InitializeConnectionAsync();

            logger.LogInformation("Initialized http2 connection. Starting stream 1.");

            await http2Utilities.StartStreamAsync(1, Http2Utilities._browserRequestHeaders, endStream: true);

            var headersFrame = await http2Utilities.ReceiveFrameAsync();

            Trace.Assert(headersFrame.Type == Http2FrameType.HEADERS);
            Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
            Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) == 0);

            logger.LogInformation("Received headers in a single frame.");

            var decodedHeaders = http2Utilities.DecodeHeaders(headersFrame);

            foreach (var header in decodedHeaders)
            {
                logger.LogInformation($"{header.Key}: {header.Value}");
            }

            var dataFrame = await http2Utilities.ReceiveFrameAsync();

            Trace.Assert(dataFrame.Type == Http2FrameType.DATA);
            Trace.Assert((dataFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 0);

            logger.LogInformation("Received data in a single frame.");

            logger.LogInformation(Encoding.UTF8.GetString(dataFrame.Payload.ToArray()));

            var trailersFrame = await http2Utilities.ReceiveFrameAsync();

            Trace.Assert(trailersFrame.Type == Http2FrameType.HEADERS);
            Trace.Assert((trailersFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 1);

            logger.LogInformation("Received trailers in a single frame.");

            http2Utilities._decodedHeaders.Clear();

            var decodedTrailers = http2Utilities.DecodeHeaders(trailersFrame);

            foreach (var header in decodedHeaders)
            {
                logger.LogInformation($"{header.Key}: {header.Value}");
            }

            await http2Utilities.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            logger.LogInformation("Connection stopped.");
        }
    }
}
