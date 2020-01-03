// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;

namespace FunctionalTests
{
    public class CustomObject
    {
        public string Name { get; set; }

        public int Value { get; set; }
    }

    public class TestHub : Hub
    {
        private readonly IHubContext<TestHub> _context;

        public TestHub(IHubContext<TestHub> context)
        {
            _context = context;
        }

        public string Echo(string message)
        {
            return message;
        }

        public string GetCallerConnectionId()
        {
            return Context.ConnectionId;
        }

        public int GetNumRedirects()
        {
            return int.Parse(Context.GetHttpContext().Request.Query["numRedirects"]);
        }

        public void ThrowException(string message)
        {
            throw new InvalidOperationException(message);
        }

        public Task InvokeWithString(string message)
        {
            return Clients.Client(Context.ConnectionId).SendAsync("Message", message);
        }

        public Task SendCustomObject(CustomObject customObject)
        {
            return Clients.Client(Context.ConnectionId).SendAsync("CustomObject", customObject);
        }

        public ChannelReader<string> Stream()
        {
            var channel = Channel.CreateUnbounded<string>();
            channel.Writer.TryWrite("a");
            channel.Writer.TryWrite("b");
            channel.Writer.TryWrite("c");
            channel.Writer.Complete();
            return channel.Reader;
        }

        public ChannelReader<string> InfiniteStream(CancellationToken token)
        {
            var channel = Channel.CreateUnbounded<string>();
            var connectionId = Context.ConnectionId;

            token.Register(async (state) =>
            {
                await ((IHubContext<TestHub>)state).Clients.Client(connectionId).SendAsync("StreamCanceled");
            }, _context);

            return channel.Reader;
        }

        public async Task<string> StreamingConcat(ChannelReader<string> stream)
        {
            var sb = new StringBuilder();

            while (await stream.WaitToReadAsync())
            {
                while (stream.TryRead(out var item))
                {
                    sb.Append(item);
                }
            }

            return sb.ToString();
        }

        public ChannelReader<int> EmptyStream()
        {
            var channel = Channel.CreateUnbounded<int>();
            channel.Writer.Complete();
            return channel.Reader;
        }

        public ChannelReader<string> StreamThrowException(string message)
        {
            throw new InvalidOperationException(message);
        }

        public string GetActiveTransportName()
        {
            return Context.Features.Get<IHttpTransportFeature>().TransportType.ToString();
        }

        public ComplexObject EchoComplexObject(ComplexObject complexObject)
        {
            return complexObject;
        }

        public ComplexObject SendComplexObject()
        {
            return new ComplexObject
            {
                ByteArray = new byte[] { 0x1, 0x2, 0x3 },
                DateTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Guid = new Guid("00010203-0405-0607-0706-050403020100"),
                IntArray = new int[] { 1, 2, 3 },
                String = "hello world",
            };
        }

        public string GetContentTypeHeader()
        {
            return Context.GetHttpContext().Request.Headers["Content-Type"];
        }

        public string GetHeader(string headerName)
        {
            return Context.GetHttpContext().Request.Headers[headerName];
        }

        public string GetCookie(string cookieName)
        {
            var cookies = Context.GetHttpContext().Request.Cookies;
            if (cookies.TryGetValue(cookieName, out var cookieValue))
            {
                return cookieValue;
            }
            return null;
        }
    }
}
