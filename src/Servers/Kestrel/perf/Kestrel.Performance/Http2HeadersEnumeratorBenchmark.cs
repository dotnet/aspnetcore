// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http2HeadersEnumeratorBenchmark
    {
        private Http2HeadersEnumerator _enumerator;
        private HttpResponseHeaders _knownSingleValueResponseHeaders;
        private HttpResponseHeaders _knownMultipleValueResponseHeaders;
        private HttpResponseHeaders _unknownSingleValueResponseHeaders;
        private HttpResponseHeaders _unknownMultipleValueResponseHeaders;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _knownSingleValueResponseHeaders = new HttpResponseHeaders
            {
                HeaderServer = "Value",
                HeaderDate = "Value",
                HeaderContentType = "Value",
                HeaderSetCookie = "Value"
            };

            _knownMultipleValueResponseHeaders = new HttpResponseHeaders
            {
                HeaderServer = new StringValues(new[] { "One", "Two" }),
                HeaderDate = new StringValues(new[] { "One", "Two" }),
                HeaderContentType = new StringValues(new[] { "One", "Two" }),
                HeaderSetCookie = new StringValues(new[] { "One", "Two" })
            };

            _unknownSingleValueResponseHeaders = new HttpResponseHeaders();
            _unknownSingleValueResponseHeaders.Append("One", "Value");
            _unknownSingleValueResponseHeaders.Append("Two", "Value");
            _unknownSingleValueResponseHeaders.Append("Three", "Value");
            _unknownSingleValueResponseHeaders.Append("Four", "Value");

            _unknownMultipleValueResponseHeaders = new HttpResponseHeaders();
            _unknownMultipleValueResponseHeaders.Append("One", new StringValues(new[] { "One", "Two" }));
            _unknownMultipleValueResponseHeaders.Append("Two", new StringValues(new[] { "One", "Two" }));
            _unknownMultipleValueResponseHeaders.Append("Three", new StringValues(new[] { "One", "Two" }));
            _unknownMultipleValueResponseHeaders.Append("Four", new StringValues(new[] { "One", "Two" }));

            _enumerator = new Http2HeadersEnumerator();
        }

        [Benchmark]
        public void KnownSingleValueResponseHeaders()
        {
            _enumerator.Initialize(_knownSingleValueResponseHeaders);

            if (_enumerator.MoveNext())
            {
            }
        }

        [Benchmark]
        public void KnownMultipleValueResponseHeaders()
        {
            _enumerator.Initialize(_knownMultipleValueResponseHeaders);

            if (_enumerator.MoveNext())
            {
            }
        }

        [Benchmark]
        public void UnknownSingleValueResponseHeaders()
        {
            _enumerator.Initialize(_unknownSingleValueResponseHeaders);

            if (_enumerator.MoveNext())
            {
            }
        }

        [Benchmark]
        public void UnknownMultipleValueResponseHeaders()
        {
            _enumerator.Initialize(_unknownMultipleValueResponseHeaders);

            if (_enumerator.MoveNext())
            {
            }
        }
    }
}
