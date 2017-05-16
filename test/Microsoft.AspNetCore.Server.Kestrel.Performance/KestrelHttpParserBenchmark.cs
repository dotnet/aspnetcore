// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]

    public class KestrelHttpParserBenchmark : IHttpRequestLineHandler, IHttpHeadersHandler
    {
        private readonly HttpParser<Adapter> _parser = new HttpParser<Adapter>();

        private ReadableBuffer _buffer;

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

        [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
        public void Unicode()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.UnicodeRequest);
                ParseData();
            }
        }

        private void InsertData(byte[] data)
        {
            _buffer = ReadableBuffer.Create(data);
        }

        private void ParseData()
        {
            if (!_parser.ParseRequestLine(new Adapter(this), _buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }

            _buffer = _buffer.Slice(consumed, _buffer.End);

            if (!_parser.ParseHeaders(new Adapter(this), _buffer, out consumed, out examined, out var consumedBytes))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
        {
        }

        public void OnHeader(Span<byte> name, Span<byte> value)
        {
        }

        private struct Adapter : IHttpRequestLineHandler, IHttpHeadersHandler
        {
            public KestrelHttpParserBenchmark RequestHandler;

            public Adapter(KestrelHttpParserBenchmark requestHandler)
            {
                RequestHandler = requestHandler;
            }

            public void OnHeader(Span<byte> name, Span<byte> value)
                => RequestHandler.OnHeader(name, value);

            public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
                => RequestHandler.OnStartLine(method, version, target, path, query, customMethod, pathEncoded);
        }
    }
}
