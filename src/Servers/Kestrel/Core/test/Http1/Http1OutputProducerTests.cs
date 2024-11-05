// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http1OutputProducerTests : IDisposable
{
    private readonly MemoryPool<byte> _memoryPool;

    public Http1OutputProducerTests()
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();
    }

    public void Dispose()
    {
        _memoryPool.Dispose();
    }

    [Fact]
    public async Task WritesNoopAfterConnectionCloses()
    {
        var pipeOptions = new PipeOptions
        (
            pool: _memoryPool,
            readerScheduler: Mock.Of<PipeScheduler>(),
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false
        );

        using (var socketOutput = CreateOutputProducer(pipeOptions))
        {
            // Close
            socketOutput.Dispose();

            await socketOutput.WriteDataAsync(new byte[] { 1, 2, 3, 4 }, default);

            Assert.False(socketOutput.Pipe.Reader.TryRead(out var result));

            socketOutput.Pipe.Writer.Complete();
            socketOutput.Pipe.Reader.Complete();
        }
    }

    [Fact]
    public async Task FlushAsync_OnDisposedSocket_ReturnsResultWithIsCompletedTrue()
    {
        var pipeOptions = new PipeOptions
        (
            pool: _memoryPool,
            readerScheduler: Mock.Of<PipeScheduler>(),
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false
        );

        var socketOutput = CreateOutputProducer(pipeOptions);

        await socketOutput.WriteDataAsync(new byte[] { 1, 2, 3, 4 }, default);
        var successResult = await socketOutput.FlushAsync();
        Assert.False(successResult.IsCompleted);

        // Close
        socketOutput.Dispose();

        await socketOutput.WriteDataAsync(new byte[] { 1, 2, 3, 4 }, default);
        var completedResult = await socketOutput.FlushAsync();
        Assert.True(completedResult.IsCompleted);

        socketOutput.Pipe.Writer.Complete();
        socketOutput.Pipe.Reader.Complete();
    }

    [Fact]
    public async Task FlushAsync_OnSocketWithCanceledPendingFlush_ReturnsResultWithIsCanceledTrue()
    {
        var pipeOptions = new PipeOptions
        (
            pool: _memoryPool,
            readerScheduler: Mock.Of<PipeScheduler>(),
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false
        );

        var socketOutput = CreateOutputProducer(pipeOptions);

        await socketOutput.WriteDataAsync(new byte[] { 1, 2, 3, 4 }, default);
        var successResult = await socketOutput.FlushAsync();
        Assert.False(successResult.IsCanceled);

        // Close
        socketOutput.CancelPendingFlush();

        var cancelResult = await socketOutput.WriteDataToPipeAsync(new byte[] { 1, 2, 3, 4 }, default);
        Assert.True(cancelResult.IsCanceled);

        // only one flush should be cancelled
        var goodResult = await socketOutput.WriteDataToPipeAsync(new byte[] { 1, 2, 3, 4 }, default);
        Assert.False(goodResult.IsCanceled);

        socketOutput.Pipe.Writer.Complete();
        socketOutput.Pipe.Reader.Complete();
    }

    [Fact]
    public void AbortsTransportEvenAfterDispose()
    {
        var mockConnectionContext = new Mock<ConnectionContext>();
        var metricsContext = new ConnectionMetricsContext { ConnectionContext = mockConnectionContext.Object };

        var outputProducer = CreateOutputProducer(connectionContext: mockConnectionContext.Object, metricsContext: metricsContext);

        outputProducer.Dispose();

        mockConnectionContext.Verify(f => f.Abort(It.IsAny<ConnectionAbortedException>()), Times.Never());

        outputProducer.Abort(null, ConnectionEndReason.AbortedByApp);

        mockConnectionContext.Verify(f => f.Abort(null), Times.Once());

        outputProducer.Abort(null, ConnectionEndReason.AbortedByApp);

        mockConnectionContext.Verify(f => f.Abort(null), Times.Once());

        Assert.Equal(ConnectionEndReason.AbortedByApp, metricsContext.ConnectionEndReason);
    }

    [Fact]
    public void AllocatesFakeMemorySmallerThanMaxBufferSize()
    {
        var pipeOptions = new PipeOptions
        (
            pool: _memoryPool,
            readerScheduler: Mock.Of<PipeScheduler>(),
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false
        );

        using (var socketOutput = CreateOutputProducer(pipeOptions))
        {
            var bufferSize = 1;
            var fakeMemory = socketOutput.GetFakeMemory(bufferSize);

            Assert.True(fakeMemory.Length >= bufferSize);
        }
    }

    [Fact]
    public void AllocatesFakeMemoryBiggerThanMaxBufferSize()
    {
        var pipeOptions = new PipeOptions
        (
            pool: _memoryPool,
            readerScheduler: Mock.Of<PipeScheduler>(),
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false
        );

        using (var socketOutput = CreateOutputProducer(pipeOptions))
        {
            var bufferSize = _memoryPool.MaxBufferSize * 2;
            var fakeMemory = socketOutput.GetFakeMemory(bufferSize);

            Assert.True(fakeMemory.Length >= bufferSize);
        }
    }

    [Fact]
    public void AllocatesIncreasingFakeMemory()
    {
        var pipeOptions = new PipeOptions
        (
            pool: _memoryPool,
            readerScheduler: Mock.Of<PipeScheduler>(),
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false
        );

        using (var socketOutput = CreateOutputProducer(pipeOptions))
        {
            var bufferSize1 = 1024;
            var bufferSize2 = 2048;
            var fakeMemory = socketOutput.GetFakeMemory(bufferSize1);
            fakeMemory = socketOutput.GetFakeMemory(bufferSize2);

            Assert.True(fakeMemory.Length >= bufferSize2);
        }
    }

    [Fact]
    public void ReusesFakeMemory()
    {
        var pipeOptions = new PipeOptions
        (
            pool: _memoryPool,
            readerScheduler: Mock.Of<PipeScheduler>(),
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false
        );

        using (var socketOutput = CreateOutputProducer(pipeOptions))
        {
            var bufferSize = 1024;
            var fakeMemory1 = socketOutput.GetFakeMemory(bufferSize);
            var fakeMemory2 = socketOutput.GetFakeMemory(bufferSize);

            Assert.Equal(fakeMemory1, fakeMemory2);
        }
    }

    private TestHttpOutputProducer CreateOutputProducer(
        PipeOptions pipeOptions = null,
        ConnectionContext connectionContext = null,
        ConnectionMetricsContext metricsContext = null)
    {
        pipeOptions = pipeOptions ?? new PipeOptions();
        connectionContext = connectionContext ?? Mock.Of<ConnectionContext>();

        var pipe = new Pipe(pipeOptions);
        var serviceContext = new TestServiceContext();
        var socketOutput = new TestHttpOutputProducer(
            pipe,
            "0",
            connectionContext,
            _memoryPool,
            serviceContext.Log,
            Mock.Of<ITimeoutControl>(),
            Mock.Of<IHttpMinResponseDataRateFeature>(),
            metricsContext ?? new ConnectionMetricsContext { ConnectionContext = connectionContext },
            Mock.Of<IHttpOutputAborter>());

        return socketOutput;
    }

    private sealed class TestConnectionMetricsContextFeature : IConnectionMetricsContextFeature
    {
        public ConnectionMetricsContext MetricsContext { get; }
    }

    private class TestHttpOutputProducer : Http1OutputProducer
    {
        public TestHttpOutputProducer(Pipe pipe, string connectionId, ConnectionContext connectionContext, MemoryPool<byte> memoryPool, KestrelTrace log, ITimeoutControl timeoutControl, IHttpMinResponseDataRateFeature minResponseDataRateFeature, ConnectionMetricsContext metricsContext, IHttpOutputAborter outputAborter)
            : base(pipe.Writer, connectionId, connectionContext, memoryPool, log, timeoutControl, minResponseDataRateFeature, metricsContext, outputAborter)
        {
            Pipe = pipe;
        }

        public Pipe Pipe { get; }
    }
}
