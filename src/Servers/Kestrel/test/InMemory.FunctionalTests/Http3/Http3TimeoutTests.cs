// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http3TimeoutTests : Http3TestBase
{
    [Fact]
    public async Task KeepAliveTimeout_ControlStreamNotReceived_ConnectionClosed()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        await Http3Api.InitializeConnectionAsync(_noopApplication).DefaultTimeout();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        Http3Api.AdvanceTime(limits.KeepAliveTimeout + TimeSpan.FromTicks(1));

        await Http3Api.WaitForConnectionStopAsync(0, false, expectedErrorCode: Http3ErrorCode.NoError);
        MetricsAssert.Equal(ConnectionEndReason.KeepAliveTimeout, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task KeepAliveTimeout_RequestNotReceived_ConnectionClosed()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        await Http3Api.InitializeConnectionAsync(_noopApplication).DefaultTimeout();
        await Http3Api.CreateControlStream();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        Http3Api.AdvanceTime(limits.KeepAliveTimeout + TimeSpan.FromTicks(1));

        await Http3Api.WaitForConnectionStopAsync(0, false, expectedErrorCode: Http3ErrorCode.NoError);
        MetricsAssert.Equal(ConnectionEndReason.KeepAliveTimeout, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task KeepAliveTimeout_AfterRequestComplete_ConnectionClosed()
    {
        var requestHeaders = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        var limits = _serviceContext.ServerOptions.Limits;

        await Http3Api.InitializeConnectionAsync(_noopApplication).DefaultTimeout();

        await Http3Api.CreateControlStream();
        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();
        var requestStream = await Http3Api.CreateRequestStream(requestHeaders, endStream: true);
        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
        await requestStream.OnDisposedTask.DefaultTimeout();

        Http3Api.AdvanceTime(limits.KeepAliveTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1));

        await Http3Api.WaitForConnectionStopAsync(4, false, expectedErrorCode: Http3ErrorCode.NoError);
        MetricsAssert.Equal(ConnectionEndReason.KeepAliveTimeout, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task KeepAliveTimeout_LongRunningRequest_KeepsConnectionAlive()
    {
        var requestHeaders = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        var limits = _serviceContext.ServerOptions.Limits;
        var requestReceivedTcs = new TaskCompletionSource();
        var requestFinishedTcs = new TaskCompletionSource();

        await Http3Api.InitializeConnectionAsync(_ =>
        {
            requestReceivedTcs.SetResult();
            return requestFinishedTcs.Task;
        }).DefaultTimeout();

        await Http3Api.CreateControlStream();
        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();
        var requestStream = await Http3Api.CreateRequestStream(requestHeaders, endStream: true);

        await requestReceivedTcs.Task;

        Http3Api.AdvanceTime(limits.KeepAliveTimeout);
        Http3Api.AdvanceTime(limits.KeepAliveTimeout);
        Http3Api.AdvanceTime(limits.KeepAliveTimeout);
        Http3Api.AdvanceTime(limits.KeepAliveTimeout);
        Http3Api.AdvanceTime(limits.KeepAliveTimeout);

        requestFinishedTcs.SetResult();

        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
        await requestStream.OnDisposedTask.DefaultTimeout();

        Http3Api.AdvanceTime(limits.KeepAliveTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1));

        await Http3Api.WaitForConnectionStopAsync(4, false, expectedErrorCode: Http3ErrorCode.NoError);
        MetricsAssert.Equal(ConnectionEndReason.KeepAliveTimeout, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task HEADERS_IncompleteFrameReceivedWithinRequestHeadersTimeout_StreamError()
    {
        var timeProvider = _serviceContext.FakeTimeProvider;
        var timestamp = timeProvider.GetTimestamp();
        var limits = _serviceContext.ServerOptions.Limits;

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, null).DefaultTimeout();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        await requestStream.SendHeadersPartialAsync().DefaultTimeout();

        await requestStream.OnStreamCreatedTask;

        var serverRequestStream = Http3Api.Connection._streams[requestStream.StreamId];

        Http3Api.TriggerTick();
        Http3Api.TriggerTick(limits.RequestHeadersTimeout);

        Assert.Equal(timeProvider.GetTimestamp(timestamp, limits.RequestHeadersTimeout), serverRequestStream.StreamTimeoutTimestamp);

        Http3Api.TriggerTick(TimeSpan.FromTicks(1));

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.RequestRejected,
            AssertExpectedErrorMessages,
            CoreStrings.BadRequest_RequestHeadersTimeout);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HEADERS_HeaderFrameReceivedWithinRequestHeadersTimeout_Success(bool pendingStreamsEnabled)
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = pendingStreamsEnabled;

        var timestamp = _serviceContext.FakeTimeProvider.GetTimestamp();
        var limits = _serviceContext.ServerOptions.Limits;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, null).DefaultTimeout();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        dynamic serverRequestStream;

        if (pendingStreamsEnabled)
        {
            await requestStream.OnUnidentifiedStreamCreatedTask.DefaultTimeout();

            serverRequestStream = Http3Api.Connection._unidentifiedStreams[requestStream.StreamId];
        }
        else
        {
            await requestStream.OnStreamCreatedTask.DefaultTimeout();

            serverRequestStream = Http3Api.Connection._streams[requestStream.StreamId];
        }

        Http3Api.TriggerTick();
        Http3Api.AdvanceTime(limits.RequestHeadersTimeout);

        Assert.Equal(_serviceContext.TimeProvider.GetTimestamp(timestamp, limits.RequestHeadersTimeout), serverRequestStream.StreamTimeoutTimestamp);

        await requestStream.SendHeadersAsync(headers).DefaultTimeout();

        await requestStream.OnHeaderReceivedTask.DefaultTimeout();

        Http3Api.AdvanceTime(TimeSpan.FromTicks(1));

        await requestStream.SendDataAsync(Memory<byte>.Empty, endStream: true);

        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task ControlStream_HeaderNotReceivedWithinRequestHeadersTimeout_StreamError_PendingStreamsEnabled()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var timeProvider = _serviceContext.FakeTimeProvider;
        var timestamp = timeProvider.GetTimestamp();
        var limits = _serviceContext.ServerOptions.Limits;

        await Http3Api.InitializeConnectionAsync(_noopApplication).DefaultTimeout();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        var outboundControlStream = await Http3Api.CreateControlStream(id: null);

        await outboundControlStream.OnUnidentifiedStreamCreatedTask.DefaultTimeout();
        var serverInboundControlStream = Http3Api.Connection._unidentifiedStreams[outboundControlStream.StreamId];

        Http3Api.TriggerTick();
        Http3Api.AdvanceTime(limits.RequestHeadersTimeout);

        Assert.Equal(timeProvider.GetTimestamp(timestamp, limits.RequestHeadersTimeout), serverInboundControlStream.StreamTimeoutTimestamp);

        Http3Api.AdvanceTime(TimeSpan.FromTicks(1));
    }

    [Fact]
    public async Task ControlStream_HeaderNotReceivedWithinRequestHeadersTimeout_StreamError()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = false;

        var timeProvider = _serviceContext.FakeTimeProvider;
        var timestamp = timeProvider.GetTimestamp();
        Http3Api._timeoutControl.Initialize();
        var limits = _serviceContext.ServerOptions.Limits;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        await Http3Api.InitializeConnectionAsync(_noopApplication).DefaultTimeout();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        var outboundControlStream = await Http3Api.CreateControlStream(id: null);

        await outboundControlStream.OnStreamCreatedTask.DefaultTimeout();

        var serverInboundControlStream = Http3Api.Connection._streams[outboundControlStream.StreamId];

        Http3Api.TriggerTick();
        Http3Api.TriggerTick(limits.RequestHeadersTimeout);

        Assert.Equal(timeProvider.GetTimestamp(timestamp, limits.RequestHeadersTimeout), serverInboundControlStream.StreamTimeoutTimestamp);

        Http3Api.TriggerTick(TimeSpan.FromTicks(1));

        await outboundControlStream.WaitForStreamErrorAsync(
            Http3ErrorCode.StreamCreationError,
            AssertExpectedErrorMessages,
            CoreStrings.Http3ControlStreamHeaderTimeout);
    }

    [Fact]
    public async Task ControlStream_HeaderReceivedWithinRequestHeadersTimeout_StreamError()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        await Http3Api.InitializeConnectionAsync(_noopApplication).DefaultTimeout();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        Http3Api.TriggerTick();
        Http3Api.TriggerTick(limits.RequestHeadersTimeout + TimeSpan.FromTicks(1));

        var outboundControlStream = await Http3Api.CreateControlStream(id: 0);

        await outboundControlStream.OnStreamCreatedTask.DefaultTimeout();

        Http3Api.TriggerTick();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ControlStream_RequestHeadersTimeoutMaxValue_ExpirationIsMaxValue(bool pendingStreamEnabled)
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = pendingStreamEnabled;

        var timeProvider = _serviceContext.FakeTimeProvider;
        var limits = _serviceContext.ServerOptions.Limits;
        limits.RequestHeadersTimeout = TimeSpan.MaxValue;

        await Http3Api.InitializeConnectionAsync(_noopApplication).DefaultTimeout();

        var controlStream = await Http3Api.GetInboundControlStream().DefaultTimeout();
        await controlStream.ExpectSettingsAsync().DefaultTimeout();

        var outboundControlStream = await Http3Api.CreateControlStream(id: null);

        dynamic serverInboundControlStream;
        if (pendingStreamEnabled)
        {
            await outboundControlStream.OnUnidentifiedStreamCreatedTask.DefaultTimeout();
            serverInboundControlStream = Http3Api.Connection._unidentifiedStreams[outboundControlStream.StreamId];
        }
        else
        {
            await outboundControlStream.OnStreamCreatedTask.DefaultTimeout();
            serverInboundControlStream = Http3Api.Connection._streams[outboundControlStream.StreamId];
        }

        Http3Api.TriggerTick();

        Assert.Equal(TimeSpan.MaxValue.ToTicks(timeProvider), serverInboundControlStream.StreamTimeoutTimestamp);
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnSmallRead_AbortsConnectionAfterGracePeriod()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        Http3Api._timeoutControl.Initialize();

        await Http3Api.InitializeConnectionAsync(_readRateApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        // _helloWorldBytes is 12 bytes, and 12 bytes / 240 bytes/sec = .05 secs which is far below the grace period.
        var requestStream = await Http3Api.CreateRequestStream(ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
        await requestStream.SendDataAsync(_helloWorldBytes, endStream: false);

        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectDataAsync();

        // Don't send any more data and advance just to and then past the grace period.
        Http3Api.AdvanceTime(limits.MinRequestBodyDataRate.GracePeriod);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        Http3Api.AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await Http3Api.WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 4,
            Http3ErrorCode.InternalError,
            null);
        MetricsAssert.Equal(ConnectionEndReason.MinRequestBodyDataRate, Http3Api.ConnectionTags);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResponseDrain_SlowerThanMinimumDataRate_AbortsConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await Http3Api.InitializeConnectionAsync(_noopApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        }, null, true, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

        await requestStream.OnDisposingTask.DefaultTimeout();

        Http3Api.TriggerTick();
        Assert.Null(requestStream.StreamContext._error);

        Http3Api.TriggerTick(TimeSpan.FromTicks(1));
        Assert.Null(requestStream.StreamContext._error);

        Http3Api.TriggerTick(limits.MinResponseDataRate.GracePeriod);

        requestStream.StartStreamDisposeTcs.TrySetResult();

        await Http3Api.WaitForConnectionErrorAsync<Http3ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 4,
            Http3ErrorCode.InternalError,
            matchExpectedErrorMessage: AssertExpectedErrorMessages,
            expectedErrorMessage: CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied);
        MetricsAssert.Equal(ConnectionEndReason.MinResponseDataRate, Http3Api.ConnectionTags);

        Assert.Contains(TestSink.Writes, w => w.EventId.Name == "ResponseMinimumDataRateNotSatisfied");
    }

    private class EchoAppWithNotification
    {
        private readonly TaskCompletionSource _writeStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WriteStartedTask => _writeStartedTcs.Task;

        public async Task RunApp(HttpContext context)
        {
            await context.Response.Body.FlushAsync();

            var buffer = new byte[16 * 1024];
            int received;

            while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var writeTask = context.Response.Body.WriteAsync(buffer, 0, received);
                _writeStartedTcs.TrySetResult();

                await writeTask;
            }
        }
    }

    [Fact]
    public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnSmallWrite_AbortsConnectionAfterGracePeriod()
    {
        var fakeTimeProvider = _serviceContext.FakeTimeProvider;
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        // Disable response buffering so "socket" backpressure is observed immediately.
        limits.MaxResponseBufferSize = 0;

        Http3Api._timeoutControl.Initialize();

        var app = new EchoAppWithNotification();
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(app.RunApp, _browserRequestHeaders, endStream: false);
        await requestStream.SendDataAsync(_helloWorldBytes, endStream: true);

        await requestStream.ExpectHeadersAsync();

        await app.WriteStartedTask.DefaultTimeout();

        // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
        Http3Api._timeoutControl.Tick(fakeTimeProvider.GetTimestamp());

        // Don't read data frame to induce "socket" backpressure.
        Http3Api.AdvanceTime(TimeSpan.FromSeconds((requestStream.BytesReceived + _helloWorldBytes.Length) / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        Http3Api.AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        // The "hello, world" bytes are buffered from before the timeout, but not an END_STREAM data frame.
        var data = await requestStream.ExpectDataAsync();
        Assert.Equal(_helloWorldBytes.Length, data.Length);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnLargeWrite_AbortsConnectionAfterRateTimeout()
    {
        var fakeTimeProvider = _serviceContext.FakeTimeProvider;
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        // Disable response buffering so "socket" backpressure is observed immediately.
        limits.MaxResponseBufferSize = 0;

        Http3Api._timeoutControl.Initialize();

        var app = new EchoAppWithNotification();
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(app.RunApp, _browserRequestHeaders, endStream: false);
        await requestStream.SendDataAsync(_maxData, endStream: true);

        await requestStream.ExpectHeadersAsync();

        await app.WriteStartedTask.DefaultTimeout();

        // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
        Http3Api._timeoutControl.Tick(fakeTimeProvider.GetTimestamp());

        var timeToWriteMaxData = TimeSpan.FromSeconds((requestStream.BytesReceived + _maxData.Length) / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

        // Don't read data frame to induce "socket" backpressure.
        Http3Api.AdvanceTime(timeToWriteMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        Http3Api.AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        // The _maxData bytes are buffered from before the timeout, but not an END_STREAM data frame.
        await requestStream.ExpectDataAsync();

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnLargeRead_AbortsConnectionAfterRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        Http3Api._timeoutControl.Initialize();

        await Http3Api.InitializeConnectionAsync(_readRateApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
        var requestStream = await Http3Api.CreateRequestStream(ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await requestStream.SendDataAsync(_maxData, endStream: false);

        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectDataAsync();

        // Due to the imprecision of floating point math and the fact that TimeoutControl derives rate from elapsed
        // time for reads instead of vice versa like for writes, use a half-second instead of single-tick cushion.
        var timeToReadMaxData = TimeSpan.FromSeconds(_maxData.Length / limits.MinRequestBodyDataRate.BytesPerSecond) - TimeSpan.FromSeconds(.5);

        // Don't send any more data and advance just to and then past the rate timeout.
        Http3Api.AdvanceTime(timeToReadMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        Http3Api.AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await Http3Api.WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: null,
            Http3ErrorCode.InternalError,
            null);
        MetricsAssert.Equal(ConnectionEndReason.MinRequestBodyDataRate, Http3Api.ConnectionTags);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnMultipleStreams_AbortsConnectionAfterAdditiveRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        Http3Api._timeoutControl.Initialize();

        await Http3Api.InitializeConnectionAsync(_readRateApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
        var requestStream1 = await Http3Api.CreateRequestStream(ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await requestStream1.SendDataAsync(_maxData, endStream: false);

        await requestStream1.ExpectHeadersAsync();
        await requestStream1.ExpectDataAsync();

        var requestStream2 = await Http3Api.CreateRequestStream(ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await requestStream2.SendDataAsync(_maxData, endStream: false);

        await requestStream2.ExpectHeadersAsync();
        await requestStream2.ExpectDataAsync();

        var timeToReadMaxData = TimeSpan.FromSeconds(_maxData.Length / limits.MinRequestBodyDataRate.BytesPerSecond);
        // Double the timeout for the second stream.
        timeToReadMaxData += timeToReadMaxData;

        // Due to the imprecision of floating point math and the fact that TimeoutControl derives rate from elapsed
        // time for reads instead of vice versa like for writes, use a half-second instead of single-tick cushion.
        timeToReadMaxData -= TimeSpan.FromSeconds(.5);

        // Don't send any more data and advance just to and then past the rate timeout.
        Http3Api.AdvanceTime(timeToReadMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        Http3Api.AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await Http3Api.WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: null,
            Http3ErrorCode.InternalError,
            null);
        MetricsAssert.Equal(ConnectionEndReason.MinRequestBodyDataRate, Http3Api.ConnectionTags);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnSecondStream_AbortsConnectionAfterNonAdditiveRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        Http3Api._timeoutControl.Initialize();

        await Http3Api.InitializeConnectionAsync(_readRateApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        Logger.LogInformation("Sending first request");

        // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
        var requestStream1 = await Http3Api.CreateRequestStream(ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await requestStream1.SendDataAsync(_maxData, endStream: true);

        await requestStream1.ExpectHeadersAsync();
        await requestStream1.ExpectDataAsync();

        await requestStream1.ExpectReceiveEndOfStream();

        Logger.LogInformation("Sending second request");
        var requestStream2 = await Http3Api.CreateRequestStream(ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await requestStream2.SendDataAsync(_maxData, endStream: false);

        await requestStream2.ExpectHeadersAsync();
        await requestStream2.ExpectDataAsync();

        // Due to the imprecision of floating point math and the fact that TimeoutControl derives rate from elapsed
        // time for reads instead of vice versa like for writes, use a half-second instead of single-tick cushion.
        var timeToReadMaxData = TimeSpan.FromSeconds(_maxData.Length / limits.MinRequestBodyDataRate.BytesPerSecond) - TimeSpan.FromSeconds(.5);

        // Don't send any more data and advance just to and then past the rate timeout.
        Http3Api.AdvanceTime(timeToReadMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        Http3Api.AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await Http3Api.WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: null,
            Http3ErrorCode.InternalError,
            null);
        MetricsAssert.Equal(ConnectionEndReason.MinRequestBodyDataRate, Http3Api.ConnectionTags);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_SlowlyWhenRateLimitDisabledPerRequest_DoesNotAbortConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        Http3Api._timeoutControl.Initialize();

        await Http3Api.InitializeConnectionAsync(context =>
        {
            // Completely disable rate limiting for this stream.
            context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = null;
            return _readRateApplication(context);
        });

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        Http3Api.OutboundControlStream = await Http3Api.CreateControlStream();

        // _helloWorldBytes is 12 bytes, and 12 bytes / 240 bytes/sec = .05 secs which is far below the grace period.
        var requestStream = await Http3Api.CreateRequestStream(ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
        await requestStream.SendDataAsync(_helloWorldBytes, endStream: false);

        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectDataAsync();

        // Don't send any more data and advance just to and then past the grace period.
        Http3Api.AdvanceTime(limits.MinRequestBodyDataRate.GracePeriod);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        Http3Api.AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        await requestStream.SendDataAsync(_helloWorldBytes, endStream: true);

        await requestStream.ExpectReceiveEndOfStream();

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }
}
