// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    class TestInput : IConnectionControl, IFrameControl, IDisposable
    {
        private MemoryPool _memoryPool;

        public TestInput()
        {
            var trace = new KestrelTrace(new TestKestrelTrace());
            var ltp = new LoggingThreadPool(trace);
            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions()
            };
            var listenerContext = new ListenerContext(serviceContext)
            {
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000")
            };
            var connectionContext = new ConnectionContext(listenerContext)
            {
                ConnectionControl = this
            };
            var context = new Frame<object>(null, connectionContext);
            FrameContext = context;
            FrameContext.FrameControl = this;
            FrameContext.ConnectionContext.ListenerContext.ServiceContext.Log = trace;

            _memoryPool = new MemoryPool();
            FrameContext.SocketInput = new SocketInput(_memoryPool, ltp);
        }

        public Frame FrameContext { get; set; }

        public void Add(string text, bool fin = false)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(text);
            FrameContext.SocketInput.IncomingData(data, 0, data.Length);
            if (fin)
            {
                FrameContext.SocketInput.IncomingFin();
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
            FrameContext.SocketInput.Dispose();
            _memoryPool.Dispose();
        }
    }
}

