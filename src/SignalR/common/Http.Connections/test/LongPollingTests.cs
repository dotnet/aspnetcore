// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal.Transports;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class LongPollingTests : VerifiableLoggedTest
{
    [Fact]
    public async Task Set204StatusCodeWhenChannelComplete()
    {
        using (StartVerifiableLog())
        {
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);

            var context = new DefaultHttpContext();

            var poll = new LongPollingServerTransport(CancellationToken.None, connection.Application.Input, LoggerFactory);

            connection.Transport.Output.Complete();

            await poll.ProcessRequestAsync(context, context.RequestAborted).DefaultTimeout();

            Assert.Equal(204, context.Response.StatusCode);
        }
    }

    [Fact]
    public async Task Set200StatusCodeWhenTimeoutTokenFires()
    {
        using (StartVerifiableLog())
        {
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
            var context = new DefaultHttpContext();

            var timeoutToken = new CancellationToken(true);
            var poll = new LongPollingServerTransport(timeoutToken, connection.Application.Input, LoggerFactory);

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, context.RequestAborted))
            {
                await poll.ProcessRequestAsync(context, cts.Token).DefaultTimeout();

                Assert.Equal(0, context.Response.ContentLength);
                Assert.Equal(200, context.Response.StatusCode);
            }
        }
    }

    [Fact]
    public async Task FrameSentAsSingleResponse()
    {
        using (StartVerifiableLog())
        {
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
            var context = new DefaultHttpContext();

            var poll = new LongPollingServerTransport(CancellationToken.None, connection.Application.Input, LoggerFactory);
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));
            connection.Transport.Output.Complete();

            await poll.ProcessRequestAsync(context, context.RequestAborted).DefaultTimeout();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("Hello World", Encoding.UTF8.GetString(ms.ToArray()));
        }
    }

    [Fact]
    public async Task MultipleFramesSentAsSingleResponse()
    {
        using (StartVerifiableLog())
        {
            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new DefaultConnectionContext("foo", pair.Transport, pair.Application);
            var context = new DefaultHttpContext();

            var poll = new LongPollingServerTransport(CancellationToken.None, connection.Application.Input, LoggerFactory);
            var ms = new MemoryStream();
            context.Response.Body = ms;

            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes(" "));
            await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("World"));

            connection.Transport.Output.Complete();

            await poll.ProcessRequestAsync(context, context.RequestAborted).DefaultTimeout();

            Assert.Equal(200, context.Response.StatusCode);

            var payload = ms.ToArray();
            Assert.Equal("Hello World", Encoding.UTF8.GetString(payload));
        }
    }

    [Fact]
    public void CheckLongPollingTimeoutValue()
    {
        var options = new HttpConnectionDispatcherOptions();
        Assert.Equal(options.LongPolling.PollTimeout, TimeSpan.FromSeconds(90));
    }
}
