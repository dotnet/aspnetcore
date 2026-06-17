// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class PooledStreamStackTests
{
    [Fact]
    public void RemoveExpired_Empty_NoOp()
    {
        var streams = new PooledStreamStack<Http2Stream>(10);

        streams.RemoveExpired(100);
    }

    [Fact]
    public void RemoveExpired_NoneExpired_NoOp()
    {
        var streams = new PooledStreamStack<Http2Stream>(10);
        streams.Push(CreateStream(streamId: 1, expirationTimestamp: 200));

        streams.RemoveExpired(100);

        Assert.Equal(1, streams.Count);
        Assert.Equal(1, ((Http2Stream)streams._array[0]).StreamId);
    }

    [Fact]
    public void RemoveExpired_OneExpired_ExpiredStreamRemoved()
    {
        var streams = new PooledStreamStack<Http2Stream>(10);
        streams.Push(CreateStream(streamId: 1, expirationTimestamp: 200));

        streams.RemoveExpired(300);

        Assert.Equal(0, streams.Count);
        Assert.Equal(default, streams._array[0]);
    }

    [Fact]
    public void RemoveExpired_MultipleExpired_ExpiredStreamsRemoved()
    {
        var streams = new PooledStreamStack<Http2Stream>(10);
        streams.Push(CreateStream(streamId: 1, expirationTimestamp: 200));
        streams.Push(CreateStream(streamId: 2, expirationTimestamp: 250));

        streams.RemoveExpired(300);

        Assert.Equal(0, streams.Count);
        Assert.Equal(default, streams._array[0]);
        Assert.Equal(default, streams._array[1]);
    }

    [Fact]
    public void RemoveExpired_OneExpiredAndOneValid_ExpiredStreamRemoved()
    {
        var streams = new PooledStreamStack<Http2Stream>(10);
        streams.Push(CreateStream(streamId: 1, expirationTimestamp: 200));
        streams.Push(CreateStream(streamId: 2, expirationTimestamp: 400));

        streams.RemoveExpired(300);

        Assert.Equal(1, streams.Count);
        Assert.Equal(2, ((Http2Stream)streams._array[0]).StreamId);
        Assert.Equal(default, streams._array[1]);
    }

    [Fact]
    public void RemoveExpired_AllExpired_ExpiredStreamRemoved()
    {
        var streams = new PooledStreamStack<Http2Stream>(5);
        streams.Push(CreateStream(streamId: 1, expirationTimestamp: 200));
        streams.Push(CreateStream(streamId: 2, expirationTimestamp: 200));
        streams.Push(CreateStream(streamId: 3, expirationTimestamp: 200));
        streams.Push(CreateStream(streamId: 4, expirationTimestamp: 200));
        streams.Push(CreateStream(streamId: 5, expirationTimestamp: 200));

        streams.RemoveExpired(300);

        Assert.Equal(0, streams.Count);
        Assert.Equal(5, streams._array.Length);
        Assert.Equal(default, streams._array[0]);
        Assert.Equal(default, streams._array[1]);
        Assert.Equal(default, streams._array[2]);
        Assert.Equal(default, streams._array[3]);
        Assert.Equal(default, streams._array[4]);
    }

    private static Http2Stream<HttpContext> CreateStream(int streamId, long expirationTimestamp)
    {
        var context = TestContextFactory.CreateHttp2StreamContext(connectionId: "TestConnectionId", streamId: streamId);

        return new Http2Stream<HttpContext>(new DummyApplication(), context)
        {
            DrainExpirationTimestamp = expirationTimestamp
        };
    }
}
