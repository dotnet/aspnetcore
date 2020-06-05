// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2KeepAliveTests : Http2TestBase
    {
        [Fact]
        public async Task IntervalExceededWithoutActivity_PingSent()
        {
            _serviceContext.ServerOptions.Limits.Http2.KeepAlivePingInterval = TimeSpan.FromSeconds(1);

            await InitializeConnectionAsync(_noopApplication);

            DateTimeOffset now = new DateTimeOffset(1, TimeSpan.Zero);

            // Heartbeat
            TriggerTick(now);

            // Heartbeat that exceeds interval
            TriggerTick(now + TimeSpan.FromSeconds(1.1));

            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.NONE,
                withStreamId: 0);

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task IntervalExceededWithActivity_NoPingSent()
        {
            _serviceContext.ServerOptions.Limits.Http2.KeepAlivePingInterval = TimeSpan.FromSeconds(1);

            await InitializeConnectionAsync(_noopApplication);

            DateTimeOffset now = new DateTimeOffset(1, TimeSpan.Zero);

            // Heartbeat
            TriggerTick(now);

            await SendPingAsync(Http2PingFrameFlags.NONE);
            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.ACK,
                withStreamId: 0);

            // Heartbeat that exceeds interval
            TriggerTick(now + TimeSpan.FromSeconds(1.1));

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task IntervalNotExceeded_NoPingSent()
        {
            _serviceContext.ServerOptions.Limits.Http2.KeepAlivePingInterval = TimeSpan.FromSeconds(5);

            await InitializeConnectionAsync(_noopApplication);

            DateTimeOffset now = new DateTimeOffset(1, TimeSpan.Zero);

            // Heartbeats
            TriggerTick(now);
            TriggerTick(now + TimeSpan.FromSeconds(1.1));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 2));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 3));

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task IntervalExceededMultipleTimes_PingsNotSentWhileAwaitingOnAck()
        {
            _serviceContext.ServerOptions.Limits.Http2.KeepAlivePingInterval = TimeSpan.FromSeconds(1);

            await InitializeConnectionAsync(_noopApplication);

            DateTimeOffset now = new DateTimeOffset(1, TimeSpan.Zero);

            TriggerTick(now);
            TriggerTick(now + TimeSpan.FromSeconds(1.1));

            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.NONE,
                withStreamId: 0);

            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 2));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 3));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 4));

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task IntervalExceededMultipleTimes_PingSentAfterAck()
        {
            _serviceContext.ServerOptions.Limits.Http2.KeepAlivePingInterval = TimeSpan.FromSeconds(1);

            await InitializeConnectionAsync(_noopApplication);

            DateTimeOffset now = new DateTimeOffset(1, TimeSpan.Zero);

            // Heartbeats
            TriggerTick(now);
            TriggerTick(now + TimeSpan.FromSeconds(1.1));

            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.NONE,
                withStreamId: 0);
            await SendPingAsync(Http2PingFrameFlags.ACK);

            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 2));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 3));

            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.NONE,
                withStreamId: 0);
            await SendPingAsync(Http2PingFrameFlags.ACK);

            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 4));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 5));

            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.NONE,
                withStreamId: 0);

            await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        }

        [Fact]
        public async Task TimeoutExceededWithoutAck_GoAway()
        {
            _serviceContext.ServerOptions.Limits.Http2.KeepAlivePingInterval = TimeSpan.FromSeconds(1);
            _serviceContext.ServerOptions.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromSeconds(3);

            await InitializeConnectionAsync(_noopApplication);

            DateTimeOffset now = new DateTimeOffset(1, TimeSpan.Zero);

            // Heartbeat
            TriggerTick(now);

            // Heartbeat that exceeds interval
            TriggerTick(now + TimeSpan.FromSeconds(1.1));

            await ExpectAsync(Http2FrameType.PING,
                withLength: 8,
                withFlags: (byte)Http2PingFrameFlags.NONE,
                withStreamId: 0);

            // Heartbeat that exceeds timeout
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 2));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 3));
            TriggerTick(now + TimeSpan.FromSeconds(1.1 * 4));

            VerifyGoAway(await ReceiveFrameAsync(), 0, Http2ErrorCode.CANCEL);
        }
    }
}
