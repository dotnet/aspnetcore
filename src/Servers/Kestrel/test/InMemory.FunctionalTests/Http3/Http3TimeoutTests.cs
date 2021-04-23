// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
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

            await AssertIsTrueRetryAsync(
                () => Connection._streams.Count == 2,
                "Wait until streams have been created.").DefaultTimeout();

            var serverRequestStream = Connection._streams[requestStream.StreamId];

            await requestStream.SendHeadersPartialAsync().DefaultTimeout();

            TriggerTick(now);
            TriggerTick(now + limits.RequestHeadersTimeout);

            Assert.Equal((now + limits.RequestHeadersTimeout).Ticks, serverRequestStream.HeaderTimeoutTicks);

            TriggerTick(now + limits.RequestHeadersTimeout + TimeSpan.FromTicks(1));

            await requestStream.WaitForStreamErrorAsync(Http3ErrorCode.RequestRejected, CoreStrings.BadRequest_RequestHeadersTimeout);
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/32106")]
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

            await AssertIsTrueRetryAsync(
                () => Connection._streams.Count == 2,
                "Wait until streams have been created.").DefaultTimeout();

            var serverRequestStream = Connection._streams[requestStream.StreamId];

            TriggerTick(now);
            TriggerTick(now + limits.RequestHeadersTimeout);

            Assert.Equal((now + limits.RequestHeadersTimeout).Ticks, serverRequestStream.HeaderTimeoutTicks);

            await requestStream.SendHeadersAsync(headers).DefaultTimeout();

            await AssertIsTrueRetryAsync(
                () => serverRequestStream.ReceivedHeader,
                "Request stream has read headers.").DefaultTimeout();

            TriggerTick(now + limits.RequestHeadersTimeout + TimeSpan.FromTicks(1));

            await requestStream.SendDataAsync(Memory<byte>.Empty, endStream: true);

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

            await AssertIsTrueRetryAsync(
                () => Connection._streams.Count == 1,
                "Wait until streams have been created.").DefaultTimeout();

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

            await AssertIsTrueRetryAsync(
                () => Connection._streams.Count == 1,
                "Wait until streams have been created.").DefaultTimeout();

            var serverInboundControlStream = Connection._streams[outboundControlStream.StreamId];

            TriggerTick(now);
            TriggerTick(now + limits.RequestHeadersTimeout);

            await outboundControlStream.WriteStreamIdAsync(id: 0);

            await AssertIsTrueRetryAsync(
                () => serverInboundControlStream.ReceivedHeader,
                "Control stream has read header.").DefaultTimeout();

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

            await AssertIsTrueRetryAsync(
                () => Connection._streams.Count == 1,
                "Wait until streams have been created.").DefaultTimeout();

            var serverInboundControlStream = Connection._streams[outboundControlStream.StreamId];

            TriggerTick(now);

            Assert.Equal(TimeSpan.MaxValue.Ticks, serverInboundControlStream.HeaderTimeoutTicks);
        }

        private static async Task AssertIsTrueRetryAsync(Func<bool> assert, string message)
        {
            const int Retries = 10;

            for (var i = 0; i < Retries; i++)
            {
                if (i > 0)
                {
                    await Task.Delay((i + 1) * 10);
                }

                if (assert())
                {
                    return;
                }
            }

            throw new Exception($"Assert failed after {Retries} retries: {message}");
        }
    }
}
