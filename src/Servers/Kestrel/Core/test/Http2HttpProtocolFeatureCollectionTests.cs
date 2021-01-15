// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
    public class Http2HttpProtocolFeatureCollectionTests
    {
        private readonly IFeatureCollection _http2Collection;

        public Http2HttpProtocolFeatureCollectionTests()
        {
            var context = TestContextFactory.CreateHttp2StreamContext(
                serviceContext: new TestServiceContext(),
                timeoutControl: Mock.Of<ITimeoutControl>());

            var http2Stream = new TestHttp2Stream(context);
            http2Stream.Reset();
            _http2Collection = http2Stream;
        }

        [Fact]
        public void Http2StreamFeatureCollectionDoesNotIncludeIHttpMinResponseDataRateFeature()
        {
            Assert.Null(_http2Collection.Get<IHttpMinResponseDataRateFeature>());
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
