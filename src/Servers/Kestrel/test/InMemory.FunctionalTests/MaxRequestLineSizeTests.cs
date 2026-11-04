// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class MaxRequestLineSizeTests : LoggedTest
{
    [Theory]
    [InlineData("GET / HTTP/1.1\r\nHost:\r\n\r\n", 16)]
    [InlineData("GET / HTTP/1.1\r\nHost:\r\n\r\n", 17)]
    [InlineData("GET / HTTP/1.1\r\nHost:\r\n\r\n", 137)]
    [InlineData("POST /abc/de HTTP/1.1\r\nHost:\r\nContent-Length: 0\r\n\r\n", 23)]
    [InlineData("POST /abc/de HTTP/1.1\r\nHost:\r\nContent-Length: 0\r\n\r\n", 24)]
    [InlineData("POST /abc/de HTTP/1.1\r\nHost:\r\nContent-Length: 0\r\n\r\n", 287)]
    [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\nHost:\r\nContent-Length: 0\r\n\r\n", 28)]
    [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\nHost:\r\nContent-Length: 0\r\n\r\n", 29)]
    [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\nHost:\r\nContent-Length: 0\r\n\r\n", 589)]
    [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\nHost:\r\n\r\n", 40)]
    [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\nHost:\r\n\r\n", 41)]
    [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\nHost:\r\n\r\n", 1027)]
    public async Task ServerAcceptsRequestLineWithinLimit(string request, int limit)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = CreateServer(limit, testMeterFactory))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(request);
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    $"Date: {server.Context.DateHeaderValue}",
                    "Transfer-Encoding: chunked",
                    "",
                    "c",
                    "hello, world",
                    "0",
                    "",
                    "");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    [Theory]
    [InlineData("GET / HTTP/1.1\r\n")]
    [InlineData("POST /abc/de HTTP/1.1\r\n")]
    [InlineData("PUT /abc/de?f=ghi HTTP/1.1\r\n")]
    [InlineData("DELETE /a%20b%20c/d%20e?f=ghi HTTP/1.1\r\n")]
    public async Task ServerRejectsRequestLineExceedingLimit(string requestLine)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        await using (var server = CreateServer(requestLine.Length - 1, testMeterFactory))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll(requestLine);
                await connection.ReceiveEnd(
                    "HTTP/1.1 414 URI Too Long",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.InvalidRequestLine, m.Tags));
    }

    private TestServer CreateServer(int maxRequestLineSize, IMeterFactory meterFactory)
    {
        return new TestServer(async httpContext => await httpContext.Response.WriteAsync("hello, world"), new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(meterFactory))
        {
            ServerOptions = new KestrelServerOptions
            {
                AddServerHeader = false,
                Limits =
                    {
                        MaxRequestLineSize = maxRequestLineSize
                    }
            }
        });
    }
}
