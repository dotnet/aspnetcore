// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http1ConnectionParsingOverheadBenchmark
    {
        private const int InnerLoopCount = 512;

        public ReadOnlySequence<byte> _buffer;
        private Http1Connection _http1Connection;

        [IterationSetup]
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

            var http1Connection = new Http1Connection(new HttpConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                MemoryPool = memoryPool,
                TimeoutControl = new TimeoutControl(timeoutHandler: null),
                Transport = pair.Transport
            });

            http1Connection.Reset();

            _http1Connection = http1Connection;
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = InnerLoopCount)]
        public void Http1ConnectionOverheadTotal()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                ParseRequest();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void Http1ConnectionOverheadRequestLine()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                ParseRequestLine();
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public void Http1ConnectionOverheadRequestHeaders()
        {
            for (var i = 0; i < InnerLoopCount; i++)
            {
                ParseRequestHeaders();
            }
        }

        private void ParseRequest()
        {
            _http1Connection.Reset();

            if (!_http1Connection.TakeStartLine(_buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestLine();
            }

            if (!_http1Connection.TakeMessageHeaders(_buffer, trailers: false, out consumed, out examined))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }

        private void ParseRequestLine()
        {
            _http1Connection.Reset();

            if (!_http1Connection.TakeStartLine(_buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestLine();
            }
        }

        private void ParseRequestHeaders()
        {
            _http1Connection.Reset();

            if (!_http1Connection.TakeMessageHeaders(_buffer, trailers: false, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }
    }
}
