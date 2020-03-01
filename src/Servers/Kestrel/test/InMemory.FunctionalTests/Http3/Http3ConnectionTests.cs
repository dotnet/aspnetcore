// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3ConnectionTests : Http3TestBase
    {
        [Fact]
        public async Task VerifySettingsAreReceived()
        {
            await InitializeConnectionAsync(_noopApplication);
            await CreateOutboundControlStream(ControlStreamId);
            await CreateOutboundControlStream(EncoderStreamId);
            await CreateOutboundControlStream(DecoderStreamId);
            await WaitForInboundControlStreamCreated();

            var settings = await ReadSettings();
            Assert.Equal(new KestrelServerLimits().MaxRequestHeadersTotalSize, settings[(long)Http3SettingType.MaxHeaderListSize]);
        }

        [Fact]
        [Flaky("<No longer needed; tracked in Kusto>", FlakyOn.All)]
        public async Task VerifyDefaultSettingsAreSent()
        {
            // It's hard to know if the peer receives any setting updates, as they occur on
            // a separate stream from the request stream.
            // This test currently has to shim the client options to know when the max header list size
            // is modified.
            var clientSettings = new Http3PeerSettings();
            clientSettings.MaxHeaderListSize = 1;

            await InitializeConnectionAsync(_echoApplication);

            await CreateOutboundControlStream(ControlStreamId);

            await WriteSettings(clientSettings.GetNonProtocolDefaults());

            await CreateOutboundControlStream(EncoderStreamId);
            await CreateOutboundControlStream(DecoderStreamId);

            await WaitForInboundControlStreamCreated();

            var requestStream = await CreateRequestStream();
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Get"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };

            var doneWithHeaders = await requestStream.SendHeadersAsync(headers, endStream: true);
            await requestStream.WaitForStreamErrorAsync(Http3ErrorCode.ProtocolError, "Exceeded client request max header list size.");
        }
    }
}
