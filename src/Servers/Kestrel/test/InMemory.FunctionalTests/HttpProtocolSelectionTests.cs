// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class HttpProtocolSelectionTests : TestApplicationErrorLoggerLoggedTest
{
    public static IEnumerable<object[]> Http2PrefaceSplitPositions()
    {
        for (var i = 1; i < Http2Connection.ClientPreface.Length; i++)
        {
            yield return new object[] { i };
        }
    }

    [Fact]
    public Task Server_NoProtocols_Error()
    {
        return TestError<InvalidOperationException>(HttpProtocols.None, CoreStrings.EndPointRequiresAtLeastOneProtocol);
    }

    [Fact]
    public Task Server_Http1AndHttp2_Cleartext_SelectsHttp1FromRequestLine()
    {
        return TestSuccess(HttpProtocols.Http1AndHttp2, "GET / HTTP/1.1\r\nHost:\r\n\r\n", "HTTP/1.1 200 OK");
    }

    [Theory]
    [InlineData(HttpProtocols.Http1AndHttp2)]
    [InlineData(HttpProtocols.Http1AndHttp2AndHttp3)]
    public Task Server_Cleartext_Http2PriorKnowledge(HttpProtocols protocols)
    {
        return TestSuccess(
            protocols,
            Encoding.ASCII.GetString(Http2Connection.ClientPreface),
            Encoding.ASCII.GetString(GetExpectedHttp2SettingsBytes()));
    }

    [Theory]
    [MemberData(nameof(Http2PrefaceSplitPositions))]
    public async Task Server_Http1AndHttp2_Cleartext_FragmentedHttp2Preface(int splitPosition)
    {
        var preface = Encoding.ASCII.GetString(Http2Connection.ClientPreface);

        await using var server = CreateServer();
        using var connection = server.CreateConnection();
        await connection.TransportConnection.WaitForReadTask;
        await connection.SendAll(preface[..splitPosition]);
        await connection.TransportConnection.WaitForAdvanceTask.DefaultTimeout();
        await connection.SendAll(preface[splitPosition..]);
        await connection.Receive(Encoding.ASCII.GetString(GetExpectedHttp2SettingsBytes()));
    }

    [Fact]
    public Task Server_Http1AndHttp2_Cleartext_FallbackToHttp1OnInvalidPreface()
    {
        return TestSuccess(HttpProtocols.Http1AndHttp2, "PRI / HTTP/1.1\r\nHost:\r\n\r\n", "HTTP/1.1 200 OK");
    }

    [Fact]
    public async Task Server_Http1AndHttp2_Cleartext_FragmentedHttp1Fallback()
    {
        await using var server = CreateServer();
        using var connection = server.CreateConnection();
        await connection.TransportConnection.WaitForReadTask;

        await connection.Send("P");
        await connection.Send("RI / HTTP/1.1\r\nHost:\r\n\r\n");

        await connection.Receive("HTTP/1.1 200 OK");
    }

    [Fact]
    public async Task Server_Http1AndHttp2_Cleartext_PartialHttp2PrefaceDoesNotDelayGracefulShutdown()
    {
        var testContext = new TestServiceContext(LoggerFactory)
        {
            ShutdownTimeout = TimeSpan.FromSeconds(60)
        };

        await using var server = CreateServer(testContext);
        using var connection = server.CreateConnection();
        await connection.TransportConnection.WaitForReadTask;
        await connection.Send("PRI");

        var shutdownTask = server.StopAsync();
        await connection.WaitForConnectionClose().DefaultTimeout();
        connection.Dispose();
        await shutdownTask.DefaultTimeout();
    }

    [Theory]
    [InlineData(HttpProtocols.Http1, false)]
    [InlineData(HttpProtocols.Http2, true)]
    [InlineData(HttpProtocols.Http1AndHttp2, true)]
    public async Task Server_Cleartext_ConnectionProtocolOverrideIsRespected(HttpProtocols protocols, bool expectsHttp2)
    {
        await using var server = CreateServer();
        using var connection = server.CreateConnection(featuresAction: features =>
            features.Set(new HttpProtocolsFeature(protocols)));

        await connection.Stream.WriteAsync(Http2Connection.ClientPreface.ToArray());

        await connection.Receive(Encoding.ASCII.GetString(expectsHttp2
            ? GetExpectedHttp2SettingsBytes()
            : Http1Connection.Http2GoAwayHttp11RequiredBytes));
    }

    [Fact]
    public async Task Server_Http1AndHttp2_Cleartext_Http1FallbackProcessesSecondRequest()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        await using var server = CreateServer(testContext);
        using var connection = server.CreateConnection();

        await connection.Send("GET /first HTTP/1.1\r\nHost:\r\n\r\n");
        await ReceiveHttp1Response(connection, testContext);
        await connection.Send("GET /second HTTP/1.1\r\nHost:\r\n\r\n");
        await ReceiveHttp1Response(connection, testContext);
    }

    [Fact]
    public async Task Server_Http1AndHttp2_Cleartext_Http2PriorKnowledgeDisabled()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions.DisableHttp2PriorKnowledge = true;
        await using var server = CreateServer(testContext);
        using var connection = server.CreateConnection();

        await connection.Stream.WriteAsync(Http2Connection.ClientPreface.ToArray());

        await connection.Receive(Encoding.ASCII.GetString(Http1Connection.Http2GoAwayHttp11RequiredBytes));
    }

    [Fact]
    public async Task Server_Http1AndHttp2_Cleartext_InfiniteKeepAliveDoesNotFaultSelection()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        testContext.InitializeHeartbeat();
        testContext.ServerOptions.Limits.KeepAliveTimeout = Timeout.InfiniteTimeSpan;
        await using var server = CreateServer(testContext);
        using var connection = server.CreateConnection();

        await connection.Send("GET / HTTP/1.1\r\nHost:\r\n\r\n");

        await connection.Receive("HTTP/1.1 200 OK");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Http1AndHttp2_Cleartext_SelectionReadFailureHasExpectedTelemetry(bool partialPreface)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, KestrelMetrics.MeterName, "kestrel.connection.duration");
        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
        {
            Scheduler = PipeScheduler.Inline
        };
        await using var server = CreateServer(testContext);
        using var connection = server.CreateConnection();
        await connection.TransportConnection.WaitForReadTask;

        if (partialPreface)
        {
            await connection.SendAll("PRI");
            await connection.TransportConnection.WaitForAdvanceTask.DefaultTimeout();
        }

        var exception = new IOException("selection read failed");
        connection.TransportConnection.Input.Complete(exception);
        connection.TransportConnection.OnClosed();

        await connection.WaitForConnectionClose().DefaultTimeout();
        var log = Assert.Single(LogMessages, message => ReferenceEquals(message.Exception, exception));
        Assert.Equal(20, log.EventId.Id);
        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), measurement =>
        {
            MetricsAssert.Equal(ConnectionEndReason.IOError, measurement.Tags);
            Assert.DoesNotContain("network.protocol.name", measurement.Tags.Keys);
            Assert.DoesNotContain("network.protocol.version", measurement.Tags.Keys);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Server_Http1AndHttp2_Cleartext_SelectionResetIsNormalCompletion(bool partialPreface)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, KestrelMetrics.MeterName, "kestrel.connection.duration");
        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
        {
            Scheduler = PipeScheduler.Inline
        };
        await using var server = CreateServer(testContext);
        using var connection = server.CreateConnection();
        await connection.TransportConnection.WaitForReadTask;

        if (partialPreface)
        {
            await connection.SendAll("PRI");
            await connection.TransportConnection.WaitForAdvanceTask.DefaultTimeout();
        }

        connection.Reset();

        await connection.WaitForConnectionClose().DefaultTimeout();
        Assert.DoesNotContain(LogMessages, message => message.Exception is ConnectionResetException);
        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), measurement =>
        {
            MetricsAssert.NoError(measurement.Tags);
            Assert.DoesNotContain("network.protocol.name", measurement.Tags.Keys);
            Assert.DoesNotContain("network.protocol.version", measurement.Tags.Keys);
        });
    }

    [Fact]
    public Task Server_Http1Only_Cleartext_Success()
    {
        return TestSuccess(HttpProtocols.Http1, "GET / HTTP/1.1\r\nHost:\r\n\r\n", "HTTP/1.1 200 OK");
    }

    [Fact]
    public Task Server_Http2Only_Cleartext_Success()
    {
        return TestSuccess(
            HttpProtocols.Http2,
            Encoding.ASCII.GetString(Http2Connection.ClientPreface),
            Encoding.ASCII.GetString(GetExpectedHttp2SettingsBytes()));
    }

    private static byte[] GetExpectedHttp2SettingsBytes()
    {
        return
        [
            0x00, 0x00, 0x18, // Payload Length (6 * settings count)
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, // SETTINGS frame (type 0x04)
            0x00, 0x03, 0x00, 0x00, 0x00, 0x64, // Connection limit (100)
            0x00, 0x04, 0x00, 0x0C, 0x00, 0x00, // Initial stream window size (768 KiB)
            0x00, 0x06, 0x00, 0x00, 0x80, 0x00, // Header size limit (32 KiB)
            0x00, 0x08, 0x00, 0x00, 0x00, 0x01, // CONNECT enabled
            0x00, 0x00, 0x04, // Payload Length (4)
            0x08, 0x00, 0x00, 0x00, 0x00, 0x00, // WINDOW_UPDATE frame (type 0x08)
            0x00, 0x0F, 0x00, 0x01, // Diff between configured and protocol default (1 MiB - 0XFFFF)
        ];
    }

    private TestServer CreateServer(
        TestServiceContext testContext = null,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2,
        RequestDelegate application = null)
        => new(
            application ?? (context => Task.CompletedTask),
            testContext ?? new TestServiceContext(LoggerFactory),
            listenOptions => listenOptions.Protocols = protocols);

    private async Task TestSuccess(HttpProtocols serverProtocols, string request, string expectedResponse)
    {
        await using var server = CreateServer(protocols: serverProtocols);
        using var connection = server.CreateConnection();
        await connection.Send(request);
        await connection.Receive(expectedResponse);
    }

    private static Task ReceiveHttp1Response(InMemoryConnection connection, TestServiceContext testContext)
        => connection.Receive(
            "HTTP/1.1 200 OK",
            "Content-Length: 0",
            $"Date: {testContext.DateHeaderValue}",
            "",
            "");

    private async Task TestError<TException>(HttpProtocols serverProtocols, string expectedErrorMessage)
        where TException : Exception
    {
        await using var server = CreateServer(protocols: serverProtocols);
        using var connection = server.CreateConnection();
        await connection.WaitForConnectionClose();

        Assert.Single(LogMessages, message => message.LogLevel == LogLevel.Error
            && message.EventId.Id == 0
            && message.Message == expectedErrorMessage);
    }
}
