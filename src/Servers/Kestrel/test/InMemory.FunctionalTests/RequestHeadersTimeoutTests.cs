// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class RequestHeadersTimeoutTests : LoggedTest
{
    private static readonly TimeSpan RequestHeadersTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LongDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ShortDelay = TimeSpan.FromSeconds(LongDelay.TotalSeconds / 10);

    [Theory]
    [InlineData("Host:\r\n")]
    [InlineData("Host:\r\nContent-Length: 1\r\n")]
    [InlineData("Host:\r\nContent-Length: 1\r\n\r")]
    public async Task ConnectionAbortedWhenRequestHeadersNotReceivedInTime(string headers)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                await connection.Send(
                    "GET / HTTP/1.1",
                    headers);

                // Min amount of time between requests that triggers a request headers timeout.
                testContext.FakeTimeProvider.Advance(RequestHeadersTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1));
                testContext.ConnectionManager.OnHeartbeat();

                await ReceiveTimeoutResponse(connection, testContext);
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.RequestHeadersTimeout, m.Tags));
    }

    [Fact]
    public async Task RequestHeadersTimeoutCanceledAfterHeadersReceived()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "",
                    "");

                // Min amount of time between requests that triggers a request headers timeout.
                testContext.FakeTimeProvider.Advance(RequestHeadersTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1));
                testContext.ConnectionManager.OnHeartbeat();

                await connection.Send(
                    "a");

                await ReceiveResponse(connection, testContext);
            }
        }

        
        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    [Theory]
    [InlineData("P")]
    [InlineData("POST / HTTP/1.1\r")]
    public async Task ConnectionAbortedWhenRequestLineNotReceivedInTime(string requestLine)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                await connection.Send(requestLine);

                // Min amount of time between requests that triggers a request headers timeout.
                testContext.FakeTimeProvider.Advance(RequestHeadersTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1));
                testContext.ConnectionManager.OnHeartbeat();

                await ReceiveTimeoutResponse(connection, testContext);
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.RequestHeadersTimeout, m.Tags));
    }

    [Fact]
    public async Task TimeoutNotResetOnEachRequestLineCharacterReceived()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

        // Disable response rate, so we can finish the send loop without timing out the response.
        testContext.ServerOptions.Limits.MinResponseDataRate = null;

        await using (var server = CreateServer(testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.TransportConnection.WaitForReadTask;

                foreach (var ch in "POST / HTTP/1.1\r\nHost:\r\n\r\n")
                {
                    await connection.Send(ch.ToString());

                    testContext.FakeTimeProvider.Advance(ShortDelay);
                    testContext.ConnectionManager.OnHeartbeat();
                }

                await ReceiveTimeoutResponse(connection, testContext);

                await connection.WaitForConnectionClose();
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.RequestHeadersTimeout, m.Tags));
    }

    private TestServer CreateServer(TestServiceContext context)
    {
        // Ensure request headers timeout is started as soon as the tests send requests.
        context.Scheduler = PipeScheduler.Inline;
        context.ServerOptions.Limits.RequestHeadersTimeout = RequestHeadersTimeout;
        context.ServerOptions.Limits.MinRequestBodyDataRate = null;

        return new TestServer(async httpContext =>
        {
            await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
            await httpContext.Response.WriteAsync("hello, world");
        }, context);
    }

    private async Task ReceiveResponse(InMemoryConnection connection, TestServiceContext testContext)
    {
        await connection.Receive(
            "HTTP/1.1 200 OK",
            $"Date: {testContext.DateHeaderValue}",
            "Transfer-Encoding: chunked",
            "",
            "c",
            "hello, world",
            "0",
            "",
            "");
    }

    private async Task ReceiveTimeoutResponse(InMemoryConnection connection, TestServiceContext testContext)
    {
        await connection.Receive(
            "HTTP/1.1 408 Request Timeout",
            "Content-Length: 0",
            "Connection: close",
            $"Date: {testContext.DateHeaderValue}",
            "",
            "");
    }
}
