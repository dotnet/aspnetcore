// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class ResponseHeadersWritingBenchmark
    {
        private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: " + Constants.ServerName);
        private static readonly byte[] _helloWorldPayload = Encoding.ASCII.GetBytes("Hello, World!");

        private HttpResponseHeaders _responseHeaders;
        private IHeaderDictionary _responseHeadersDict;
        private DateHeaderValueManager _dateHeaderValueManager;
        private Writer _writer;

        private DateHeaderValueManager.DateHeaderValues DateHeaderValues => _dateHeaderValueManager.GetDateHeaderValues();

        [Params(
            BenchmarkTypes.TechEmpowerPlaintext,
            BenchmarkTypes.PlaintextChunked,
            BenchmarkTypes.PlaintextWithCookie,
            BenchmarkTypes.PlaintextChunkedWithCookie,
            BenchmarkTypes.LiveAspNet
        )]
        public BenchmarkTypes Type { get; set; }

        [Benchmark]
        public void Output()
        {
            switch (Type)
            {
                case BenchmarkTypes.TechEmpowerPlaintext:
                    TechEmpowerPlaintext();
                    break;
                case BenchmarkTypes.PlaintextChunked:
                    PlaintextChunked();
                    break;
                case BenchmarkTypes.PlaintextWithCookie:
                    PlaintextWithCookie();
                    break;
                case BenchmarkTypes.PlaintextChunkedWithCookie:
                    PlaintextChunkedWithCookie();
                    break;
                case BenchmarkTypes.LiveAspNet:
                    LiveAspNet();
                    break;
            }
        }

        private void TechEmpowerPlaintext()
        {
            var responseHeaders = _responseHeaders;
            responseHeaders.HeaderContentType = "text/plain";
            responseHeaders.ContentLength = _helloWorldPayload.Length;

            var writer = new BufferWriter<PipeWriter>(_writer);
            _responseHeaders.CopyTo(ref writer);
        }

        private void PlaintextChunked()
        {
            var responseHeaders = _responseHeaders;
            responseHeaders.HeaderContentType = "text/plain";

            var writer = new BufferWriter<PipeWriter>(_writer);
            _responseHeaders.CopyTo(ref writer);
        }

        private void LiveAspNet()
        {
            var responseHeaders = _responseHeaders;
            responseHeaders.HeaderContentEncoding = "gzip";
            responseHeaders.HeaderContentType = "text/html; charset=utf-8";
            _responseHeadersDict[HeaderNames.StrictTransportSecurity] = "max-age=31536000; includeSubdomains";
            responseHeaders.HeaderVary = "Accept-Encoding";
            _responseHeadersDict["X-Powered-By"] = "ASP.NET";

            var writer = new BufferWriter<PipeWriter>(_writer);
            _responseHeaders.CopyTo(ref writer);
        }

        private void PlaintextWithCookie()
        {
            var responseHeaders = _responseHeaders;
            responseHeaders.HeaderContentType = "text/plain";
            responseHeaders.HeaderSetCookie = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
            responseHeaders.ContentLength = _helloWorldPayload.Length;

            var writer = new BufferWriter<PipeWriter>(_writer);
            _responseHeaders.CopyTo(ref writer);
        }

        private void PlaintextChunkedWithCookie()
        {
            var responseHeaders = _responseHeaders;
            responseHeaders.HeaderContentType = "text/plain";
            responseHeaders.HeaderSetCookie = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
            responseHeaders.HeaderTransferEncoding = "chunked";

            var writer = new BufferWriter<PipeWriter>(_writer);
            _responseHeaders.CopyTo(ref writer);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _responseHeaders = new HttpResponseHeaders();
            _responseHeadersDict = _responseHeaders;
            _dateHeaderValueManager = new DateHeaderValueManager();
            _dateHeaderValueManager.OnHeartbeat(DateTimeOffset.Now);
            _writer = new Writer();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _responseHeaders.Reset();
            _responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
            _responseHeaders.SetRawDate(DateHeaderValues.String, DateHeaderValues.Bytes);
        }

        public class Writer : PipeWriter
        {
            private Memory<byte> _memory = new byte[4096 * 4];

            public override Memory<byte> GetMemory(int sizeHint = 0) => _memory;

            public override Span<byte> GetSpan(int sizeHint = 0) => _memory.Span;

            public override void Advance(int bytes) { }
            public override void CancelPendingFlush() { }
            public override void Complete(Exception exception = null)  { }
            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => default;
        }

        public enum BenchmarkTypes
        {
            TechEmpowerPlaintext,
            PlaintextChunked,
            PlaintextWithCookie,
            PlaintextChunkedWithCookie,
            LiveAspNet
        }
    }
}
