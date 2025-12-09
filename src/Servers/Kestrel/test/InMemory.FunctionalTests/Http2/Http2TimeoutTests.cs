// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2TimeoutTests : Http2TestBase
{
    [Fact]
    public async Task Preamble_NotReceivedInitially_WithinKeepAliveTimeout_ClosesConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        CreateConnection();

        _connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(_noopApplication));

        AdvanceTime(limits.KeepAliveTimeout + Heartbeat.Interval);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

        await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HEADERS_NotReceivedInitially_WithinKeepAliveTimeout_ClosesConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        await InitializeConnectionAsync(_noopApplication);

        AdvanceTime(limits.KeepAliveTimeout + Heartbeat.Interval);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

        await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HEADERS_NotReceivedAfterFirstRequest_WithinKeepAliveTimeout_ClosesConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        await InitializeConnectionAsync(_noopApplication);

        AdvanceTime(limits.KeepAliveTimeout + Heartbeat.Interval);

        // keep-alive timeout set but not fired.
        _mockTimeoutControl.Verify(c => c.SetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive), Times.Once);
        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // The KeepAlive timeout is set when the stream completes processing on a background thread, so we need to hook the
        // keep-alive set afterwards to make a reliable test.
        var setTimeoutTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockTimeoutControl.Setup(c => c.SetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive)).Callback<TimeSpan, TimeoutReason>((t, r) =>
        {
            _timeoutControl.SetTimeout(t, r);
            setTimeoutTcs.SetResult();
        });

        // Send continuation frame to verify intermediate request header timeout doesn't interfere with keep-alive timeout.
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);
        await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.END_HEADERS);

        _mockTimeoutControl.Verify(c => c.SetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.RequestHeaders), Times.Once);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await setTimeoutTcs.Task.DefaultTimeout();

        AdvanceTime(limits.KeepAliveTimeout + Heartbeat.Interval);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

        await WaitForConnectionStopAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionEndReason(ConnectionEndReason.KeepAliveTimeout);

        _mockTimeoutHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PING_WithinKeepAliveTimeout_ResetKeepAliveTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        CreateConnection();

        await InitializeConnectionAsync(_noopApplication);

        // Connection starts and sets keep alive timeout
        _mockTimeoutControl.Verify(c => c.SetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive), Times.Once);
        _mockTimeoutControl.Verify(c => c.ResetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive), Times.Never);

        await SendPingAsync(Http2PingFrameFlags.NONE);
        await ExpectAsync(Http2FrameType.PING,
            withLength: 8,
            withFlags: (byte)Http2PingFrameFlags.ACK,
            withStreamId: 0);

        // Server resets keep alive timeout
        _mockTimeoutControl.Verify(c => c.ResetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive), Times.Once);
    }

    [Fact]
    public async Task PING_NoKeepAliveTimeout_DoesNotResetKeepAliveTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        CreateConnection();

        await InitializeConnectionAsync(_echoApplication);

        // Connection starts and sets keep alive timeout
        _mockTimeoutControl.Verify(c => c.SetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive), Times.Once);
        _mockTimeoutControl.Verify(c => c.ResetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive), Times.Never);
        _mockTimeoutControl.Verify(c => c.CancelTimeout(), Times.Never);

        // Stream will stay open because it is waiting for request body to end
        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // Starting a stream cancels the keep alive timeout
        _mockTimeoutControl.Verify(c => c.CancelTimeout(), Times.Once);

        await SendPingAsync(Http2PingFrameFlags.NONE);
        await ExpectAsync(Http2FrameType.PING,
            withLength: 8,
            withFlags: (byte)Http2PingFrameFlags.ACK,
            withStreamId: 0);

        // Server doesn't reset keep alive timeout because it isn't running
        _mockTimeoutControl.Verify(c => c.ResetTimeout(It.IsAny<TimeSpan>(), TimeoutReason.KeepAlive), Times.Never);

        // End stream
        await SendDataAsync(1, _helloWorldBytes, endStream: true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: _helloWorldBytes.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
    }

    [Fact]
    public async Task HEADERS_ReceivedWithoutAllCONTINUATIONs_WithinRequestHeadersTimeout_AbortsConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

        await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.NONE);

        AdvanceTime(limits.RequestHeadersTimeout + Heartbeat.Interval);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.NONE);

        AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.RequestHeaders), Times.Once);

        await WaitForConnectionErrorAsync<Microsoft.AspNetCore.Http.BadHttpRequestException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            CoreStrings.BadRequest_RequestHeadersTimeout);
        AssertConnectionEndReason(ConnectionEndReason.RequestHeadersTimeout);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.BadRequest_RequestHeadersTimeout)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResponseDrain_SlowerThanMinimumDataRate_AbortsConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        await InitializeConnectionAsync(_noopApplication);

        await SendGoAwayAsync();

        await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        AdvanceTime(TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);
        _mockConnectionContext.Verify(c => c.Abort(It.IsAny<ConnectionAbortedException>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();

        Assert.Contains(TestSink.Writes, w => w.EventId.Name == "ResponseMinimumDataRateNotSatisfied");
    }

    [Theory]
    [InlineData((int)Http2FrameType.DATA)]
    [InlineData((int)Http2FrameType.CONTINUATION)]
    public async Task AbortedStream_ResetsAndDrainsRequest_RefusesFramesAfterCooldownExpires(int intFinalFrameType)
    {
        var closeLock = new object();
        var closed = false;
        var finalFrameType = (Http2FrameType)intFinalFrameType;
        // Remove callback that completes _pair.Application.Output on abort.
        _mockConnectionContext.Reset();

        var timeProvider = _serviceContext.FakeTimeProvider;

        var headers = new[]
        {
                new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
                new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            };
        await InitializeConnectionAsync(_appAbort);

        await StartStreamAsync(1, headers, endStream: false);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "The connection was aborted by the application.");

        async Task AdvanceClockAndSendFrames()
        {
            if (finalFrameType == Http2FrameType.CONTINUATION)
            {
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, new byte[0]);
                await SendContinuationAsync(1, Http2ContinuationFrameFlags.NONE, new byte[0]);
            }

            // There's a race when the appfunc is exiting about how soon it unregisters the stream, so retry until success.
            while (!closed)
            {
                // Just past the timeout
                timeProvider.Advance(Constants.RequestBodyDrainTimeout + TimeSpan.FromTicks(1));

                // Send an extra frame to make it fail
                switch (finalFrameType)
                {
                    case Http2FrameType.DATA:
                        await SendDataAsync(1, new byte[100], endStream: false);
                        break;

                    case Http2FrameType.CONTINUATION:
                        await SendContinuationAsync(1, Http2ContinuationFrameFlags.NONE, new byte[0]);
                        break;

                    default:
                        throw new NotImplementedException(finalFrameType.ToString());
                }

                // TODO how do I force a function to go async?
                await Task.Delay(1);
            }
        }

        var sendTask = AdvanceClockAndSendFrames();

        await WaitForConnectionErrorAsyncDoNotCloseTransport<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            Http2ErrorCode.STREAM_CLOSED,
            CoreStrings.FormatHttp2ErrorStreamClosed(finalFrameType, 1));

        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                AssertConnectionEndReason(ConnectionEndReason.UnknownStream);
                break;

            case Http2FrameType.CONTINUATION:
                AssertConnectionEndReason(ConnectionEndReason.FrameAfterStreamClose);
                break;

            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }

        closed = true;

        await sendTask.DefaultTimeout();

        _pair.Application.Output.Complete();
    }

    private class EchoAppWithNotification
    {
        private readonly TaskCompletionSource _writeStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WriteStartedTask => _writeStartedTcs.Task;

        public async Task RunApp(HttpContext context)
        {

            var buffer = new byte[Http2PeerSettings.MinAllowedMaxFrameSize];
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
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        // Disable response buffering so "socket" backpressure is observed immediately.
        limits.MaxResponseBufferSize = 0;

        var app = new EchoAppWithNotification();
        await InitializeConnectionAsync(app.RunApp);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await app.WriteStartedTask.DefaultTimeout();

        // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
        TriggerTick();

        // Don't read data frame to induce "socket" backpressure.
        AdvanceTime(TimeSpan.FromSeconds((_bytesReceived + _helloWorldBytes.Length) / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        // The "hello, world" bytes are buffered from before the timeout, but not an END_STREAM data frame.
        await ExpectAsync(Http2FrameType.DATA,
            withLength: _helloWorldBytes.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        Assert.True((await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout()).IsCompleted);
        AssertConnectionEndReason(ConnectionEndReason.MinResponseDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnLargeWrite_AbortsConnectionAfterRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        // Disable response buffering so "socket" backpressure is observed immediately.
        limits.MaxResponseBufferSize = 0;

        var app = new EchoAppWithNotification();
        await InitializeConnectionAsync(app.RunApp);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _maxData, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await app.WriteStartedTask.DefaultTimeout();

        // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
        TriggerTick();

        var timeToWriteMaxData = TimeSpan.FromSeconds((_bytesReceived + _maxData.Length) / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

        // Don't read data frame to induce "socket" backpressure.
        AdvanceTime(timeToWriteMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        // The _maxData bytes are buffered from before the timeout, but not an END_STREAM data frame.
        await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        Assert.True((await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout()).IsCompleted);
        AssertConnectionEndReason(ConnectionEndReason.MinResponseDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Sent_TooSlowlyDueToFlowControlOnSmallWrite_AbortsConnectionAfterGracePeriod()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = 6;

        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: (int)_clientSettings.InitialWindowSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
        TriggerTick();

        // Don't send WINDOW_UPDATE to induce flow-control backpressure
        AdvanceTime(TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinResponseDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Sent_TooSlowlyDueToOutputFlowControlOnLargeWrite_AbortsConnectionAfterRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = (uint)_maxData.Length - 1;

        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _maxData, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: (int)_clientSettings.InitialWindowSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
        TriggerTick();

        var timeToWriteMaxData = TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

        // Don't send WINDOW_UPDATE to induce flow-control backpressure
        AdvanceTime(timeToWriteMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinResponseDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Sent_TooSlowlyDueToOutputFlowControlOnMultipleStreams_AbortsConnectionAfterAdditiveRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = (uint)_maxData.Length - 1;

        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _maxData, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: (int)_clientSettings.InitialWindowSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);
        await SendDataAsync(3, _maxData, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: (int)_clientSettings.InitialWindowSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);

        // Complete timing of the request bodies so we don't induce any unexpected request body rate timeouts.
        TriggerTick();

        var timeToWriteMaxData = TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
            limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

        // Don't send WINDOW_UPDATE to induce flow-control backpressure
        AdvanceTime(timeToWriteMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinResponseDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnSmallRead_AbortsConnectionAfterGracePeriod()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await InitializeConnectionAsync(_readRateApplication);

        // _helloWorldBytes is 12 bytes, and 12 bytes / 240 bytes/sec = .05 secs which is far below the grace period.
        await StartStreamAsync(1, ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
        await SendDataAsync(1, _helloWorldBytes, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Don't send any more data and advance just to and then past the grace period.
        AdvanceTime(limits.MinRequestBodyDataRate.GracePeriod);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinRequestBodyDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnLargeRead_AbortsConnectionAfterRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await InitializeConnectionAsync(_readRateApplication);

        // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
        await StartStreamAsync(1, ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await SendDataAsync(1, _maxData, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Due to the imprecision of floating point math and the fact that TimeoutControl derives rate from elapsed
        // time for reads instead of vice versa like for writes, use a half-second instead of single-tick cushion.
        var timeToReadMaxData = TimeSpan.FromSeconds(_maxData.Length / limits.MinRequestBodyDataRate.BytesPerSecond) - TimeSpan.FromSeconds(.5);

        // Don't send any more data and advance just to and then past the rate timeout.
        AdvanceTime(timeToReadMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinRequestBodyDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnMultipleStreams_AbortsConnectionAfterAdditiveRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await InitializeConnectionAsync(_readRateApplication);

        // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
        await StartStreamAsync(1, ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await SendDataAsync(1, _maxData, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await StartStreamAsync(3, ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await SendDataAsync(3, _maxData, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);

        var timeToReadMaxData = TimeSpan.FromSeconds(_maxData.Length / limits.MinRequestBodyDataRate.BytesPerSecond);
        // Double the timeout for the second stream.
        timeToReadMaxData += timeToReadMaxData;

        // Due to the imprecision of floating point math and the fact that TimeoutControl derives rate from elapsed
        // time for reads instead of vice versa like for writes, use a half-second instead of single-tick cushion.
        timeToReadMaxData -= TimeSpan.FromSeconds(.5);

        // Don't send any more data and advance just to and then past the rate timeout.
        AdvanceTime(timeToReadMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinRequestBodyDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_TooSlowlyOnSecondStream_AbortsConnectionAfterNonAdditiveRateTimeout()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await InitializeConnectionAsync(_readRateApplication);

        // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
        await StartStreamAsync(1, ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await SendDataAsync(1, _maxData, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StartStreamAsync(3, ReadRateRequestHeaders(_maxData.Length), endStream: false);
        await SendDataAsync(3, _maxData, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);

        // Due to the imprecision of floating point math and the fact that TimeoutControl derives rate from elapsed
        // time for reads instead of vice versa like for writes, use a half-second instead of single-tick cushion.
        var timeToReadMaxData = TimeSpan.FromSeconds(_maxData.Length / limits.MinRequestBodyDataRate.BytesPerSecond) - TimeSpan.FromSeconds(.5);

        // Don't send any more data and advance just to and then past the rate timeout.
        AdvanceTime(timeToReadMaxData);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromSeconds(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinRequestBodyDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_SlowlyWhenRateLimitDisabledPerRequest_DoesNotAbortConnection()
    {
        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await InitializeConnectionAsync(context =>
        {
            // Completely disable rate limiting for this stream.
            context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = null;
            return _readRateApplication(context);
        });

        // _helloWorldBytes is 12 bytes, and 12 bytes / 240 bytes/sec = .05 secs which is far below the grace period.
        await StartStreamAsync(1, ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
        await SendDataAsync(1, _helloWorldBytes, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Don't send any more data and advance just to and then past the grace period.
        AdvanceTime(limits.MinRequestBodyDataRate.GracePeriod);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DATA_Received_SlowlyDueToConnectionFlowControl_DoesNotAbortConnection()
    {
        var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesConnectionInWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        var backpressureTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var limits = _serviceContext.ServerOptions.Limits;

        // Use non-default value to ensure the min request and response rates aren't mixed up.
        limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

        await InitializeConnectionAsync(async context =>
        {
            var streamId = context.Features.Get<IHttp2StreamIdFeature>().StreamId;

            if (streamId == 1)
            {
                await backpressureTcs.Task;
            }
            else
            {
                await _readRateApplication(context);
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        for (var i = 0; i < framesConnectionInWindow / 2; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }
        await SendDataAsync(1, _maxData, endStream: true);

        await StartStreamAsync(3, ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
        await SendDataAsync(3, _helloWorldBytes, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);

        // No matter how much time elapses there is no read timeout because the connection window is too small.
        AdvanceTime(TimeSpan.FromDays(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        // Opening the connection window starts the read rate timeout enforcement after that point.
        backpressureTcs.SetResult();

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        var updateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 0);

        var expectedUpdateSize = ((framesConnectionInWindow / 2) + 1) * _maxData.Length + _helloWorldBytes.Length;
        Assert.Equal(expectedUpdateSize, updateFrame.WindowUpdateSizeIncrement);

        AdvanceTime(limits.MinRequestBodyDataRate.GracePeriod);

        _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

        AdvanceTime(TimeSpan.FromTicks(1));

        _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

        await WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: int.MaxValue,
            Http2ErrorCode.INTERNAL_ERROR,
            null);
        AssertConnectionEndReason(ConnectionEndReason.MinRequestBodyDataRate);

        _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
             e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

        _mockTimeoutHandler.VerifyNoOtherCalls();
        _mockConnectionContext.VerifyNoOtherCalls();
    }
}
