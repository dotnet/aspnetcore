// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [ParameterizedJobConfig(typeof(CoreConfig))]
    public class HttpProtocolFeatureCollection
    {
        private readonly Http1Connection _http1Connection;
        private IFeatureCollection _collection;

        [Benchmark(Baseline = true)]
        public IHttpRequestFeature GetFirstViaFastFeature()
        {
            return (IHttpRequestFeature)GetFastFeature(typeof(IHttpRequestFeature));
        }

        [Benchmark]
        public IHttpRequestFeature GetFirstViaType()
        {
            return (IHttpRequestFeature)Get(typeof(IHttpRequestFeature));
        }

        [Benchmark]
        public IHttpRequestFeature GetFirstViaExtension()
        {
            return _collection.GetType<IHttpRequestFeature>();
        }

        [Benchmark]
        public IHttpRequestFeature GetFirstViaGeneric()
        {
            return _collection.Get<IHttpRequestFeature>();
        }

        [Benchmark]
        public IHttpSendFileFeature GetLastViaFastFeature()
        {
            return (IHttpSendFileFeature)GetFastFeature(typeof(IHttpSendFileFeature));
        }

        [Benchmark]
        public IHttpSendFileFeature GetLastViaType()
        {
            return (IHttpSendFileFeature)Get(typeof(IHttpSendFileFeature));
        }

        [Benchmark]
        public IHttpSendFileFeature GetLastViaExtension()
        {
            return _collection.GetType<IHttpSendFileFeature>();
        }

        [Benchmark]
        public IHttpSendFileFeature GetLastViaGeneric()
        {
            return _collection.Get<IHttpSendFileFeature>();
        }

        private object Get(Type type)
        {
            return _collection[type];
        }

        private object GetFastFeature(Type type)
        {
            return _http1Connection.FastFeatureGet(type);
        }

        public HttpProtocolFeatureCollection()
        {
            var memoryPool = new MemoryPool();
            var pair = PipeFactory.CreateConnectionPair(memoryPool);

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = new MockTrace(),
                HttpParser = new HttpParser<Http1ParsingHandler>()
            };

            var http1Connection = new Http1Connection(new Http1ConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                MemoryPool = memoryPool,
                Application = pair.Application,
                Transport = pair.Transport
            });

            http1Connection.Reset();

            _http1Connection = http1Connection;
        }

        [IterationSetup]
        public void Setup()
        {
            _collection = _http1Connection;
        }

    }
    public static class IFeatureCollectionExtensions
    {
        public static T GetType<T>(this IFeatureCollection collection)
        {
            return (T)collection[typeof(T)];
        }
    }
}
