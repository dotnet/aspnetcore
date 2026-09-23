// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http3OutputProducerTests
{
    [Fact]
    public void GetFakeMemoryWithZeroSizeHintReturnsNonEmptyMemory()
    {
        var connectionFeatures = new TestConnectionFeatures().FeatureCollection;
        var streamContext = TestContextFactory.CreateHttp3StreamContext(
            transport: DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions()).Application,
            connectionFeatures: connectionFeatures,
            memoryPool: MemoryPool<byte>.Shared);
        var stream = new TestHttp3Stream();
        stream.Initialize(streamContext);
        var output = Assert.IsType<Http3OutputProducer>(stream.Output);

        try
        {
            var memory = output.GetFakeMemory(0);

            Assert.True(memory.Length > 0);
        }
        finally
        {
            output.Dispose();
        }
    }

    private sealed class TestHttp3Stream : Http3Stream
    {
        public override void Execute()
        {
        }
    }

    private sealed class TestConnectionFeatures : IProtocolErrorCodeFeature, IStreamIdFeature, IStreamAbortFeature, IStreamClosedFeature, IConnectionMetricsContextFeature
    {
        public TestConnectionFeatures()
        {
            var featureCollection = new FeatureCollection();
            featureCollection.Set<IProtocolErrorCodeFeature>(this);
            featureCollection.Set<IStreamIdFeature>(this);
            featureCollection.Set<IStreamAbortFeature>(this);
            featureCollection.Set<IStreamClosedFeature>(this);
            featureCollection.Set<IConnectionMetricsContextFeature>(this);

            FeatureCollection = featureCollection;
        }

        public IFeatureCollection FeatureCollection { get; }
        public ConnectionMetricsContext MetricsContext { get; }
        long IProtocolErrorCodeFeature.Error { get; set; }
        long IStreamIdFeature.StreamId { get; }

        void IStreamAbortFeature.AbortRead(long errorCode, ConnectionAbortedException abortReason)
        {
            throw new NotImplementedException();
        }

        void IStreamAbortFeature.AbortWrite(long errorCode, ConnectionAbortedException abortReason)
        {
            throw new NotImplementedException();
        }

        void IStreamClosedFeature.OnClosed(Action<object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}
