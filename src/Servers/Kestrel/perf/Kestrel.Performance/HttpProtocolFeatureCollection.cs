// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class HttpProtocolFeatureCollection
    {
        private readonly IFeatureCollection _collection;

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IHttpRequestFeature GetViaTypeOf_First()
        {
            return (IHttpRequestFeature)_collection[typeof(IHttpRequestFeature)];
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IHttpRequestFeature GetViaGeneric_First()
        {
            return _collection.Get<IHttpRequestFeature>();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object GetViaTypeOf_Custom()
        {
            return (IHttpCustomFeature)_collection[typeof(IHttpCustomFeature)];
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object GetViaGeneric_Custom()
        {
            return _collection.Get<IHttpCustomFeature>();
        }


        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object GetViaTypeOf_NotFound()
        {
            return (IHttpNotFoundFeature)_collection[typeof(IHttpNotFoundFeature)];
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object GetViaGeneric_NotFound()
        {
            return _collection.Get<IHttpNotFoundFeature>();
        }

        public HttpProtocolFeatureCollection()
        {
            var memoryPool = SlabMemoryPoolFactory.Create();
            var options = new PipeOptions(memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
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
                MemoryPool = memoryPool,
                Transport = pair.Transport
            });

            http1Connection.Reset();

            _collection = http1Connection;
        }

        private interface IHttpCustomFeature
        {
        }

        private interface IHttpNotFoundFeature
        {
        }
    }

}
