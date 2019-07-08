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
    public class RequestParsingBenchmark
    {
        private MemoryPool<byte> _memoryPool;

        public Pipe Pipe { get; set; }

        internal Http1Connection Http1Connection { get; set; }

        [IterationSetup]
        public void Setup()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();
            var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            var pair = DuplexPipe.CreateConnectionPair(options, options);

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = new MockTrace(),
                HttpParser = new HttpParser<Http1ParsingHandler>()
            };

            var http1Connection = new Http1Connection(new HttpConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                MemoryPool = _memoryPool,
                Transport = pair.Transport,
                TimeoutControl = new TimeoutControl(timeoutHandler: null)
            });

            http1Connection.Reset();

            Http1Connection = http1Connection;
            Pipe = new Pipe(new PipeOptions(_memoryPool));
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
        public void PlaintextAbsoluteUri()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.PlaintextAbsoluteUriRequest);
                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
        public void PipelinedPlaintextTechEmpower()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.PlaintextTechEmpowerPipelinedRequests);
                ParseData();
            }
        }

        [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
        public void PipelinedPlaintextTechEmpowerDrainBuffer()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.PlaintextTechEmpowerPipelinedRequests);
                ParseDataDrainBuffer();
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

        [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
        public void PipelinedLiveAspNet()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.LiveaspnetPipelinedRequests);
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

        [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
        public void UnicodePipelined()
        {
            for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
            {
                InsertData(RequestParsingData.UnicodePipelinedRequests);
                ParseData();
            }
        }

        private void InsertData(byte[] bytes)
        {
            Pipe.Writer.Write(bytes);
            // There should not be any backpressure and task completes immediately
            Pipe.Writer.FlushAsync().GetAwaiter().GetResult();
        }

        private void ParseDataDrainBuffer()
        {
            var awaitable = Pipe.Reader.ReadAsync();
            if (!awaitable.IsCompleted)
            {
                // No more data
                return;
            }

            var readableBuffer = awaitable.GetAwaiter().GetResult().Buffer;
            do
            {
                Http1Connection.Reset();

                if (!Http1Connection.TakeStartLine(readableBuffer, out var consumed, out var examined))
                {
                    ErrorUtilities.ThrowInvalidRequestLine();
                }

                readableBuffer = readableBuffer.Slice(consumed);

                if (!Http1Connection.TakeMessageHeaders(readableBuffer, trailers: false, out consumed, out examined))
                {
                    ErrorUtilities.ThrowInvalidRequestHeaders();
                }

                readableBuffer = readableBuffer.Slice(consumed);
            }
            while (readableBuffer.Length > 0);

            Pipe.Reader.AdvanceTo(readableBuffer.End);
        }

        private void ParseData()
        {
            do
            {
                var awaitable = Pipe.Reader.ReadAsync();
                if (!awaitable.IsCompleted)
                {
                    // No more data
                    return;
                }

                var result = awaitable.GetAwaiter().GetResult();
                var readableBuffer = result.Buffer;

                Http1Connection.Reset();

                if (!Http1Connection.TakeStartLine(readableBuffer, out var consumed, out var examined))
                {
                    ErrorUtilities.ThrowInvalidRequestLine();
                }
                Pipe.Reader.AdvanceTo(consumed, examined);

                result = Pipe.Reader.ReadAsync().GetAwaiter().GetResult();
                readableBuffer = result.Buffer;

                if (!Http1Connection.TakeMessageHeaders(readableBuffer, trailers: false, out consumed, out examined))
                {
                    ErrorUtilities.ThrowInvalidRequestHeaders();
                }
                Pipe.Reader.AdvanceTo(consumed, examined);
            }
            while (true);
        }


        [IterationCleanup]
        public void Cleanup()
        {
            _memoryPool.Dispose();
        }
    }
}
