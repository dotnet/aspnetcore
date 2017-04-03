// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    class TestInput : ITimeoutControl, IFrameControl, IDisposable
    {
        private MemoryPool _memoryPool;
        private PipeFactory _pipelineFactory;

        public TestInput()
        {
            var innerContext = new FrameContext { ServiceContext = new TestServiceContext() };

            FrameContext = new Frame<object>(null, innerContext);
            FrameContext.FrameControl = this;

            _memoryPool = new MemoryPool();
            _pipelineFactory = new PipeFactory();
            Pipe = _pipelineFactory.Create();
            FrameContext.Input = Pipe.Reader;
        }

        public IPipe Pipe { get;  }

        public Frame FrameContext { get; set; }

        public void Add(string text, bool fin = false)
        {
            var data = Encoding.ASCII.GetBytes(text);
            Pipe.Writer.WriteAsync(data).Wait();
            if (fin)
            {
                Pipe.Writer.Complete();
            }
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

        public void SetTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
        }

        public void ResetTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
        }

        public void CancelTimeout()
        {
        }

        public void Abort()
        {
        }

        public void Write(ArraySegment<byte> data, Action<Exception, object> callback, object state)
        {
        }

        void IFrameControl.ProduceContinue()
        {
        }

        void IFrameControl.Write(ArraySegment<byte> data)
        {
        }

        Task IFrameControl.WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            return TaskCache.CompletedTask;
        }

        void IFrameControl.Flush()
        {
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

