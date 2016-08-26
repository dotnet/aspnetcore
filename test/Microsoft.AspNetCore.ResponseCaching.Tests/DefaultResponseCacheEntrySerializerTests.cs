// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class DefaultResponseCacheEntrySerializerTests
    {
        [Fact]
        public void Serialize_NullObject_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DefaultResponseCacheSerializer.Serialize(null));
        }

        [Fact]
        public void Serialize_UnknownObject_Throws()
        {
            Assert.Throws<NotSupportedException>(() => DefaultResponseCacheSerializer.Serialize(new object()));
        }

        [Fact]
        public void RoundTrip_CachedResponses_Succeeds()
        {
            var headers = new HeaderDictionary();
            headers["keyA"] = "valueA";
            headers["keyB"] = "valueB";
            var cachedEntry = new CachedResponse()
            {
                Created = DateTimeOffset.UtcNow,
                StatusCode = StatusCodes.Status200OK,
                Body = Encoding.ASCII.GetBytes("Hello world"),
                Headers = headers
            };

            AssertCachedResponsesEqual(cachedEntry, (CachedResponse)DefaultResponseCacheSerializer.Deserialize(DefaultResponseCacheSerializer.Serialize(cachedEntry)));
        }

        [Fact]
        public void RoundTrip_Empty_CachedVaryBy_Succeeds()
        {
            var cachedVaryBy = new CachedVaryBy();

            AssertCachedVarybyEqual(cachedVaryBy, (CachedVaryBy)DefaultResponseCacheSerializer.Deserialize(DefaultResponseCacheSerializer.Serialize(cachedVaryBy)));
        }

        [Fact]
        public void RoundTrip_HeadersOnly_CachedVaryBy_Succeeds()
        {
            var headers = new[] { "headerA", "headerB" };
            var cachedVaryBy = new CachedVaryBy()
            {
                Headers = headers
            };

            AssertCachedVarybyEqual(cachedVaryBy, (CachedVaryBy)DefaultResponseCacheSerializer.Deserialize(DefaultResponseCacheSerializer.Serialize(cachedVaryBy)));
        }

        [Fact]
        public void RoundTrip_ParamsOnly_CachedVaryBy_Succeeds()
        {
            var param = new[] { "paramA", "paramB" };
            var cachedVaryBy = new CachedVaryBy()
            {
                Params = param
            };

            AssertCachedVarybyEqual(cachedVaryBy, (CachedVaryBy)DefaultResponseCacheSerializer.Deserialize(DefaultResponseCacheSerializer.Serialize(cachedVaryBy)));
        }

        [Fact]
        public void RoundTrip_HeadersAndParams_CachedVaryBy_Succeeds()
        {
            var headers = new[] { "headerA", "headerB" };
            var param = new[] { "paramA", "paramB" };
            var cachedVaryBy = new CachedVaryBy()
            {
                Headers = headers,
                Params = param
            };

            AssertCachedVarybyEqual(cachedVaryBy, (CachedVaryBy)DefaultResponseCacheSerializer.Deserialize(DefaultResponseCacheSerializer.Serialize(cachedVaryBy)));
        }

        [Fact]
        public void Deserialize_InvalidEntries_ReturnsNull()
        {
            var headers = new[] { "headerA", "headerB" };
            var cachedVaryBy = new CachedVaryBy()
            {
                Headers = headers
            };
            var serializedEntry = DefaultResponseCacheSerializer.Serialize(cachedVaryBy);
            Array.Reverse(serializedEntry);

            Assert.Null(DefaultResponseCacheSerializer.Deserialize(serializedEntry));
        }

        private static void AssertCachedResponsesEqual(CachedResponse expected, CachedResponse actual)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.Equal(expected.Created, actual.Created);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.Headers.Count, actual.Headers.Count);
            foreach (var expectedHeader in expected.Headers)
            {
                Assert.Equal(expectedHeader.Value, actual.Headers[expectedHeader.Key]);
            }
            Assert.True(expected.Body.SequenceEqual(actual.Body));
        }

        private static void AssertCachedVarybyEqual(CachedVaryBy expected, CachedVaryBy actual)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.Equal(expected.Headers, actual.Headers);
            Assert.Equal(expected.Params, actual.Params);
        }
    }
}
