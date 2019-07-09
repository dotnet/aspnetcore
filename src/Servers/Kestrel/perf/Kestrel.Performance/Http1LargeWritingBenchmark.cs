// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http1LargeWritingBenchmark
    {
        private TestHttp1Connection _http1Connection;
        private DuplexPipe.DuplexPipePair _pair;
        private MemoryPool<byte> _memoryPool;
        private Task _consumeResponseBodyTask;

        // Keep this divisable by 10 so it can be evenly segmented.
        private readonly byte[] _writeData = new byte[10 * 1024 * 1024];

        [GlobalSetup]
        public void GlobalSetup()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();
            _http1Connection = MakeHttp1Connection();
            _consumeResponseBodyTask = ConsumeResponseBody();
        }

        [IterationSetup]
        public void Setup()
        {
            _http1Connection.Reset();
            _http1Connection.RequestHeaders.ContentLength = _writeData.Length;
            _http1Connection.FlushAsync().GetAwaiter().GetResult();
        }

        [Benchmark]
        public Task WriteAsync()
        {
            return _http1Connection.ResponseBody.WriteAsync(_writeData, 0, _writeData.Length, default);
        }

        [Benchmark]
        public Task WriteSegmentsUnawaitedAsync()
        {
            // Write a 10th the of the data at a time
            var segmentSize = _writeData.Length / 10;

            for (int i = 0; i < 9; i++)
            {
                // Ignore the first nine tasks.
                _ = _http1Connection.ResponseBody.WriteAsync(_writeData, i * segmentSize, segmentSize, default);
            }

            return _http1Connection.ResponseBody.WriteAsync(_writeData, 9 * segmentSize, segmentSize, default);
        }

        private TestHttp1Connection MakeHttp1Connection()
        {
            var options = new PipeOptions(_memoryPool, useSynchronizationContext: false);
            var pair = DuplexPipe.CreateConnectionPair(options, options);
            _pair = pair;

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = new MockTrace(),
                HttpParser = new HttpParser<Http1ParsingHandler>()
            };

            var http1Connection = new TestHttp1Connection(new HttpConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                MemoryPool = _memoryPool,
                TimeoutControl = new TimeoutControl(timeoutHandler: null),
                Transport = pair.Transport
            });

            http1Connection.Reset();
            http1Connection.InitializeBodyControl(MessageBody.ZeroContentLengthKeepAlive);
            serviceContext.DateHeaderValueManager.OnHeartbeat(DateTimeOffset.UtcNow);

            return http1Connection;
        }

        private async Task ConsumeResponseBody()
        {
            var reader = _pair.Application.Input;
            var readResult = await reader.ReadAsync();

            while (!readResult.IsCompleted)
            {
                reader.AdvanceTo(readResult.Buffer.End);
                readResult = await reader.ReadAsync();
            }

            reader.Complete();
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _pair.Transport.Output.Complete();
            _consumeResponseBodyTask.GetAwaiter().GetResult();
            _memoryPool?.Dispose();
        }
    }
}
