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
        public void SerializeNullObjectThrows()
        {
            Assert.Throws<ArgumentNullException>(() => DefaultResponseCacheSerializer.Serialize(null));
        }

        [Fact]
        public void SerializeUnknownObjectThrows()
        {
            Assert.Throws<NotSupportedException>(() => DefaultResponseCacheSerializer.Serialize(new object()));
        }

        [Fact]
        public void RoundTripCachedResponsesSucceeds()
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
        public void RoundTripCachedVaryBySucceeds()
        {
            var headers = new[] { "headerA", "headerB" };
            var cachedVaryBy = new CachedVaryBy()
            {
                Headers = headers
            };

            AssertCachedVarybyEqual(cachedVaryBy, (CachedVaryBy)DefaultResponseCacheSerializer.Deserialize(DefaultResponseCacheSerializer.Serialize(cachedVaryBy)));
        }


        [Fact]
        public void DeserializeInvalidEntriesReturnsNull()
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
        }
    }
}
