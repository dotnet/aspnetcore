// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;

namespace FunctionalTests
{
    public class CustomObject
    {
        public string Name { get; set; }

        public int Value { get; set; }
    }

    public class TestHub : Hub
    {
        public string Echo(string message)
        {
            return message;
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
                GUID = new Guid("00010203-0405-0607-0706-050403020100"),
                IntArray = new int[] { 1, 2, 3 },
                String = "hello world",
            };
        }
    }
}
