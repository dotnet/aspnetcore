// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http2Cat;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace http2cat
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using var host = new HostBuilder()
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                })
                .UseHttp2Cat("https://localhost:5001", RunTestCase)
                .Build();

            await host.RunAsync();
        }

        internal static async Task RunTestCase(Http2Utilities h2Connection)
        {
            await h2Connection.InitializeConnectionAsync();

            h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

            await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

            var headersFrame = await h2Connection.ReceiveFrameAsync();

            Trace.Assert(headersFrame.Type == Http2FrameType.HEADERS, headersFrame.Type.ToString());
            Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
            Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) == 0);

            h2Connection.Logger.LogInformation("Received headers in a single frame.");

            var decodedHeaders = h2Connection.DecodeHeaders(headersFrame);

            foreach (var header in decodedHeaders)
            {
                h2Connection.Logger.LogInformation($"{header.Key}: {header.Value}");
            }

            var dataFrame = await h2Connection.ReceiveFrameAsync();

            Trace.Assert(dataFrame.Type == Http2FrameType.DATA);
            Trace.Assert((dataFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 0);

            h2Connection.Logger.LogInformation("Received data in a single frame.");

            h2Connection.Logger.LogInformation(Encoding.UTF8.GetString(dataFrame.Payload.ToArray()));

            var trailersFrame = await h2Connection.ReceiveFrameAsync();

            Trace.Assert(trailersFrame.Type == Http2FrameType.HEADERS);
            Trace.Assert((trailersFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 1);

            h2Connection.Logger.LogInformation("Received trailers in a single frame.");

            h2Connection.ResetHeaders();
            var decodedTrailers = h2Connection.DecodeHeaders(trailersFrame);

            foreach (var header in decodedTrailers)
            {
                h2Connection.Logger.LogInformation($"{header.Key}: {header.Value}");
            }

            await h2Connection.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            h2Connection.Logger.LogInformation("Connection stopped.");
        }
    }
}
