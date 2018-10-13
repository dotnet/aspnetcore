// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2TimeoutTests : Http2TestBase
    {
        [Fact]
        public async Task HEADERS_NotReceivedInitially_WithinKeepAliveTimeout_ClosesConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_noopApplication);

            mockSystemClock.UtcNow += limits.KeepAliveTimeout + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

            await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_NotReceivedAfterFirstRequest_WithinKeepAliveTimeout_ClosesConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_noopApplication);

            mockSystemClock.UtcNow += limits.KeepAliveTimeout + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

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
                withLength: 55,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 0,
                withFlags: (byte)Http2DataFrameFlags.END_STREAM,
                withStreamId: 1);

            await setTimeoutTcs.Task.DefaultTimeout();

            mockSystemClock.UtcNow += limits.KeepAliveTimeout + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.KeepAlive), Times.Once);

            await WaitForConnectionStopAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task HEADERS_ReceivedWithoutAllCONTINUATIONs_WithinRequestHeadersTimeout_AbortsConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.NONE);

            mockSystemClock.UtcNow += limits.RequestHeadersTimeout + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.NONE);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.RequestHeaders), Times.Once);

            await WaitForConnectionErrorAsync<BadHttpRequestException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                Http2ErrorCode.INTERNAL_ERROR,
                CoreStrings.BadRequest_RequestHeadersTimeout);

            _mockConnectionContext.Verify(c =>c.Abort(It.Is<ConnectionAbortedException>(e => 
                e.Message == CoreStrings.BadRequest_RequestHeadersTimeout)), Times.Once);
        }

        [Fact]
        public async Task ResponseDrain_SlowerThanMinimumDataRate_AbortsConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_noopApplication);

            await SendGoAwayAsync();

            await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

            mockSystemClock.UtcNow +=
                TimeSpan.FromSeconds(limits.MaxResponseBufferSize.Value * 2 / limits.MinResponseDataRate.BytesPerSecond) +
                Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);
            _mockConnectionContext.Verify(c => c.Abort(It.IsAny<ConnectionAbortedException>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            _mockConnectionContext.Verify(c =>c.Abort(It.Is<ConnectionAbortedException>(e => 
                e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);
        }

        [Theory]
        [InlineData(Http2FrameType.DATA)]
        [InlineData(Http2FrameType.HEADERS)]
        public async Task AbortedStream_ResetsAndDrainsRequest_RefusesFramesAfterCooldownExpires(Http2FrameType finalFrameType)
        {
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

            // There's a race when the appfunc is exiting about how soon it unregisters the stream.
            for (var i = 0; i < 10; i++)
            {
                await SendDataAsync(1, new byte[100], endStream: false);
            }

            // Just short of the timeout
            mockSystemClock.UtcNow += Constants.RequestBodyDrainTimeout;
            (_connection as IRequestProcessor).Tick(mockSystemClock.UtcNow);

            // Still fine
            await SendDataAsync(1, new byte[100], endStream: false);

            // Just past the timeout
            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            (_connection as IRequestProcessor).Tick(mockSystemClock.UtcNow);

            // Send an extra frame to make it fail
            switch (finalFrameType)
            {
                case Http2FrameType.DATA:
                    await SendDataAsync(1, new byte[100], endStream: true);
                    break;

                case Http2FrameType.HEADERS:
                    await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);
                    break;

                default:
                    throw new NotImplementedException(finalFrameType.ToString());
            }

            await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(ignoreNonGoAwayFrames: false, expectedLastStreamId: 1, Http2ErrorCode.STREAM_CLOSED,
                CoreStrings.FormatHttp2ErrorStreamClosed(finalFrameType, 1));
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnSmallWrite_AbortsConnectionAfterGracePeriod()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Disable response buffering so "socket" backpressure is observed immediately.
            limits.MaxResponseBufferSize = 0;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_echoApplication);
            
            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            // Don't read data frame to induce "socket" backpressure.
            mockSystemClock.UtcNow += limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            // The "hello, world" bytes are buffered from before the timeout, but not an END_STREAM data frame.
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _helloWorldBytes.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c =>c.Abort(It.Is<ConnectionAbortedException>(e => 
                e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnLargeWrite_AbortsConnectionAfterRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Disable response buffering so "socket" backpressure is observed immediately.
            limits.MaxResponseBufferSize = 0;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);

            var timeToWriteMaxData = TimeSpan.FromSeconds(_maxData.Length / limits.MinResponseDataRate.BytesPerSecond);

            // Don't read data frame to induce "socket" backpressure.
            mockSystemClock.UtcNow += timeToWriteMaxData + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            // The "hello, world" bytes are buffered from before the timeout, but not an END_STREAM data frame.
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToFlowControlOnSmallWrite_AbortsConnectionAfterGracePeriod()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
            _clientSettings.InitialWindowSize = 6;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _helloWorldBytes, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            // Don't send WINDOW_UPDATE to induce flow-control backpressure
            mockSystemClock.UtcNow += limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToOutputFlowControlOnLargeWrite_AbortsConnectionAfterRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
            _clientSettings.InitialWindowSize = (uint)_maxData.Length - 1;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            var timeToWriteMaxData = TimeSpan.FromSeconds(_clientSettings.InitialWindowSize / limits.MinResponseDataRate.BytesPerSecond);

            // Don't send WINDOW_UPDATE to induce flow-control backpressure
            mockSystemClock.UtcNow += timeToWriteMaxData + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 1,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToOutputFlowControlOnMultipleStreams_AbortsConnectionAfterAdditiveRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
            _clientSettings.InitialWindowSize = (uint)_maxData.Length - 1;

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_echoApplication);

            await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
            await SendDataAsync(1, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 1);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);

            await StartStreamAsync(3, _browserRequestHeaders, endStream: false);
            await SendDataAsync(3, _maxData, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 37,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: 3);
            await ExpectAsync(Http2FrameType.DATA,
                withLength: (int)_clientSettings.InitialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 3);

            var timeToWriteMaxData = TimeSpan.FromSeconds(_clientSettings.InitialWindowSize / limits.MinResponseDataRate.BytesPerSecond);
            // Double the timeout for the second stream.
            timeToWriteMaxData += timeToWriteMaxData;

            // Don't send WINDOW_UPDATE to induce flow-control backpressure
            mockSystemClock.UtcNow += timeToWriteMaxData + Heartbeat.Interval;
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            mockSystemClock.UtcNow += TimeSpan.FromTicks(1);
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 3,
                Http2ErrorCode.INTERNAL_ERROR,
                null);

            _mockConnectionContext.Verify(c => c.Abort(It.Is<ConnectionAbortedException>(e =>
                 e.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)), Times.Once);
        }
    }
}
