// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
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

            Connection.Abort(new ConnectionAbortedException());
            await _closedStateReached.Task.DefaultTimeout();
            await WaitForConnectionErrorAsync(ignoreNonGoAwayFrames: true, expectedLastStreamId: 0, expectedErrorCode: 0);
        }

        [Fact]
        public async Task GracefulServerShutdownSendsGoawayClosesConnection()
        {
            await InitializeConnectionAsync(_echoApplication);
            // Trigger server shutdown.
            MultiplexedConnectionContext.ConnectionClosingCts.Cancel();
            Assert.Null(await MultiplexedConnectionContext.AcceptAsync().DefaultTimeout());
        }
    }
}
