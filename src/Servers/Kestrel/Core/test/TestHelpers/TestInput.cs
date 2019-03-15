// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    class TestInput : IDisposable
    {
        private MemoryPool<byte> _memoryPool;

        public TestInput()
        {
            _memoryPool = KestrelMemoryPool.Create();
            var options = new PipeOptions(pool: _memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            var pair = DuplexPipe.CreateConnectionPair(options, options);
            Transport = pair.Transport;
            Application = pair.Application;

            var connectionFeatures = new FeatureCollection();
            connectionFeatures.Set(Mock.Of<IConnectionLifetimeFeature>());

            Http1ConnectionContext = new HttpConnectionContext
            {
                ServiceContext = new TestServiceContext(),
                ConnectionContext = Mock.Of<ConnectionContext>(),
                ConnectionFeatures = connectionFeatures,
                Transport = Transport,
                MemoryPool = _memoryPool,
                TimeoutControl = Mock.Of<ITimeoutControl>()
            };

            Http1Connection = new Http1Connection(Http1ConnectionContext);
            Http1Connection.HttpResponseControl = Mock.Of<IHttpResponseControl>();
            Http1Connection.Reset();
        }

        public IDuplexPipe Transport { get; }

        public IDuplexPipe Application { get; }

        public HttpConnectionContext Http1ConnectionContext { get; }

        public Http1Connection Http1Connection { get; set; }

        public void Add(string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            async Task Write() => await Application.Output.WriteAsync(data);
            Write().Wait();
        }

        public void Fin()
        {
            Application.Output.Complete();
        }

        public void Cancel()
        {
            Transport.Input.CancelPendingRead();
        }

        public void Dispose()
        {
            Application.Input.Complete();
            Application.Output.Complete();
            Transport.Input.Complete();
            Transport.Output.Complete();
            _memoryPool.Dispose();
        }
    }
}

