// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2TimeoutTests : Http2TestBase
    {
        [Fact]
        public async Task Preamble_NotReceivedInitially_WithinKeepAliveTimeout_ClosesConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            CreateConnection();

            _connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(_noopApplication));

            AdvanceClock(limits.KeepAliveTimeout + Heartbeat.Interval);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

            await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HEADERS_NotReceivedInitially_WithinKeepAliveTimeout_ClosesConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_noopApplication);

            AdvanceClock(limits.KeepAliveTimeout + Heartbeat.Interval);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

            await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HEADERS_NotReceivedAfterFirstRequest_WithinKeepAliveTimeout_ClosesConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_noopApplication);

            StartHeartbeat();

            AdvanceClock(limits.KeepAliveTimeout + Heartbeat.Interval);

            // keep-alive timeout set but not fired.
            _mockTimeoutControl.Verify(c => c.SetTimeout(It.IsAny<long>(), TimeoutReason.KeepAlive), Times.Once);
            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // The KeepAlive timeout is set when the stream completes processing on a background thread, so we need to hook the
            // keep-alive set afterwards to make a reliable test.
            var setTimeoutTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _mockTimeoutControl.Setup(c => c.SetTimeout(It.IsAny<long>(), TimeoutReason.KeepAlive)).Callback<long, TimeoutReason>((t, r) =>
            {
                _timeoutControl.SetTimeout(t, r);
                setTimeoutTcs.SetResult(null);
            });

            // Send continuation frame to verify intermediate request header timeout doesn't interfere with keep-alive timeout.
            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);
            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.END_HEADERS);

            _mockTimeoutControl.Verify(c => c.SetTimeout(It.IsAny<long>(), TimeoutReason.RequestHeaders), Times.Once);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
                withStreamId: 1);

            await setTimeoutTcs.Task.DefaultTimeout();

            AdvanceClock(limits.KeepAliveTimeout + Heartbeat.Interval);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

            await WaitForConnectionStopAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HEADERS_ReceivedWithoutAllCONTINUATIONs_WithinRequestHeadersTimeout_AbortsConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits; ;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.NONE);

            AdvanceClock(limits.RequestHeadersTimeout + Heartbeat.Interval);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.NONE);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.RequestHeaders), Times.Once);

            await WaitForConnectionErrorAsync<BadHttpRequestException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                CoreStrings.BadRequest_RequestHeadersTimeout);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.BadRequest_RequestHeadersTimeout)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ResponseDrain_SlowerThanMinimumDataRate_AbortsConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_noopApplication);

            await SendGoAwayAsync();

            await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

            AdvanceClock(TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);
            _mockConnectionContext.Verify(c => c.Abort(It.IsAny<ConnectionAbortedException>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
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

            var mockSystemClock = _serviceContext.MockSystemClock;

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "POST"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
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
                    mockSystemClock.UtcNow += Constants.RequestBodyDrainTimeout + TimeSpan.FromTicks(1);

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

            closed = true;

            await sendTask.DefaultTimeout();

            _pair.Application.Output.Complete();
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/1323")]
        public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnSmallWrite_AbortsConnectionAfterGracePeriod()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            // Disable response buffering so "socket" backpressure is observed immediately.
            limits.MaxResponseBufferSize = 0;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            // Don't read data frame to induce "socket" backpressure.
            AdvanceClock(TimeSpan.FromSeconds((_bytesReceived + _helloWorldBytes.Length) / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            // The "hello, world" bytes are buffered from before the timeout, but not an END_STREAM data frame.
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _helloWorldBytes.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            Assert.True((await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout()).IsCompleted);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        [QuarantinedTest]
        public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnLargeWrite_AbortsConnectionAfterRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            // Disable response buffering so "socket" backpressure is observed immediately.
            limits.MaxResponseBufferSize = 0;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            var timeToWriteMaxData = TimeSpan.FromSeconds((_bytesReceived + _maxData.Length) / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

            // Don't read data frame to induce "socket" backpressure.
            AdvanceClock(timeToWriteMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            // The _maxData bytes are buffered from before the timeout, but not an END_STREAM data frame.
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            Assert.True((await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout()).IsCompleted);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToFlowControlOnSmallWrite_AbortsConnectionAfterGracePeriod()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
            _clientSettings.InitialWindowSize = 6;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            // Don't send WINDOW_UPDATE to induce flow-control backpressure
            AdvanceClock(TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToOutputFlowControlOnLargeWrite_AbortsConnectionAfterRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
            _clientSettings.InitialWindowSize = (uint)_maxData.Length - 1;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            var timeToWriteMaxData = TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

            // Don't send WINDOW_UPDATE to induce flow-control backpressure
            AdvanceClock(timeToWriteMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore-internal/issues/2197")]
        public async Task DATA_Sent_TooSlowlyDueToOutputFlowControlOnMultipleStreams_AbortsConnectionAfterAdditiveRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
            _clientSettings.InitialWindowSize = (uint)_maxData.Length - 1;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await StartStreamAsync(3, _browserRequestHeaders, endStream: false);
            await SendDataAsync(3, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 3);

            // Complete timing of the request bodies so we don't induce any unexpected request body rate timeouts.
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            var timeToWriteMaxData = TimeSpan.FromSeconds(_bytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

            // Don't send WINDOW_UPDATE to induce flow-control backpressure
            AdvanceClock(timeToWriteMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_TooSlowlyOnSmallRead_AbortsConnectionAfterGracePeriod()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_readRateApplication);

            // _helloWorldBytes is 12 bytes, and 12 bytes / 240 bytes/sec = .05 secs which is far below the grace period.
            await StartStreamAsync(1, ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
            await SendDataAsync(1, _helloWorldBytes, endStream: false);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            await ExpectAsync(Http2FrameType.DATA,
                withLength: 1,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            // Don't send any more data and advance just to and then past the grace period.
            AdvanceClock(limits.MinRequestBodyDataRate.GracePeriod);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_TooSlowlyOnLargeRead_AbortsConnectionAfterRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_readRateApplication);

            // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
            await StartStreamAsync(1, ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await SendDataAsync(1, _maxData, endStream: false);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
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
            AdvanceClock(timeToReadMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_TooSlowlyOnMultipleStreams_AbortsConnectionAfterAdditiveRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_readRateApplication);

            // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
            await StartStreamAsync(1, ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await SendDataAsync(1, _maxData, endStream: false);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            await ExpectAsync(Http2FrameType.DATA,
                withLength: 1,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await StartStreamAsync(3, ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await SendDataAsync(3, _maxData, endStream: false);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
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
            AdvanceClock(timeToReadMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_TooSlowlyOnSecondStream_AbortsConnectionAfterNonAdditiveRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_readRateApplication);

            // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
            await StartStreamAsync(1, ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 33,
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
                withLength: 33,
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
            AdvanceClock(timeToReadMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_SlowlyWhenRateLimitDisabledPerRequest_DoesNotAbortConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

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
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            await ExpectAsync(Http2FrameType.DATA,
                withLength: 1,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            // Don't send any more data and advance just to and then past the grace period.
            AdvanceClock(limits.MinRequestBodyDataRate.GracePeriod);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_SlowlyDueToConnectionFlowControl_DoesNotAbortConnection()
        {
            var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
            var framesConnectionInWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

            var backpressureTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

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
                withLength: 33,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 1,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 3);

            // No matter how much time elapses there is no read timeout because the connection window is too small.
            AdvanceClock(TimeSpan.FromDays(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            // Opening the connection window starts the read rate timeout enforcement after that point.
            backpressureTcs.SetResult(null);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
                withStreamId: 1);

            var updateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
                withLength: 4,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 0);

            var expectedUpdateSize = ((framesConnectionInWindow / 2) + 1) * _maxData.Length + _helloWorldBytes.Length;
            Assert.Equal(expectedUpdateSize, updateFrame.WindowUpdateSizeIncrement);

            AdvanceClock(limits.MinRequestBodyDataRate.GracePeriod);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: int.MaxValue,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.BadRequest_RequestBodyTimeout)), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
            _mockConnectionContext.VerifyNoOtherCalls();
        }
    }
}
