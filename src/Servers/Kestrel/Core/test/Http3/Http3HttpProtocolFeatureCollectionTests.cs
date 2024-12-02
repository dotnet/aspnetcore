// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http3HttpProtocolFeatureCollectionTests
{
    private readonly IFeatureCollection _http3Collection;

    public Http3HttpProtocolFeatureCollectionTests()
    {
        var connectionFeatures = new TestConnectionFeatures().FeatureCollection;

        var streamContext = TestContextFactory.CreateHttp3StreamContext(
            transport: DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions()).Application,
            connectionFeatures: connectionFeatures);

        var http3Stream = new TestHttp3Stream();
        http3Stream.Initialize(streamContext);
        _http3Collection = http3Stream;
    }

    [Fact]
    public void Http3StreamFeatureCollectionDoesNotIncludeIHttpMinResponseDataRateFeature()
    {
        Assert.Null(_http3Collection.Get<IHttpMinResponseDataRateFeature>());
    }

    [Fact]
    public void Http3StreamFeatureCollectionDoesIncludeUpgradeFeature()
    {
        var upgradeFeature = _http3Collection.Get<IHttpUpgradeFeature>();

        Assert.NotNull(upgradeFeature);
        Assert.False(upgradeFeature.IsUpgradableRequest);
    }

    [Fact]
    public void Http3StreamFeatureCollectionDoesIncludeIHttpMinRequestBodyDataRateFeature()
    {
        var minRateFeature = _http3Collection.Get<IHttpMinRequestBodyDataRateFeature>();

        Assert.NotNull(minRateFeature);

        Assert.Throws<NotSupportedException>(() => minRateFeature.MinDataRate);
        Assert.Throws<NotSupportedException>(() => minRateFeature.MinDataRate = new MinDataRate(1, TimeSpan.FromSeconds(2)));

        // You can set the MinDataRate to null though.
        minRateFeature.MinDataRate = null;

        // But you still cannot read the property;
        Assert.Throws<NotSupportedException>(() => minRateFeature.MinDataRate);
    }

    private class TestHttp3Stream : Http3Stream
    {
        public override void Execute()
        {
        }
    }

    private class TestConnectionFeatures : IProtocolErrorCodeFeature, IStreamIdFeature, IStreamAbortFeature, IStreamClosedFeature, IConnectionMetricsContextFeature
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
