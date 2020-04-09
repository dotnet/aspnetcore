// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http1ConnectionBenchmark
    {
        private const int InnerLoopCount = 512;

        private readonly HttpParser<Adapter> _parser = new HttpParser<Adapter>();

        private ReadOnlySequence<byte> _buffer;

        internal Http1Connection Connection { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var memoryPool = SlabMemoryPoolFactory.Create();
            var options = new PipeOptions(memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            var pair = DuplexPipe.CreateConnectionPair(options, options);

            var serviceContext = new ServiceContext
            {
                ServerOptions = new KestrelServerOptions(),
                HttpParser = NullParser<Http1ParsingHandler>.Instance
            };

            var http1Connection = new Http1Connection(context: new HttpConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                MemoryPool = memoryPool,
                TimeoutControl = new TimeoutControl(timeoutHandler: null),
                Transport = pair.Transport
            });

            http1Connection.Reset();

            Connection = http1Connection;
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
        public void PlaintextTechEmpower()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.PlaintextTechEmpowerRequest);
                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
        public void LiveAspNet()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.LiveaspnetRequest);
                ParseData();
            }
        }

        private void InsertData(byte[] data)
        {
            _buffer = new ReadOnlySequence<byte>(data);
        }

        private void ParseData()
        {
            if (!_parser.ParseRequestLine(new Adapter(this), _buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }

            _buffer = _buffer.Slice(consumed, _buffer.End);
            var reader = new SequenceReader<byte>(_buffer);

            if (!_parser.ParseHeaders(new Adapter(this), ref reader))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }

            Connection.EnsureHostHeaderExists();

            Connection.Reset();
        }

        private struct Adapter : IHttpRequestLineHandler, IHttpHeadersHandler
        {
            public Http1ConnectionBenchmark RequestHandler;

            public Adapter(Http1ConnectionBenchmark requestHandler)
            {
                RequestHandler = requestHandler;
            }

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
                => RequestHandler.Connection.OnHeader(name, value);

            public void OnHeadersComplete(bool endStream)
                => RequestHandler.Connection.OnHeadersComplete();

            public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
                => RequestHandler.Connection.OnStartLine(method, version, target, path, query, customMethod, pathEncoded);

            public void OnStaticIndexedHeader(int index)
            {
                throw new NotImplementedException();
            }

            public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
