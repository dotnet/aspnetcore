// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private PipeFactory _pipelineFactory;

        public TestInput()
        {
            _memoryPool = new MemoryPool();
            _pipelineFactory = new PipeFactory();
            var pair = _pipelineFactory.CreateConnectionPair();
            Transport = pair.Transport;
            Application = pair.Application;

            FrameContext = new FrameContext
            {
                ServiceContext = new TestServiceContext(),
                ConnectionFeatures = new FeatureCollection(),
                Application = Application,
                Transport = Transport,
                PipeFactory = _pipelineFactory,
                TimeoutControl = Mock.Of<ITimeoutControl>()
            };

            Frame = new Frame<object>(null, FrameContext);
            Frame.FrameControl = Mock.Of<IFrameControl>();
        }

        public IPipeConnection Transport { get; }

        public IPipeConnection Application { get; }

        public PipeFactory PipeFactory => _pipelineFactory;

        public FrameContext FrameContext { get; }

        public Frame Frame { get; set; }

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
            _pipelineFactory.Dispose();
            _memoryPool.Dispose();
        }
    }
}

