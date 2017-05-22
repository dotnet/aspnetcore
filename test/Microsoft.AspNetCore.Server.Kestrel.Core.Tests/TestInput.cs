// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    class TestInput : IFrameControl, IDisposable
    {
        private MemoryPool _memoryPool;
        private PipeFactory _pipelineFactory;

        public TestInput()
        {
            _memoryPool = new MemoryPool();
            _pipelineFactory = new PipeFactory();
            Pipe = _pipelineFactory.Create();

            FrameContext = new Frame<object>(null, new FrameContext
            {
                ServiceContext = new TestServiceContext(),
                Input = Pipe.Reader,
                ConnectionInformation = new MockConnectionInformation
                {
                    PipeFactory = _pipelineFactory
                }
            });
            FrameContext.FrameControl = this;
        }

        public IPipe Pipe { get; }

        public PipeFactory PipeFactory => _pipelineFactory;

        public Frame FrameContext { get; set; }

        public void Add(string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            Pipe.Writer.WriteAsync(data).Wait();
        }

        public void Fin()
        {
            Pipe.Writer.Complete();
        }

        public void ProduceContinue()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void End(ProduceEndType endType)
        {
        }

        public void Abort()
        {
        }

        public void Write(ArraySegment<byte> data, Action<Exception, object> callback, object state)
        {
        }

        Task IFrameControl.WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            return TaskCache.CompletedTask;
        }

        Task IFrameControl.FlushAsync(CancellationToken cancellationToken)
        {
            return TaskCache.CompletedTask;
        }

        public void Dispose()
        {
            _pipelineFactory.Dispose();
            _memoryPool.Dispose();
        }
    }
}

