// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3HttpProtocolFeatureCollectionTests
    {
        private readonly IFeatureCollection _http3Collection;

        public Http3HttpProtocolFeatureCollectionTests()
        {
            var streamContext = TestContextFactory.CreateHttp3StreamContext(transport: DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions()).Application);

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
    }
}
