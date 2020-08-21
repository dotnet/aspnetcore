// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpProtocolFeatureCollectionTests
    {
        private readonly TestHttp1Connection _http1Connection;
        private readonly HttpConnectionContext _httpConnectionContext;
        private readonly IFeatureCollection _collection;
        private readonly IFeatureCollection _http2Collection;

        public HttpProtocolFeatureCollectionTests()
        {
            var context = new Http2StreamContext
            {
                ServiceContext = new TestServiceContext(),
                ConnectionFeatures = new FeatureCollection(),
                TimeoutControl = Mock.Of<ITimeoutControl>(),
                Transport = Mock.Of<IDuplexPipe>(),
                ServerPeerSettings = new Http2PeerSettings(),
                ClientPeerSettings = new Http2PeerSettings(),
            };

            _httpConnectionContext = context;
            _http1Connection = new TestHttp1Connection(context);
            _http1Connection.Reset();
            _collection = _http1Connection;

            var http2Stream = new TestHttp2Stream(context);
            http2Stream.Reset();
            _http2Collection = http2Stream;
        }

        [Fact]
        public int FeaturesStartAsSelf()
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

            Assert.NotEqual(0, featureCount);

            return featureCount;
        }

        [Fact]
        public int FeaturesCanBeAssignedTo()
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

            return featureCount;
        }

        [Fact]
        public void FeaturesResetToSelf()
        {
            var featuresAssigned = SetFeaturesToNonDefault();
            _http1Connection.ResetFeatureCollection();
            var featuresReset = FeaturesStartAsSelf();

            Assert.Equal(featuresAssigned, featuresReset);
        }

        [Fact]
        public void FeaturesByGenericSameAsByType()
        {
            var featuresAssigned = SetFeaturesToNonDefault();

            CompareGenericGetterToIndexer();

            _http1Connection.ResetFeatureCollection();
            var featuresReset = FeaturesStartAsSelf();

            Assert.Equal(featuresAssigned, featuresReset);
        }

        [Fact]
        public void FeaturesSetByTypeSameAsGeneric()
        {
            _collection[typeof(IHttpRequestFeature)] = CreateHttp1Connection();
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

            CompareGenericGetterToIndexer();

            EachHttpProtocolFeatureSetAndUnique();
        }

        [Fact]
        public void FeaturesSetByGenericSameAsByType()
        {
            _collection.Set<IHttpRequestFeature>(CreateHttp1Connection());
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

            CompareGenericGetterToIndexer();

            EachHttpProtocolFeatureSetAndUnique();
        }

        [Fact]
        public void Http2StreamFeatureCollectionDoesNotIncludeIHttpMinResponseDataRateFeature()
        {
            Assert.Null(_http2Collection.Get<IHttpMinResponseDataRateFeature>());
            Assert.NotNull(_collection.Get<IHttpMinResponseDataRateFeature>());
        }

        [Fact]
        public void Http2StreamFeatureCollectionDoesIncludeUpgradeFeature()
        {
            var upgradeFeature = _http2Collection.Get<IHttpUpgradeFeature>();

            Assert.NotNull(upgradeFeature);
            Assert.False(upgradeFeature.IsUpgradableRequest);
        }

        [Fact]
        public void Http2StreamFeatureCollectionDoesIncludeIHttpMinRequestBodyDataRateFeature()
        {
            var minRateFeature = _http2Collection.Get<IHttpMinRequestBodyDataRateFeature>();

            Assert.NotNull(minRateFeature);

            Assert.Throws<NotSupportedException>(() => minRateFeature.MinDataRate);
            Assert.Throws<NotSupportedException>(() => minRateFeature.MinDataRate = new MinDataRate(1, TimeSpan.FromSeconds(2)));

            // You can set the MinDataRate to null though.
            minRateFeature.MinDataRate = null;

            // But you still cannot read the property;
            Assert.Throws<NotSupportedException>(() => minRateFeature.MinDataRate);
        }

        private void CompareGenericGetterToIndexer()
        {
            Assert.Same(_collection.Get<IHttpRequestFeature>(), _collection[typeof(IHttpRequestFeature)]);
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
            int featureCount = 0;
            foreach (var item in _collection)
            {
                Type type = item.Key;
                if (type.IsAssignableFrom(typeof(HttpProtocol)))
                {
                    Assert.Equal(1, _collection.Count(kv => ReferenceEquals(kv.Value, item.Value)));

                    featureCount++;
                }
            }

            Assert.NotEqual(0, featureCount);

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

        private class TestHttp2Stream : Http2Stream
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
}
