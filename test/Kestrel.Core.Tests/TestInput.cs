// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    class TestInput : IDisposable
    {
        private MemoryPool _memoryPool;

        public TestInput()
        {
            _memoryPool = new MemoryPool();
            var pair = PipeFactory.CreateConnectionPair(_memoryPool);
            Transport = pair.Transport;
            Application = pair.Application;

            Http1ConnectionContext = new Http1ConnectionContext
            {
                ServiceContext = new TestServiceContext(),
                ConnectionFeatures = new FeatureCollection(),
                Application = Application,
                Transport = Transport,
                BufferPool = _memoryPool,
                TimeoutControl = Mock.Of<ITimeoutControl>()
            };

            Http1Connection = new Http1Connection<object>(null, Http1ConnectionContext);
            Http1Connection.HttpResponseControl = Mock.Of<IHttpResponseControl>();
        }

        public IPipeConnection Transport { get; }

        public IPipeConnection Application { get; }

        public Http1ConnectionContext Http1ConnectionContext { get; }

        public Http1Connection Http1Connection { get; set; }

        public void Add(string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            Application.Output.WriteAsync(data).Wait();
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
            _memoryPool.Dispose();
        }
    }
}

