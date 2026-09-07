// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2OutputProducerTests
{
    [Fact]
    public void GetFakeMemoryWithZeroSizeHintReturnsNonEmptyMemory()
    {
        var memoryPool = CreateMemoryPool();
        var context = TestContextFactory.CreateHttp2StreamContext(
            serviceContext: new TestServiceContext(),
            memoryPool: memoryPool.Object,
            timeoutControl: Mock.Of<ITimeoutControl>());
        using var stream = new TestHttp2Stream(context);
        var output = Assert.IsType<Http2OutputProducer>(stream.Output);

        var memory = output.GetFakeMemory(0);

        Assert.True(memory.Length > 0);
    }

    private static Mock<MemoryPool<byte>> CreateMemoryPool()
    {
        var memoryPool = new Mock<MemoryPool<byte>>();
        memoryPool.SetupGet(pool => pool.MaxBufferSize).Returns(MemoryPool<byte>.Shared.MaxBufferSize);
        memoryPool.Setup(pool => pool.Rent(0)).Returns(Mock.Of<IMemoryOwner<byte>>());
        memoryPool.Setup(pool => pool.Rent(It.Is<int>(size => size > 0)))
            .Returns((int size) =>
            {
                var memoryOwner = new Mock<IMemoryOwner<byte>>();
                memoryOwner.SetupGet(owner => owner.Memory).Returns(new byte[size]);
                return memoryOwner.Object;
            });

        return memoryPool;
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
