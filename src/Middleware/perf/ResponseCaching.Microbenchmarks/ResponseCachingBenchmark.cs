// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets.Microbenchmarks
{
    public class ResponseCachingBenchmark
    {
        private static readonly string _cacheControl = $"{CacheControlHeaderValue.PublicString}, {CacheControlHeaderValue.MaxAgeString}={int.MaxValue}";

        private ResponseCachingMiddleware _middleware;
        private readonly byte[] _data = new byte[1 * 1024 * 1024];

        [Params(
            100,
            64 * 1024,
            1 * 1024 * 1024
        )]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _middleware = new ResponseCachingMiddleware(
                    async context => {
                        context.Response.Headers[HeaderNames.CacheControl] = _cacheControl;
                        await context.Response.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(_data, 0, Size));
                    },
                    Options.Create(new ResponseCachingOptions
                    {
                        SizeLimit = int.MaxValue, // ~2GB
                        MaximumBodySize = 1 * 1024 * 1024,
                    }),
                    NullLoggerFactory.Instance,
                    new DefaultObjectPoolProvider()
                );

            // no need to actually cache as there is a warm-up fase
        }

        [Benchmark]
        public async Task Cache()
        {
            var pipe = new Pipe();
            var consumer = ConsumeAsync(pipe.Reader, CancellationToken.None);
            DefaultHttpContext context = CreateHttpContext(pipe);
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/a";

            // don't serve from cache but store result
            context.Request.Headers[HeaderNames.CacheControl] = CacheControlHeaderValue.NoCacheString;

            await _middleware.Invoke(context);

            await pipe.Writer.CompleteAsync();
            await consumer;
        }

        [Benchmark]
        public async Task ServeFromCache()
        {
            var pipe = new Pipe();
            var consumer = ConsumeAsync(pipe.Reader, CancellationToken.None);
            DefaultHttpContext context = CreateHttpContext(pipe);
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/b";

            await _middleware.Invoke(context);

            await pipe.Writer.CompleteAsync();
            await consumer;
        }

        private static DefaultHttpContext CreateHttpContext(Pipe pipe)
        {
            var features = new FeatureCollection();
            features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            features.Set<IHttpResponseFeature>(new HttpResponseFeature());
            features.Set<IHttpResponseBodyFeature>(new PipeResponseBodyFeature(pipe.Writer));
            var context = new DefaultHttpContext(features);
            return context;
        }

        private async ValueTask ConsumeAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                reader.AdvanceTo(buffer.End, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await reader.CompleteAsync();
        }

        private class PipeResponseBodyFeature : IHttpResponseBodyFeature
        {
            public PipeResponseBodyFeature(PipeWriter pipeWriter)
            {
                Writer = pipeWriter;
            }

            public Stream Stream => Writer.AsStream();

            public PipeWriter Writer { get; }

            public Task CompleteAsync() => Writer.CompleteAsync().AsTask();

            public void DisableBuffering()
            {
                throw new NotImplementedException();
            }

            public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task StartAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
