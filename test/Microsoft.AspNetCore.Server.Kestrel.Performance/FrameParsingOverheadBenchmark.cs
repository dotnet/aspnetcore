// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Performance.Mocks;

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
                HttpParserFactory = _ => NullParser<FrameAdapter>.Instance,
                ServerOptions = new KestrelServerOptions()
            };
            var frameContext = new FrameContext
            {
                ServiceContext = serviceContext,
                ConnectionInformation = new MockConnectionInformation
                {
                    PipeFactory = new PipeFactory()
                },
                TimeoutControl = new MockTimeoutControl()
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

            if (!_frame.TakeMessageHeaders(_buffer, out var consumed, out var examined))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }
    }
}
