// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3ConnectionTests : Http3TestBase
    {
        [Fact]
        public async Task GoAwayReceived()
        {
            await InitializeConnectionAsync(_echoApplication);

            var outboundcontrolStream = await CreateControlStream();
            var inboundControlStream = await GetInboundControlStream();

            Connection.Abort(new ConnectionAbortedException(), Http3ErrorCode.NoError);
            await _closedStateReached.Task.DefaultTimeout();
            await WaitForConnectionErrorAsync(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 0,
                expectedErrorCode: Http3ErrorCode.NoError);
        }

        [Fact]
        public async Task CreateRequestStream_RequestCompleted_Disposed()
        {
            var appCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await InitializeConnectionAsync(async context =>
            {
                var buffer = new byte[16 * 1024];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, received);
                }

                await appCompletedTcs.Task;
            });

            await CreateControlStream();
            await GetInboundControlStream();

            var requestStream = await CreateRequestStream();

            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };

            await requestStream.SendHeadersAsync(headers);
            await requestStream.SendDataAsync(Encoding.ASCII.GetBytes("Hello world"), endStream: true);

            Assert.False(requestStream.Disposed);

            appCompletedTcs.SetResult();
            await requestStream.ExpectHeadersAsync();
            var responseData = await requestStream.ExpectDataAsync();
            Assert.Equal("Hello world", Encoding.ASCII.GetString(responseData.ToArray()));

            Assert.True(requestStream.Disposed);
        }

        [Fact]
        public async Task GracefulServerShutdownSendsGoawayClosesConnection()
        {
            await InitializeConnectionAsync(_echoApplication);
            // Trigger server shutdown.
            MultiplexedConnectionContext.ConnectionClosingCts.Cancel();
            Assert.Null(await MultiplexedConnectionContext.AcceptAsync().DefaultTimeout());
        }

        [Fact]
        public async Task SETTINGS_ReservedSettingSent_ConnectionError()
        {
            await InitializeConnectionAsync(_echoApplication);

            var outboundcontrolStream = await CreateControlStream();
            await outboundcontrolStream.SendSettingsAsync(new List<Http3PeerSetting>
            {
                new Http3PeerSetting(0x0, 0) // reserved value
            });

            await GetInboundControlStream();

            await WaitForConnectionErrorAsync(
                ignoreNonGoAwayFrames: true,
                expectedLastStreamId: 0,
                expectedErrorCode: Http3ErrorCode.SettingsError);
        }
    }
}
