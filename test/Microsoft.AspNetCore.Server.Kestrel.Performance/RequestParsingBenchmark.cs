// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]
    public class RequestParsingBenchmark
    {
        public IPipe Pipe { get; set; }

        public Frame<object> Frame { get; set; }

        public PipeFactory PipelineFactory { get; set; }

        [Setup]
        public void Setup()
        {
            var connectionContext = new MockConnection();
            connectionContext.ListenerContext.ServiceContext.HttpParserFactory = frame => new KestrelHttpParser(frame.ConnectionContext.ListenerContext.ServiceContext.Log);
            connectionContext.ListenerContext.ServiceContext.ServerOptions = new KestrelServerOptions();

            Frame = new Frame<object>(application: null, context: connectionContext);
            PipelineFactory = new PipeFactory();
            Pipe = PipelineFactory.Create();
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
            var buffer = Pipe.Writer.Alloc(2048);
            buffer.WriteFast(bytes);
            // There should not be any backpressure and task completes immediately
            buffer.FlushAsync().GetAwaiter().GetResult();
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

                Frame.Reset();

                if (!Frame.TakeStartLine(readableBuffer, out var consumed, out var examined))
                {
                    ErrorUtilities.ThrowInvalidRequestLine();
                }
                Pipe.Reader.Advance(consumed, examined);

                result = Pipe.Reader.ReadAsync().GetAwaiter().GetResult();
                readableBuffer = result.Buffer;

                Frame.InitializeHeaders();

                if (!Frame.TakeMessageHeaders(readableBuffer, out consumed, out examined))
                {
                    ErrorUtilities.ThrowInvalidRequestHeaders();
                }
                Pipe.Reader.Advance(consumed, examined);
            }
            while (true);
        }
    }
}
