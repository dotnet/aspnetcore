// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    class TestInput : IDisposable
    {
        private MemoryPool _memoryPool;
        private PipeFactory _pipelineFactory;

        public TestInput()
        {
            _memoryPool = new MemoryPool();
            _pipelineFactory = new PipeFactory();
            Pipe = _pipelineFactory.Create();

            FrameContext = new FrameContext
            {
                ServiceContext = new TestServiceContext(),
                Input = Pipe.Reader,
                ConnectionInformation = new MockConnectionInformation
                {
                    PipeFactory = _pipelineFactory
                },
                TimeoutControl = Mock.Of<ITimeoutControl>()
            };

            Frame = new Frame<object>(null, FrameContext);
            Frame.FrameControl = Mock.Of<IFrameControl>();
        }

        public IPipe Pipe { get; }

        public PipeFactory PipeFactory => _pipelineFactory;

        public FrameContext FrameContext { get;  }

        public Frame Frame { get; set; }

        public void Add(string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            Pipe.Writer.WriteAsync(data).Wait();
        }

        public void Fin()
        {
            Pipe.Writer.Complete();
        }

        public void Cancel()
        {
            Pipe.Reader.CancelPendingRead();
        }

        public void Dispose()
        {
            _pipelineFactory.Dispose();
            _memoryPool.Dispose();
        }
    }
}

