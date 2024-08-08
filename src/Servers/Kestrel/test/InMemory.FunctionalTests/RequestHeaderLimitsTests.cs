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

public class RequestHeaderLimitsTests : LoggedTest
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 1337)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 1337)]
    [InlineData(5, 0)]
    [InlineData(5, 1)]
    [InlineData(5, 1337)]
    public async Task ServerAcceptsRequestWithHeaderTotalSizeWithinLimit(int headerCount, int extraLimit)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var headers = MakeHeaders(headerCount);

        await using (var server = CreateServer(maxRequestHeadersTotalSize: headers.Length + extraLimit, meterFactory: testMeterFactory))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send($"GET / HTTP/1.1\r\n{headers}\r\n");
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
    [InlineData(0, 1)]
    [InlineData(0, 1337)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 1337)]
    [InlineData(5, 5)]
    [InlineData(5, 6)]
    [InlineData(5, 1337)]
    public async Task ServerAcceptsRequestWithHeaderCountWithinLimit(int headerCount, int maxHeaderCount)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var headers = MakeHeaders(headerCount);

        await using (var server = CreateServer(maxRequestHeaderCount: maxHeaderCount, meterFactory: testMeterFactory))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send($"GET / HTTP/1.1\r\n{headers}\r\n");
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
    [InlineData(1)]
    [InlineData(5)]
    public async Task ServerRejectsRequestWithHeaderTotalSizeOverLimit(int headerCount)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var headers = MakeHeaders(headerCount);

        await using (var server = CreateServer(maxRequestHeadersTotalSize: headers.Length - 1, meterFactory: testMeterFactory))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll($"GET / HTTP/1.1\r\n{headers}\r\n");
                await connection.ReceiveEnd(
                    "HTTP/1.1 431 Request Header Fields Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.MaxRequestHeadersTotalSizeExceeded, m.Tags));
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(5, 1)]
    [InlineData(5, 4)]
    public async Task ServerRejectsRequestWithHeaderCountOverLimit(int headerCount, int maxHeaderCount)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var headers = MakeHeaders(headerCount);

        await using (var server = CreateServer(maxRequestHeaderCount: maxHeaderCount, meterFactory: testMeterFactory))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendAll($"GET / HTTP/1.1\r\n{headers}\r\n");
                await connection.ReceiveEnd(
                    "HTTP/1.1 431 Request Header Fields Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.MaxRequestHeaderCountExceeded, m.Tags));
    }

    private static string MakeHeaders(int count)
    {
        const string host = "Host:\r\n";
        if (count <= 1)
        {
            return host;
        }

        return string.Join("", new[] { host }
            .Concat(Enumerable
            .Range(0, count - 1)
            .Select(i => $"Header-{i}: value{i}\r\n")));
    }

    private TestServer CreateServer(int? maxRequestHeaderCount = null, int? maxRequestHeadersTotalSize = null, IMeterFactory meterFactory = null)
    {
        var options = new KestrelServerOptions { AddServerHeader = false };

        if (maxRequestHeaderCount.HasValue)
        {
            options.Limits.MaxRequestHeaderCount = maxRequestHeaderCount.Value;
        }

        if (maxRequestHeadersTotalSize.HasValue)
        {
            options.Limits.MaxRequestHeadersTotalSize = maxRequestHeadersTotalSize.Value;
        }

        var kestrelMetrics = meterFactory != null ? new KestrelMetrics(meterFactory) : null;
        return new TestServer(async httpContext => await httpContext.Response.WriteAsync("hello, world"), new TestServiceContext(LoggerFactory, metrics: kestrelMetrics)
        {
            ServerOptions = options
        });
    }
}
