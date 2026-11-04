// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http1HttpProtocolFeatureCollectionTests
{
    private readonly TestHttp1Connection _http1Connection;
    private readonly HttpConnectionContext _httpConnectionContext;
    private readonly IFeatureCollection _collection;

    public Http1HttpProtocolFeatureCollectionTests()
    {
        var connectionContext = Mock.Of<ConnectionContext>();
        var metricsContext = TestContextFactory.CreateMetricsContext(connectionContext);

        var connectionFeatures = new FeatureCollection();
        connectionFeatures.Set<IConnectionMetricsContextFeature>(new TestConnectionMetricsContextFeature { MetricsContext = metricsContext });

        var context = TestContextFactory.CreateHttpConnectionContext(
            connectionContext: connectionContext,
            serviceContext: new TestServiceContext(),
            transport: Mock.Of<IDuplexPipe>(),
            connectionFeatures: connectionFeatures,
            timeoutControl: Mock.Of<ITimeoutControl>(),
            metricsContext: metricsContext);

        _httpConnectionContext = context;
        _http1Connection = new TestHttp1Connection(context);
        _http1Connection.Reset();
        _collection = _http1Connection;
    }

    [Fact]
    public void FeaturesStartAsSelf()
    {
        var featureCount = GetFeaturesCount();

        Assert.NotEqual(0, featureCount);
    }

    [Fact]
    public void FeaturesCanBeAssignedTo()
    {
        var featureCount = SetFeaturesToNonDefault();
        Assert.NotEqual(0, featureCount);

        featureCount = 0;
        foreach (var feature in _collection)
        {
            Type type = feature.Key;
            if (type.IsAssignableFrom(typeof(HttpProtocol)))
            {
                Assert.Same(_collection[type], feature.Value);
                Assert.NotSame(_collection[type], _collection);
                featureCount++;
            }
        }

        Assert.NotEqual(0, featureCount);
    }

    [Fact]
    public void FeaturesResetToSelf()
    {
        var featuresAssigned = SetFeaturesToNonDefault();
        _http1Connection.ResetFeatureCollection();
        var featuresReset = GetFeaturesCount();

        Assert.Equal(featuresAssigned, featuresReset);
    }

    [Fact]
    public void FeaturesByGenericSameAsByType()
    {
        var featuresAssigned = SetFeaturesToNonDefault();

        CompareGenericGetterToIndexer();

        _http1Connection.ResetFeatureCollection();
        var featuresReset = GetFeaturesCount();

        Assert.Equal(featuresAssigned, featuresReset);
    }

    [Fact]
    public void FeaturesSetByTypeSameAsGeneric()
    {
        _collection[typeof(IHttpRequestFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpRequestBodyDetectionFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpResponseFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpResponseBodyFeature)] = CreateHttp1Connection();
        _collection[typeof(IRequestBodyPipeFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpRequestIdentifierFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpRequestLifetimeFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpRequestTrailersFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpConnectionFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpMaxRequestBodySizeFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpMinRequestBodyDataRateFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpMinResponseDataRateFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpBodyControlFeature)] = CreateHttp1Connection();
        _collection[typeof(IRouteValuesFeature)] = CreateHttp1Connection();
        _collection[typeof(IEndpointFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpExtendedConnectFeature)] = CreateHttp1Connection();
        _collection[typeof(IHttpUpgradeFeature)] = CreateHttp1Connection();
        _collection[typeof(IPersistentStateFeature)] = CreateHttp1Connection();
#pragma warning disable CA2252 // WebTransport is a preview feature
        _collection.Set<IHttpWebTransportFeature>(CreateHttp1Connection());
#pragma warning restore CA2252 // WebTransport is a preview feature

        CompareGenericGetterToIndexer();

        EachHttpProtocolFeatureSetAndUnique();
    }

    [Fact]
    public void FeaturesSetByGenericSameAsByType()
    {
        _collection.Set<IHttpRequestFeature>(CreateHttp1Connection());
        _collection.Set<IHttpRequestBodyDetectionFeature>(CreateHttp1Connection());
        _collection.Set<IHttpResponseFeature>(CreateHttp1Connection());
        _collection.Set<IHttpResponseBodyFeature>(CreateHttp1Connection());
        _collection.Set<IRequestBodyPipeFeature>(CreateHttp1Connection());
        _collection.Set<IHttpRequestIdentifierFeature>(CreateHttp1Connection());
        _collection.Set<IHttpRequestLifetimeFeature>(CreateHttp1Connection());
        _collection.Set<IHttpRequestTrailersFeature>(CreateHttp1Connection());
        _collection.Set<IHttpConnectionFeature>(CreateHttp1Connection());
        _collection.Set<IHttpMaxRequestBodySizeFeature>(CreateHttp1Connection());
        _collection.Set<IHttpMinRequestBodyDataRateFeature>(CreateHttp1Connection());
        _collection.Set<IHttpMinResponseDataRateFeature>(CreateHttp1Connection());
        _collection.Set<IHttpBodyControlFeature>(CreateHttp1Connection());
        _collection.Set<IRouteValuesFeature>(CreateHttp1Connection());
        _collection.Set<IEndpointFeature>(CreateHttp1Connection());
        _collection.Set<IHttpExtendedConnectFeature>(CreateHttp1Connection());
        _collection.Set<IHttpUpgradeFeature>(CreateHttp1Connection());
        _collection.Set<IPersistentStateFeature>(CreateHttp1Connection());
#pragma warning disable CA2252 // WebTransport is a preview feature
        _collection.Set<IHttpWebTransportFeature>(CreateHttp1Connection());
#pragma warning restore CA2252 // WebTransport is a preview feature

        CompareGenericGetterToIndexer();

        EachHttpProtocolFeatureSetAndUnique();
    }

    [Fact]
    public void Http1HasIHttpMinResponseDataRateFeature()
    {
        Assert.NotNull(_collection.Get<IHttpMinResponseDataRateFeature>());
    }

    [Fact]
    public void SetExtraFeatureAsNull()
    {
        _collection[typeof(string)] = null;
        Assert.Equal(0, _collection.Count(kv => kv.Key == typeof(string)));

        _collection[typeof(string)] = "A string";
        Assert.Equal(1, _collection.Count(kv => kv.Key == typeof(string)));

        _collection[typeof(string)] = null;
        Assert.Equal(0, _collection.Count(kv => kv.Key == typeof(string)));
    }

    private void CompareGenericGetterToIndexer()
    {
        Assert.Same(_collection.Get<IHttpRequestFeature>(), _collection[typeof(IHttpRequestFeature)]);
        Assert.Same(_collection.Get<IHttpRequestBodyDetectionFeature>(), _collection[typeof(IHttpRequestBodyDetectionFeature)]);
        Assert.Same(_collection.Get<IHttpResponseFeature>(), _collection[typeof(IHttpResponseFeature)]);
        Assert.Same(_collection.Get<IHttpResponseBodyFeature>(), _collection[typeof(IHttpResponseBodyFeature)]);
        Assert.Same(_collection.Get<IRequestBodyPipeFeature>(), _collection[typeof(IRequestBodyPipeFeature)]);
        Assert.Same(_collection.Get<IHttpRequestIdentifierFeature>(), _collection[typeof(IHttpRequestIdentifierFeature)]);
        Assert.Same(_collection.Get<IHttpRequestLifetimeFeature>(), _collection[typeof(IHttpRequestLifetimeFeature)]);
        Assert.Same(_collection.Get<IHttpConnectionFeature>(), _collection[typeof(IHttpConnectionFeature)]);
        Assert.Same(_collection.Get<IHttpMaxRequestBodySizeFeature>(), _collection[typeof(IHttpMaxRequestBodySizeFeature)]);
        Assert.Same(_collection.Get<IHttpMinRequestBodyDataRateFeature>(), _collection[typeof(IHttpMinRequestBodyDataRateFeature)]);
        Assert.Same(_collection.Get<IHttpMinResponseDataRateFeature>(), _collection[typeof(IHttpMinResponseDataRateFeature)]);
        Assert.Same(_collection.Get<IHttpBodyControlFeature>(), _collection[typeof(IHttpBodyControlFeature)]);
    }

    private int EachHttpProtocolFeatureSetAndUnique()
    {
        var featureCount = 0;
        foreach (var item in _collection)
        {
            var type = item.Key;
            if (type.IsAssignableFrom(typeof(HttpProtocol)))
            {
                var matches = _collection.Where(kv => ReferenceEquals(kv.Value, item.Value)).ToList();
                try
                {
                    Assert.Single(matches);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error for feature {type}.", ex);
                }

                featureCount++;
            }
        }

        Assert.NotEqual(0, featureCount);

        return featureCount;
    }

    public int GetFeaturesCount()
    {
        var featureCount = 0;
        foreach (var featureIter in _collection)
        {
            Type type = featureIter.Key;
            if (type.IsAssignableFrom(typeof(HttpProtocol)))
            {
                var featureLookup = _collection[type];
                Assert.Same(featureLookup, featureIter.Value);
                Assert.Same(featureLookup, _collection);
                featureCount++;
            }
        }
        return featureCount;
    }

    private int SetFeaturesToNonDefault()
    {
        int featureCount = 0;
        foreach (var feature in _collection)
        {
            Type type = feature.Key;
            if (type.IsAssignableFrom(typeof(HttpProtocol)))
            {
                _collection[type] = CreateHttp1Connection();
                featureCount++;
            }
        }

        var protocolFeaturesCount = EachHttpProtocolFeatureSetAndUnique();

        Assert.Equal(protocolFeaturesCount, featureCount);

        return featureCount;
    }

    private Http1Connection CreateHttp1Connection() => new TestHttp1Connection(_httpConnectionContext);
}
