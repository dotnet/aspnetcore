// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3TimeoutTests : Http3TestBase
    {
        [Fact]
        public async Task HEADERS_IncompleteFrameReceivedWithinRequestHeadersTimeout_StreamError()
        {
            var now = _serviceContext.MockSystemClock.UtcNow;
            var limits = _serviceContext.ServerOptions.Limits;

            var requestStream = await InitializeConnectionAndStreamsAsync(_noopApplication).DefaultTimeout();

            var controlStream = await GetInboundControlStream().DefaultTimeout();
            await controlStream.ExpectSettingsAsync().DefaultTimeout();

            await requestStream.OnStreamCreatedTask.DefaultTimeout();

            var serverRequestStream = Connection._streams[requestStream.StreamId];

            await requestStream.SendHeadersPartialAsync().DefaultTimeout();

            TriggerTick(now);
            TriggerTick(now + limits.RequestHeadersTimeout);

            Assert.Equal((now + limits.RequestHeadersTimeout).Ticks, serverRequestStream.HeaderTimeoutTicks);

            TriggerTick(now + limits.RequestHeadersTimeout + TimeSpan.FromTicks(1));

            await requestStream.WaitForStreamErrorAsync(Http3ErrorCode.RequestRejected, CoreStrings.BadRequest_RequestHeadersTimeout);
        }

        [Fact]
        public async Task HEADERS_HeaderFrameReceivedWithinRequestHeadersTimeout_Success()
        {
            var now = _serviceContext.MockSystemClock.UtcNow;
            var limits = _serviceContext.ServerOptions.Limits;
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };

            var requestStream = await InitializeConnectionAndStreamsAsync(_noopApplication).DefaultTimeout();

            var controlStream = await GetInboundControlStream().DefaultTimeout();
            await controlStream.ExpectSettingsAsync().DefaultTimeout();

            await requestStream.OnStreamCreatedTask.DefaultTimeout();

            var serverRequestStream = Connection._streams[requestStream.StreamId];

            TriggerTick(now);
            TriggerTick(now + limits.RequestHeadersTimeout);

            Assert.Equal((now + limits.RequestHeadersTimeout).Ticks, serverRequestStream.HeaderTimeoutTicks);

            await requestStream.SendHeadersAsync(headers).DefaultTimeout();

            await requestStream.OnHeaderReceivedTask.DefaultTimeout();

            TriggerTick(now + limits.RequestHeadersTimeout + TimeSpan.FromTicks(1));

            await requestStream.SendDataAsync(Memory<byte>.Empty, endStream: true);

            await requestStream.ExpectHeadersAsync();

            await requestStream.ExpectReceiveEndOfStream();
        }

        [Fact]
        public async Task ControlStream_HeaderNotReceivedWithinRequestHeadersTimeout_StreamError()
        {
            var now = _serviceContext.MockSystemClock.UtcNow;
            var limits = _serviceContext.ServerOptions.Limits;
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };

            await InitializeConnectionAsync(_noopApplication).DefaultTimeout();

            var controlStream = await GetInboundControlStream().DefaultTimeout();
            await controlStream.ExpectSettingsAsync().DefaultTimeout();

            var outboundControlStream = await CreateControlStream(id: null);

            await outboundControlStream.OnStreamCreatedTask.DefaultTimeout();

            var serverInboundControlStream = Connection._streams[outboundControlStream.StreamId];

            TriggerTick(now);
            TriggerTick(now + limits.RequestHeadersTimeout);

            Assert.Equal((now + limits.RequestHeadersTimeout).Ticks, serverInboundControlStream.HeaderTimeoutTicks);

            TriggerTick(now + limits.RequestHeadersTimeout + TimeSpan.FromTicks(1));

            await outboundControlStream.WaitForStreamErrorAsync(Http3ErrorCode.StreamCreationError, CoreStrings.Http3ControlStreamHeaderTimeout);
        }

        [Fact]
        public async Task ControlStream_HeaderReceivedWithinRequestHeadersTimeout_StreamError()
        {
            var now = _serviceContext.MockSystemClock.UtcNow;
            var limits = _serviceContext.ServerOptions.Limits;
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };

            await InitializeConnectionAsync(_noopApplication).DefaultTimeout();

            var controlStream = await GetInboundControlStream().DefaultTimeout();
            await controlStream.ExpectSettingsAsync().DefaultTimeout();

            var outboundControlStream = await CreateControlStream(id: null);

            await outboundControlStream.OnStreamCreatedTask.DefaultTimeout();

            TriggerTick(now);
            TriggerTick(now + limits.RequestHeadersTimeout);

            await outboundControlStream.WriteStreamIdAsync(id: 0);

            await outboundControlStream.OnHeaderReceivedTask.DefaultTimeout();

            TriggerTick(now + limits.RequestHeadersTimeout + TimeSpan.FromTicks(1));
        }

        [Fact]
        public async Task ControlStream_RequestHeadersTimeoutMaxValue_ExpirationIsMaxValue()
        {
            var now = _serviceContext.MockSystemClock.UtcNow;
            var limits = _serviceContext.ServerOptions.Limits;
            limits.RequestHeadersTimeout = TimeSpan.MaxValue;

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };

            await InitializeConnectionAsync(_noopApplication).DefaultTimeout();

            var controlStream = await GetInboundControlStream().DefaultTimeout();
            await controlStream.ExpectSettingsAsync().DefaultTimeout();

            var outboundControlStream = await CreateControlStream(id: null);

            await outboundControlStream.OnStreamCreatedTask.DefaultTimeout();

            var serverInboundControlStream = Connection._streams[outboundControlStream.StreamId];

            TriggerTick(now);

            Assert.Equal(TimeSpan.MaxValue.Ticks, serverInboundControlStream.HeaderTimeoutTicks);
        }

        [Fact]
        public async Task DATA_Received_TooSlowlyOnSmallRead_AbortsConnectionAfterGracePeriod()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            var requestStream = await InitializeConnectionAndStreamsAsync(_readRateApplication);

            var inboundControlStream = await GetInboundControlStream();
            await inboundControlStream.ExpectSettingsAsync();

            // _helloWorldBytes is 12 bytes, and 12 bytes / 240 bytes/sec = .05 secs which is far below the grace period.
            await requestStream.SendHeadersAsync(ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
            await requestStream.SendDataAsync(_helloWorldBytes, endStream: false);

            await requestStream.ExpectHeadersAsync();

            await requestStream.ExpectDataAsync();

            // Don't send any more data and advance just to and then past the grace period.
            AdvanceClock(limits.MinRequestBodyDataRate.GracePeriod);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: 8,
                Http3ErrorCode.InternalError,
                null);

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

        /*
         * Additional work around closing connections is required before response drain can be supported.
        [Fact]
        public async Task ResponseDrain_SlowerThanMinimumDataRate_AbortsConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            await InitializeConnectionAsync(_noopApplication);

            var inboundControlStream = await GetInboundControlStream();
            await inboundControlStream.ExpectSettingsAsync();

            CloseConnectionGracefully();

            await inboundControlStream.ReceiveFrameAsync().DefaultTimeout();
            await inboundControlStream.ReceiveFrameAsync().DefaultTimeout();
            await inboundControlStream.ReceiveEndAsync().DefaultTimeout();

            //await WaitForConnectionStopAsync(expectedLastStreamId: VariableLengthIntegerHelper.EightByteLimit, ignoreNonGoAwayFrames: false, expectedErrorCode: Http3ErrorCode.NoError);

            AdvanceClock(TimeSpan.FromSeconds(inboundControlStream.BytesReceived / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }
        */

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
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            // Disable response buffering so "socket" backpressure is observed immediately.
            limits.MaxResponseBufferSize = 0;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            var app = new EchoAppWithNotification();
            var requestStream = await InitializeConnectionAndStreamsAsync(app.RunApp);

            await requestStream.SendHeadersAsync(_browserRequestHeaders, endStream: false);
            await requestStream.SendDataAsync(_helloWorldBytes, endStream: true);

            await requestStream.ExpectHeadersAsync();

            await app.WriteStartedTask.DefaultTimeout();

            // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            // Don't read data frame to induce "socket" backpressure.
            AdvanceClock(TimeSpan.FromSeconds((requestStream.BytesReceived + _helloWorldBytes.Length) / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            // The "hello, world" bytes are buffered from before the timeout, but not an END_STREAM data frame.
            var data = await requestStream.ExpectDataAsync();
            Assert.Equal(_helloWorldBytes.Length, data.Length);

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Sent_TooSlowlyDueToSocketBackPressureOnLargeWrite_AbortsConnectionAfterRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinResponseDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            // Disable response buffering so "socket" backpressure is observed immediately.
            limits.MaxResponseBufferSize = 0;

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            var app = new EchoAppWithNotification();
            var requestStream = await InitializeConnectionAndStreamsAsync(app.RunApp);

            await requestStream.SendHeadersAsync(_browserRequestHeaders, endStream: false);
            await requestStream.SendDataAsync(_maxData, endStream: true);

            await requestStream.ExpectHeadersAsync();

            await app.WriteStartedTask.DefaultTimeout();

            // Complete timing of the request body so we don't induce any unexpected request body rate timeouts.
            _timeoutControl.Tick(mockSystemClock.UtcNow);

            var timeToWriteMaxData = TimeSpan.FromSeconds((requestStream.BytesReceived + _maxData.Length) / limits.MinResponseDataRate.BytesPerSecond) +
                limits.MinResponseDataRate.GracePeriod + Heartbeat.Interval - TimeSpan.FromSeconds(.5);

            // Don't read data frame to induce "socket" backpressure.
            AdvanceClock(timeToWriteMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.WriteDataRate), Times.Once);

            // The _maxData bytes are buffered from before the timeout, but not an END_STREAM data frame.
            await requestStream.ExpectDataAsync();

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_TooSlowlyOnLargeRead_AbortsConnectionAfterRateTimeout()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            var requestStream = await InitializeConnectionAndStreamsAsync(_readRateApplication);

            var inboundControlStream = await GetInboundControlStream();
            await inboundControlStream.ExpectSettingsAsync();

            // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
            await requestStream.SendHeadersAsync(ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await requestStream.SendDataAsync(_maxData, endStream: false);

            await requestStream.ExpectHeadersAsync();

            await requestStream.ExpectDataAsync();

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
                expectedLastStreamId: null,
                Http3ErrorCode.InternalError,
                null);

            _mockTimeoutHandler.VerifyNoOtherCalls();
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

            var inboundControlStream = await GetInboundControlStream();
            await inboundControlStream.ExpectSettingsAsync();

            var requestStream1 = await CreateRequestStream();

            // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
            await requestStream1.SendHeadersAsync(ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await requestStream1.SendDataAsync(_maxData, endStream: false);

            await requestStream1.ExpectHeadersAsync();
            await requestStream1.ExpectDataAsync();

            var requestStream2 = await CreateRequestStream();

            await requestStream2.SendHeadersAsync(ReadRateRequestHeaders(_maxData.Length), endStream: false);
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
            AdvanceClock(timeToReadMaxData);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromSeconds(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(TimeoutReason.ReadDataRate), Times.Once);

            await WaitForConnectionErrorAsync<ConnectionAbortedException>(
                ignoreNonGoAwayFrames: false,
                expectedLastStreamId: null,
                Http3ErrorCode.InternalError,
                null);

            _mockTimeoutHandler.VerifyNoOtherCalls();
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

            var inboundControlStream = await GetInboundControlStream();
            await inboundControlStream.ExpectSettingsAsync();

            var requestStream1 = await CreateRequestStream();

            // _maxData is 16 KiB, and 16 KiB / 240 bytes/sec ~= 68 secs which is far above the grace period.
            await requestStream1.SendHeadersAsync(ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await requestStream1.SendDataAsync(_maxData, endStream: true);

            await requestStream1.ExpectHeadersAsync();
            await requestStream1.ExpectDataAsync();

            await requestStream1.ExpectReceiveEndOfStream();

            var requestStream2 = await CreateRequestStream();

            await requestStream2.SendHeadersAsync(ReadRateRequestHeaders(_maxData.Length), endStream: false);
            await requestStream2.SendDataAsync(_maxData, endStream: false);

            await requestStream2.ExpectHeadersAsync();
            await requestStream2.ExpectDataAsync();

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
                expectedLastStreamId: null,
                Http3ErrorCode.InternalError,
                null);

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DATA_Received_SlowlyWhenRateLimitDisabledPerRequest_DoesNotAbortConnection()
        {
            var mockSystemClock = _serviceContext.MockSystemClock;
            var limits = _serviceContext.ServerOptions.Limits;

            // Use non-default value to ensure the min request and response rates aren't mixed up.
            limits.MinRequestBodyDataRate = new MinDataRate(480, TimeSpan.FromSeconds(2.5));

            _timeoutControl.Initialize(mockSystemClock.UtcNow.Ticks);

            var requestStream = await InitializeConnectionAndStreamsAsync(context =>
            {
               // Completely disable rate limiting for this stream.
               context.Features.Get<IHttpMinRequestBodyDataRateFeature>().MinDataRate = null;
                return _readRateApplication(context);
            });

            var inboundControlStream = await GetInboundControlStream();
            await inboundControlStream.ExpectSettingsAsync();

            // _helloWorldBytes is 12 bytes, and 12 bytes / 240 bytes/sec = .05 secs which is far below the grace period.
            await requestStream.SendHeadersAsync(ReadRateRequestHeaders(_helloWorldBytes.Length), endStream: false);
            await requestStream.SendDataAsync(_helloWorldBytes, endStream: false);

            await requestStream.ExpectHeadersAsync();

            await requestStream.ExpectDataAsync();

            // Don't send any more data and advance just to and then past the grace period.
            AdvanceClock(limits.MinRequestBodyDataRate.GracePeriod);

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            AdvanceClock(TimeSpan.FromTicks(1));

            _mockTimeoutHandler.Verify(h => h.OnTimeout(It.IsAny<TimeoutReason>()), Times.Never);

            await requestStream.SendDataAsync(_helloWorldBytes, endStream: true);

            await requestStream.ExpectReceiveEndOfStream();

            _mockTimeoutHandler.VerifyNoOtherCalls();
        }

    }
}
