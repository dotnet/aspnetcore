// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2HeadersEnumeratorTests
    {
        [Fact]
        public void CanIterateOverResponseHeaders()
        {
            var responseHeaders = new HttpResponseHeaders
            {
                ContentLength = 9,
                HeaderAcceptRanges = "AcceptRanges!",
                HeaderAge = new StringValues(new[] { "1", "2" }),
                HeaderDate = "Date!"
            };
            responseHeaders.Append("Name1", "Value1");
            responseHeaders.Append("Name2", "Value2-1");
            responseHeaders.Append("Name2", "Value2-2");
            responseHeaders.Append("Name3", "Value3");

            var e = new Http2HeadersEnumerator();
            e.Initialize(responseHeaders);

            var headers = GetNormalizedHeaders(e);

            Assert.Equal(new[]
            {
                new KeyValuePair<string, string>("Date", "Date!"),
                new KeyValuePair<string, string>("Accept-Ranges", "AcceptRanges!"),
                new KeyValuePair<string, string>("Age", "1"),
                new KeyValuePair<string, string>("Age", "2"),
                new KeyValuePair<string, string>("Content-Length", "9"),
                new KeyValuePair<string, string>("Name1", "Value1"),
                new KeyValuePair<string, string>("Name2", "Value2-1"),
                new KeyValuePair<string, string>("Name2", "Value2-2"),
                new KeyValuePair<string, string>("Name3", "Value3"),
            }, headers);
        }

        [Fact]
        public void CanIterateOverResponseTrailers()
        {
            var responseHeaders = new HttpResponseTrailers
            {
                ContentLength = 9,
                HeaderETag = "ETag!"
            };
            responseHeaders.Append("Name1", "Value1");
            responseHeaders.Append("Name2", "Value2-1");
            responseHeaders.Append("Name2", "Value2-2");
            responseHeaders.Append("Name3", "Value3");

            var e = new Http2HeadersEnumerator();
            e.Initialize(responseHeaders);

            var headers = GetNormalizedHeaders(e);

            Assert.Equal(new[]
            {
                new KeyValuePair<string, string>("ETag", "ETag!"),
                new KeyValuePair<string, string>("Name1", "Value1"),
                new KeyValuePair<string, string>("Name2", "Value2-1"),
                new KeyValuePair<string, string>("Name2", "Value2-2"),
                new KeyValuePair<string, string>("Name3", "Value3"),
            }, headers);
        }

        private KeyValuePair<string, string>[] GetNormalizedHeaders(Http2HeadersEnumerator enumerator)
        {
            var headers = new List<KeyValuePair<string, string>>();
            while (enumerator.MoveNext())
            {
                headers.Add(enumerator.Current);
            }
            return headers.ToArray();
        }
    }
}
