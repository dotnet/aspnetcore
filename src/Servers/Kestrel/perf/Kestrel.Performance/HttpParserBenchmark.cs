// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class HttpParserBenchmark : IHttpRequestLineHandler, IHttpHeadersHandler
    {
        private readonly HttpParser<Adapter> _parser = new HttpParser<Adapter>();

        private ReadOnlySequence<byte> _buffer;

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
        public void JsonTechEmpower()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.JsonTechEmpowerRequest);
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
            _buffer = new ReadOnlySequence<byte>(data);
        }

        private void ParseData()
        {
            var reader = new SequenceReader<byte>(_buffer);
            if (!_parser.ParseRequestLine(new Adapter(this), ref reader))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }

            if (!_parser.ParseHeaders(new Adapter(this), ref reader))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }

        public void OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
        {
        }

        public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
        }

        public void OnHeadersComplete(bool endStream)
        {
        }

        public void OnStaticIndexedHeader(int index)
        {
            throw new NotImplementedException();
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }

        private struct Adapter : IHttpRequestLineHandler, IHttpHeadersHandler
        {
            public HttpParserBenchmark RequestHandler;

            public Adapter(HttpParserBenchmark requestHandler)
            {
                RequestHandler = requestHandler;
            }

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
                => RequestHandler.OnHeader(name, value);

            public void OnHeadersComplete(bool endStream)
                => RequestHandler.OnHeadersComplete(endStream);

            public void OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
                => RequestHandler.OnStartLine(versionAndMethod, targetPath, startLine);

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
