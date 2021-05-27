// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http.HPack;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks
{
    public class HPackHeaderWriterBenchmark
    {
        private Http2HeadersEnumerator _http2HeadersEnumerator;
        private DynamicHPackEncoder _hpackEncoder;
        private IHeaderDictionary _knownResponseHeaders;
        private IHeaderDictionary _unknownResponseHeaders;
        private byte[] _buffer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _http2HeadersEnumerator = new Http2HeadersEnumerator();
            _hpackEncoder = new DynamicHPackEncoder();
            _buffer = new byte[1024 * 1024];

            _knownResponseHeaders = new HttpResponseHeaders();

            _knownResponseHeaders.Server = "Kestrel";
            _knownResponseHeaders.ContentType = "application/json";
            _knownResponseHeaders.Date = "Date!";
            _knownResponseHeaders.ContentLength = 0;
            _knownResponseHeaders.AcceptRanges = "Ranges!";
            _knownResponseHeaders.TransferEncoding = "Encoding!";
            _knownResponseHeaders.Via = "Via!";
            _knownResponseHeaders.Vary = "Vary!";
            _knownResponseHeaders.WWWAuthenticate = "Authenticate!";
            _knownResponseHeaders.LastModified = "Modified!";
            _knownResponseHeaders.Expires = "Expires!";
            _knownResponseHeaders.Age = "Age!";

            _unknownResponseHeaders = new HttpResponseHeaders();
            for (var i = 0; i < 10; i++)
            {
                _unknownResponseHeaders.Append("Unknown" + i, "Value" + i);
            }
        }

        [Benchmark]
        public void BeginEncodeHeaders_KnownHeaders()
        {
            _http2HeadersEnumerator.Initialize(_knownResponseHeaders);
            HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _http2HeadersEnumerator, _buffer, out _);
        }

        [Benchmark]
        public void BeginEncodeHeaders_UnknownHeaders()
        {
            _http2HeadersEnumerator.Initialize(_unknownResponseHeaders);
            HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _http2HeadersEnumerator, _buffer, out _);
        }
    }
}
