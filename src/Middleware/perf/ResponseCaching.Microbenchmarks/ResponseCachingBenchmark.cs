// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
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

        private readonly ResponseCachingMiddleware _middleware;
        private readonly MemoryStream _memory;

        private long _counter = 0;

        public ResponseCachingBenchmark()
        {
            _middleware = new ResponseCachingMiddleware(
                    async context => {
                        context.Response.Headers[HeaderNames.CacheControl] = _cacheControl;
                        await context.Response.WriteAsync("Hello World!");
                    },
                    Options.Create(new ResponseCachingOptions {
                        SizeLimit = int.MaxValue // 2GB
                    }),
                    NullLoggerFactory.Instance,
                    new DefaultObjectPoolProvider()
                );
            _memory = new MemoryStream();
        }

        [GlobalSetup]
        public void Setup()
        {
            // call once to actually cache
            // not async as the version of BenchmarkDotNet used here doesn't have support for async GlobalSetup
            ServeFromCache().GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task Cache()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = $"/{++_counter}";

            await _middleware.Invoke(context);
        }

        [Benchmark]
        public async Task ServeFromCache()
        {
            _memory.Seek(0, SeekOrigin.Begin);
            var context = new DefaultHttpContext();
            context.Response.Body = _memory;
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/cached";

            await _middleware.Invoke(context);

            context.Response.BodyWriter.Complete();
        }
    }
}
