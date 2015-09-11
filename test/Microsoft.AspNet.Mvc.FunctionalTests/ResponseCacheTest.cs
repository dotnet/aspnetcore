// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ResponseCacheTest : IClassFixture<MvcTestFixture<ResponseCacheWebSite.Startup>>
    {
        public ResponseCacheTest(MvcTestFixture<ResponseCacheWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ResponseCache_SetsAllHeaders()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheHeaders/Index");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=100", data);
            data = Assert.Single(response.Headers.GetValues("Vary"));
            Assert.Equal("Accept", data);
        }

        public static IEnumerable<object[]> CacheControlData
        {
            get
            {
                yield return new object[] { "http://localhost/CacheHeaders/PublicCache", "public, max-age=100" };
                yield return new object[] { "http://localhost/CacheHeaders/ClientCache", "max-age=100, private" };
                yield return new object[] { "http://localhost/CacheHeaders/NoStore", "no-store" };
                yield return new object[] { "http://localhost/CacheHeaders/NoCacheAtAll", "no-store, no-cache" };
            }
        }

        [Theory]
        [MemberData(nameof(CacheControlData))]
        public async Task ResponseCache_SetsDifferentCacheControlHeaders(string url, string expected)
        {
            // Arrange & Act
            var response = await Client.GetAsync(url);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals(expected, data);
        }

        [Fact]
        public async Task SetsHeadersForAllActionsOfClass()
        {
            // Arrange & Act
            var response1 = await Client.GetAsync("http://localhost/ClassLevelCache/GetHelloWorld");
            var response2 = await Client.GetAsync("http://localhost/ClassLevelCache/GetFooBar");

            // Assert
            var data = Assert.Single(response1.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=100", data);
            data = Assert.Single(response1.Headers.GetValues("Vary"));
            Assert.Equal("Accept", data);

            data = Assert.Single(response2.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=100", data);
            data = Assert.Single(response2.Headers.GetValues("Vary"));
            Assert.Equal("Accept", data);
        }

        [Fact]
        public async Task HeadersSetInActionOverridesTheOnesInClass()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ClassLevelCache/ConflictExistingHeader");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=20", data);
        }

        [Fact]
        public async Task HeadersToNotCacheAParticularAction()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ClassLevelCache/DoNotCacheThisAction");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("no-store, no-cache", data);
        }

        [Fact]
        public async Task ClassLevelHeadersAreUnsetByActionLevelHeaders()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ClassLevelNoStore/CacheThisAction");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Vary"));
            Assert.Equal("Accept", data);
            data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=10", data);
            IEnumerable<string> pragmaValues;
            response.Headers.TryGetValues("Pragma", out pragmaValues);
            Assert.Null(pragmaValues);
        }

        [Fact]
        public async Task SetsCacheControlPublicByDefault()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheHeaders/SetsCacheControlPublicByDefault");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=40", data);
        }

        [Fact]
        public async Task ThrowsWhenDurationIsNotSet()
        {
            // Arrange & Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => Client.GetAsync("http://localhost/CacheHeaders/ThrowsWhenDurationIsNotSet"));
            Assert.Equal(
                "If the 'NoStore' property is not set to true, 'Duration' property must be specified.",
                ex.Message);
        }

        // Cache Profiles
        [Fact]
        public async Task ResponseCache_SetsAllHeaders_FromCacheProfile()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfiles/PublicCache30Sec");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=30", data);
        }

        [Fact]
        public async Task ResponseCache_SetsAllHeaders_ChosesTheRightProfile()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfiles/PrivateCache30Sec");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("max-age=30, private", data);
        }

        [Fact]
        public async Task ResponseCache_SetsNoCacheHeaders()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfiles/NoCache");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("no-store, no-cache", data);
            data = Assert.Single(response.Headers.GetValues("Pragma"));
            Assert.Equal("no-cache", data);
        }

        [Fact]
        public async Task ResponseCache_AddsHeaders()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfiles/CacheProfileAddParameter");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=30", data);
            data = Assert.Single(response.Headers.GetValues("Vary"));
            Assert.Equal("Accept", data);
        }

        [Fact]
        public async Task ResponseCache_ModifiesHeaders()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfiles/CacheProfileOverride");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=10", data);
        }

        [Fact]
        public async Task ResponseCache_FallbackToFilter_IfNoAttribute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfiles/FallbackToFilter");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            Assert.Equal("no-store", data);
            data = Assert.Single(response.Headers.GetValues("Vary"));
            Assert.Equal("TestDefault", data);
        }

        [Fact]
        public async Task ResponseCacheAttribute_OnAction_OverridesTheValuesOnClass()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ClassLevelNoStore/CacheThisActionWithProfileSettings");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Vary"));
            Assert.Equal("Accept", data);
            data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=30", data);
            IEnumerable<string> pragmaValues;
            response.Headers.TryGetValues("Pragma", out pragmaValues);
            Assert.Null(pragmaValues);
        }

        // Cache profile overrides
        [Fact]
        public async Task ResponseCacheAttribute_OverridesProfileDuration_FromAttributeProperty()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfileOverrides/PublicCache30SecTo15Sec");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=15", data);
        }

        [Fact]
        public async Task ResponseCacheAttribute_OverridesProfileLocation_FromAttributeProperty()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfileOverrides/PublicCache30SecToPrivateCache");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("max-age=30, private", data);
        }

        [Fact]
        public async Task ResponseCacheAttribute_OverridesProfileNoStore_FromAttributeProperty()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CacheProfileOverrides/PublicCache30SecToNoStore");

            // Assert
            var data = Assert.Single(response.Headers.GetValues("Cache-control"));
            Assert.Equal("no-store", data);
        }

        [Fact]
        public async Task ResponseCacheAttribute_OverridesProfileVaryBy_FromAttributeProperty()
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/CacheProfileOverrides/PublicCache30SecWithVaryByAcceptToVaryByTest");

            // Assert
            var cacheControl = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=30", cacheControl);
            var vary = Assert.Single(response.Headers.GetValues("Vary"));
            Assert.Equal("Test", vary);
        }

        [Fact]
        public async Task ResponseCacheAttribute_OverridesProfileVaryBy_FromAttributeProperty_AndRemovesVaryHeader()
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/CacheProfileOverrides/PublicCache30SecWithVaryByAcceptToVaryByNone");

            // Assert
            var cacheControl = Assert.Single(response.Headers.GetValues("Cache-control"));
            AssertHeaderEquals("public, max-age=30", cacheControl);
            Assert.Throws<InvalidOperationException>(() => response.Headers.GetValues("Vary"));
        }

        private void AssertHeaderEquals(string expected, string actual)
        {
            // OrderBy is used because the order of the results may very depending on the platform / client.
            Assert.Equal(
                expected.Split(',').Select(p => p.Trim()).OrderBy(item => item, StringComparer.Ordinal),
                actual.Split(',').Select(p => p.Trim()).OrderBy(item => item, StringComparer.Ordinal));
        }
    }
}