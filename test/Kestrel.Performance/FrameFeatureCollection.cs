// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]
    public class FrameFeatureCollection
    {
        private readonly Frame<object> _frame;
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
            return _collection.Get<IHttpSendFileFeature> ();
        }

        private object Get(Type type)
        {
            return _collection[type];
        }

        private object GetFastFeature(Type type)
        {
            return _frame.FastFeatureGet(type);
        }

        public FrameFeatureCollection()
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
                }
            };

            _frame = new Frame<object>(application: null, frameContext: frameContext);
        }

        [Setup]
        public void Setup()
        {
            _collection = _frame;
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
