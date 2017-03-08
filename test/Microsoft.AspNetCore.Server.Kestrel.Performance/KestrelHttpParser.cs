// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]

    public class KestrelHttpParser : IHttpRequestLineHandler, IHttpHeadersHandler
    {
        private readonly Internal.Http.KestrelHttpParser _parser = new Internal.Http.KestrelHttpParser(log: null);

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
            if (!_parser.ParseRequestLine(this, _buffer, out var consumed, out var examined))
            {
                RequestParsing.ThrowInvalidRequestHeaders();
            }

            _buffer = _buffer.Slice(consumed, _buffer.End);

            if (!_parser.ParseHeaders(this, _buffer, out consumed, out examined, out var consumedBytes))
            {
                RequestParsing.ThrowInvalidRequestHeaders();
            }
        }

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
        {
        }

        public void OnHeader(Span<byte> name, Span<byte> value)
        {
        }
    }
}
