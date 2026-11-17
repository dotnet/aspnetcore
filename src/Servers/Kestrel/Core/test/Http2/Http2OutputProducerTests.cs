// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2OutputProducerTests
{
    [Fact]
    public void GetFakeMemoryWithZeroSizeHintReturnsNonEmptyMemory()
    {
        var context = TestContextFactory.CreateHttp2StreamContext(
            serviceContext: new TestServiceContext(),
            memoryPool: MemoryPool<byte>.Shared);
        using var stream = new TestHttp2Stream(context);
        var output = Assert.IsType<Http2OutputProducer>(stream.Output);

        var memory = output.GetFakeMemory(0);

        Assert.True(memory.Length > 0);
    }

    private sealed class TestHttp2Stream : Http2Stream
    {
        public TestHttp2Stream(Http2StreamContext context)
        {
            Initialize(context);
        }

        public override void Execute()
        {
        }
    }
}
