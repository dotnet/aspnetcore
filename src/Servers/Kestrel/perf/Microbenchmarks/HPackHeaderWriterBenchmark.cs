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

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks
{
    public class HPackHeaderWriterBenchmark
    {
        private Http2HeadersEnumerator _http2HeadersEnumerator;
        private HPackEncoder _hpackEncoder;
        private HttpResponseHeaders _knownResponseHeaders;
        private HttpResponseHeaders _unknownResponseHeaders;
        private byte[] _buffer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _http2HeadersEnumerator = new Http2HeadersEnumerator();
            _hpackEncoder = new HPackEncoder();
            _buffer = new byte[1024 * 1024];

            _knownResponseHeaders = new HttpResponseHeaders
            {
                HeaderServer = "Kestrel",
                HeaderContentType = "application/json",
                HeaderDate = "Date!",
                HeaderContentLength = "0",
                HeaderAcceptRanges = "Ranges!",
                HeaderTransferEncoding = "Encoding!",
                HeaderVia = "Via!",
                HeaderVary = "Vary!",
                HeaderWWWAuthenticate = "Authenticate!",
                HeaderLastModified = "Modified!",
                HeaderExpires = "Expires!",
                HeaderAge = "Age!"
            };

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
