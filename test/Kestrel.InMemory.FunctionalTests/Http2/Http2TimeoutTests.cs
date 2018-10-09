// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2TimeoutTests : Http2TestBase
    {
        [Fact]
        public async Task HEADERS_NotReceivedInitially_WithinKeepAliveTimeout_ClosesConnection()
        {
            var mockSystemClock = new MockSystemClock();
            var limits = _connectionContext.ServiceContext.ServerOptions.Limits;

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
            var mockSystemClock = new MockSystemClock();
            var limits = _connectionContext.ServiceContext.ServerOptions.Limits;

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
            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
            await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.END_HEADERS);
            await SendDataAsync(1, new Memory<byte>(), endStream: true);

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
            var mockSystemClock = new MockSystemClock();
            var limits = _connectionContext.ServiceContext.ServerOptions.Limits;

            _mockConnectionContext.Setup(c => c.Abort(It.IsAny<ConnectionAbortedException>())).Callback<ConnectionAbortedException>(ex =>
            {
                // Emulate transport abort so the _connectionTask completes.
                _pair.Application.Output.Complete(ex);
            });

            _timeoutControl.Initialize(mockSystemClock.UtcNow);

            await InitializeConnectionAsync(_noopApplication);

            await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);

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
            var mockSystemClock = new MockSystemClock();
            var limits = _connectionContext.ServiceContext.ServerOptions.Limits;

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
    }
}
