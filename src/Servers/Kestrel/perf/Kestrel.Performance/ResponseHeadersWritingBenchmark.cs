// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class ResponseHeadersWritingBenchmark
    {
        private static readonly byte[] _helloWorldPayload = Encoding.ASCII.GetBytes("Hello, World!");

        private TestHttp1Connection _http1Connection;

        private MemoryPool<byte> _memoryPool;

        private DuplexPipe.DuplexPipePair _pair;

        [Params(
            BenchmarkTypes.TechEmpowerPlaintext,
            BenchmarkTypes.PlaintextChunked,
            BenchmarkTypes.PlaintextWithCookie,
            BenchmarkTypes.PlaintextChunkedWithCookie,
            BenchmarkTypes.LiveAspNet
        )]
        public BenchmarkTypes Type { get; set; }

        [Benchmark]
        public async Task Output()
        {
            _http1Connection.Reset();
            _http1Connection.StatusCode = 200;
            _http1Connection.HttpVersionEnum = HttpVersion.Http11;
            _http1Connection.KeepAlive = true;

            Task writeTask = Task.CompletedTask;
            switch (Type)
            {
                case BenchmarkTypes.TechEmpowerPlaintext:
                    writeTask = TechEmpowerPlaintext();
                    break;
                case BenchmarkTypes.PlaintextChunked:
                    writeTask = PlaintextChunked();
                    break;
                case BenchmarkTypes.PlaintextWithCookie:
                    writeTask = PlaintextWithCookie();
                    break;
                case BenchmarkTypes.PlaintextChunkedWithCookie:
                    writeTask = PlaintextChunkedWithCookie();
                    break;
                case BenchmarkTypes.LiveAspNet:
                    writeTask = LiveAspNet();
                    break;
            }

            await writeTask;
            await _http1Connection.ProduceEndAsync();
        }

        private Task TechEmpowerPlaintext()
        {
            var responseHeaders = _http1Connection.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            responseHeaders.ContentLength = _helloWorldPayload.Length;
            return _http1Connection.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task PlaintextChunked()
        {
            var responseHeaders = _http1Connection.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            return _http1Connection.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task LiveAspNet()
        {
            var responseHeaders = _http1Connection.ResponseHeaders;
            responseHeaders["Content-Encoding"] = "gzip";
            responseHeaders["Content-Type"] = "text/html; charset=utf-8";
            responseHeaders["Strict-Transport-Security"] = "max-age=31536000; includeSubdomains";
            responseHeaders["Vary"] = "Accept-Encoding";
            responseHeaders["X-Powered-By"] = "ASP.NET";
            return _http1Connection.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task PlaintextWithCookie()
        {
            var responseHeaders = _http1Connection.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            responseHeaders["Set-Cookie"] = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
            responseHeaders.ContentLength = _helloWorldPayload.Length;
            return _http1Connection.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        private Task PlaintextChunkedWithCookie()
        {
            var responseHeaders = _http1Connection.ResponseHeaders;
            responseHeaders["Content-Type"] = "text/plain";
            responseHeaders["Set-Cookie"] = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
            return _http1Connection.WriteAsync(new ArraySegment<byte>(_helloWorldPayload), default(CancellationToken));
        }

        [IterationSetup]
        public void Setup()
        {
            _memoryPool = KestrelMemoryPool.Create();
            var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            _pair = DuplexPipe.CreateConnectionPair(options, options);

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = new MockTrace(),
                HttpParser = new HttpParser<Http1ParsingHandler>()
            };

            var http1Connection = new TestHttp1Connection(new HttpConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                MemoryPool = _memoryPool,
                TimeoutControl = new TimeoutControl(timeoutHandler: null),
                Transport = _pair.Transport
            });

            http1Connection.Reset();
            serviceContext.DateHeaderValueManager.OnHeartbeat(DateTimeOffset.UtcNow);

            _http1Connection = http1Connection;
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _pair.Application.Input.Complete();
            _pair.Application.Output.Complete();
            _pair.Transport.Input.Complete();
            _pair.Transport.Output.Complete();
            _memoryPool.Dispose();
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
