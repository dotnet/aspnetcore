// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]
    public class FrameParsingOverheadBenchmark
    {
        private const int InnerLoopCount = 512;

        public ReadableBuffer _buffer;
        public Frame<object> _frame;

        [Setup]
        public void Setup()
        {
            var serviceContext = new ServiceContext
            {
                HttpParserFactory = _ => NullParser.Instance,
                ServerOptions = new KestrelServerOptions()
            };
            var frameContext = new FrameContext
            {
                ServiceContext = serviceContext,
                ConnectionInformation = new MockConnectionInformation()
            };

            _frame = new Frame<object>(application: null, frameContext: frameContext);
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = InnerLoopCount)]
        public void FrameOverheadTotal()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                ParseRequest();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void FrameOverheadRequestLine()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                ParseRequestLine();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void FrameOverheadRequestHeaders()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                ParseRequestHeaders();
            }
        }

        private void ParseRequest()
        {
            _frame.Reset();

            if (!_frame.TakeStartLine(_buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestLine();
            }

            _frame.InitializeHeaders();

            if (!_frame.TakeMessageHeaders(_buffer, out consumed, out examined))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }

        private void ParseRequestLine()
        {
            _frame.Reset();

            if (!_frame.TakeStartLine(_buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestLine();
            }
        }

        private void ParseRequestHeaders()
        {
            _frame.Reset();
            _frame.InitializeHeaders();

            if (!_frame.TakeMessageHeaders(_buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }

        private class NullParser : IHttpParser
        {
            private readonly byte[] _startLine = Encoding.ASCII.GetBytes("GET /plaintext HTTP/1.1\r\n");
            private readonly byte[] _target = Encoding.ASCII.GetBytes("/plaintext");
            private readonly byte[] _hostHeaderName = Encoding.ASCII.GetBytes("Host");
            private readonly byte[] _hostHeaderValue = Encoding.ASCII.GetBytes("www.example.com");
            private readonly byte[] _acceptHeaderName = Encoding.ASCII.GetBytes("Accept");
            private readonly byte[] _acceptHeaderValue = Encoding.ASCII.GetBytes("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7\r\n\r\n");
            private readonly byte[] _connectionHeaderName = Encoding.ASCII.GetBytes("Connection");
            private readonly byte[] _connectionHeaderValue = Encoding.ASCII.GetBytes("keep-alive");

            public static readonly NullParser Instance = new NullParser();

            public bool ParseHeaders<T>(T handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined, out int consumedBytes) where T : IHttpHeadersHandler
            {
                handler.OnHeader(new Span<byte>(_hostHeaderName), new Span<byte>(_hostHeaderValue));
                handler.OnHeader(new Span<byte>(_acceptHeaderName), new Span<byte>(_acceptHeaderValue));
                handler.OnHeader(new Span<byte>(_connectionHeaderName), new Span<byte>(_connectionHeaderValue));

                consumedBytes = 0;
                consumed = buffer.Start;
                examined = buffer.End;

                return true;
            }

            public bool ParseRequestLine<T>(T handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined) where T : IHttpRequestLineHandler
            {
                handler.OnStartLine(HttpMethod.Get,
                    HttpVersion.Http11,
                    new Span<byte>(_target),
                    new Span<byte>(_target),
                    Span<byte>.Empty,
                    Span<byte>.Empty,
                    false);

                consumed = buffer.Start;
                examined = buffer.End;

                return true;
            }

            public void Reset()
            {
            }
        }
    }
}
