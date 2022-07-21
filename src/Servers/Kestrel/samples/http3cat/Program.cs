// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http3Cat;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace http3cat;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var host = new HostBuilder()
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
            })
            .UseHttp3Cat("https://localhost:5001", RunTestCase)
            .Build();
        await host.RunAsync();
    }

    internal static async Task RunTestCase(Http3Utilities h3Connection)
    {
        Console.WriteLine("In progress");
        //await h3Connection.InitializeConnectionAsync();

        //h3Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

        //await h3Connection.StartStreamAsync(1, Http3Utilities.BrowserRequestHeaders, endStream: true);

        //var headersFrame = await h3Connection.ReceiveFrameAsync();

        //Trace.Assert(headersFrame.Type == Http3FrameType.Headers, headersFrame.Type.ToString());
        //Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_HEADERS) != 0);
        //Trace.Assert((headersFrame.Flags & (byte)Http2HeadersFrameFlags.END_STREAM) == 0);

        //h3Connection.Logger.LogInformation("Received headers in a single frame.");

        //var decodedHeaders = h3Connection.DecodeHeaders(headersFrame);

        //foreach (var header in decodedHeaders)
        //{
        //    h3Connection.Logger.LogInformation($"{header.Key}: {header.Value}");
        //}

        //var dataFrame = await h3Connection.ReceiveFrameAsync();

        //Trace.Assert(dataFrame.Type == Http3FrameType.DATA);
        //Trace.Assert((dataFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 0);

        //h3Connection.Logger.LogInformation("Received data in a single frame.");

        //h3Connection.Logger.LogInformation(Encoding.UTF8.GetString(dataFrame.Payload.ToArray()));

        //var trailersFrame = await h3Connection.ReceiveFrameAsync();

        //Trace.Assert(trailersFrame.Type == Http3FrameType.Headers);
        //Trace.Assert((trailersFrame.Flags & (byte)Http2DataFrameFlags.END_STREAM) == 1);

        //h3Connection.Logger.LogInformation("Received trailers in a single frame.");

        //h3Connection.ResetHeaders();
        //var decodedTrailers = h3Connection.DecodeHeaders(trailersFrame);

        //foreach (var header in decodedTrailers)
        //{
        //    h3Connection.Logger.LogInformation($"{header.Key}: {header.Value}");
        //}

        //await h3Connection.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        //h3Connection.Logger.LogInformation("Connection stopped.");
    }
}
