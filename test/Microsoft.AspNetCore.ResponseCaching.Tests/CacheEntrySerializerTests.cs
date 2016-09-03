// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class CacheEntrySerializerTests
    {
        [Fact]
        public void Serialize_NullObject_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CacheEntrySerializer.Serialize(null));
        }

        [Fact]
        public void Serialize_UnknownObject_Throws()
        {
            Assert.Throws<NotSupportedException>(() => CacheEntrySerializer.Serialize(new object()));
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

            AssertCachedResponsesEqual(cachedEntry, (CachedResponse)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedEntry)));
        }

        [Fact]
        public void RoundTrip_Empty_CachedVaryRules_Succeeds()
        {
            var cachedVaryRules = new CachedVaryRules();

            AssertCachedVaryRulesEqual(cachedVaryRules, (CachedVaryRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryRules)));
        }

        [Fact]
        public void RoundTrip_CachedVaryRules_EmptyRules_Succeeds()
        {
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryRules = new VaryRules()
            };

            AssertCachedVaryRulesEqual(cachedVaryRules, (CachedVaryRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryRules)));
        }

        [Fact]
        public void RoundTrip_HeadersOnly_CachedVaryRules_Succeeds()
        {
            var headers = new[] { "headerA", "headerB" };
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryRules = new VaryRules()
                {
                    Headers = headers
                }
            };

            AssertCachedVaryRulesEqual(cachedVaryRules, (CachedVaryRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryRules)));
        }

        [Fact]
        public void RoundTrip_ParamsOnly_CachedVaryRules_Succeeds()
        {
            var param = new[] { "paramA", "paramB" };
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryRules = new VaryRules()
                {
                    Params = param
                }
            };

            AssertCachedVaryRulesEqual(cachedVaryRules, (CachedVaryRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryRules)));
        }

        [Fact]
        public void RoundTrip_HeadersAndParams_CachedVaryRules_Succeeds()
        {
            var headers = new[] { "headerA", "headerB" };
            var param = new[] { "paramA", "paramB" };
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryRules = new VaryRules()
                {
                    Headers = headers,
                    Params = param
                }
            };

            AssertCachedVaryRulesEqual(cachedVaryRules, (CachedVaryRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryRules)));
        }

        [Fact]
        public void Deserialize_InvalidEntries_ReturnsNull()
        {
            var headers = new[] { "headerA", "headerB" };
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryRules = new VaryRules()
                {
                    Headers = headers
                }
            };
            var serializedEntry = CacheEntrySerializer.Serialize(cachedVaryRules);
            Array.Reverse(serializedEntry);

            Assert.Null(CacheEntrySerializer.Deserialize(serializedEntry));
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

        private static void AssertCachedVaryRulesEqual(CachedVaryRules expected, CachedVaryRules actual)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            if (expected.VaryRules == null)
            {
                Assert.Null(actual.VaryRules);
            }
            else
            {
                Assert.NotNull(actual.VaryRules);
                Assert.Equal(expected.VaryRules.Headers, actual.VaryRules.Headers);
                Assert.Equal(expected.VaryRules.Params, actual.VaryRules.Params);
            }
        }
    }
}
