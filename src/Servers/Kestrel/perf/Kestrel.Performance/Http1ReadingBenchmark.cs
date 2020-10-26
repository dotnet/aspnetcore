// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
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
    public class Http1ReadingBenchmark
    {
        // Standard completed task
        private static readonly Func<object, Task> _syncTaskFunc = (obj) => Task.CompletedTask;
        // Non-standard completed task
        private static readonly Task _pseudoAsyncTask = Task.FromResult(27);
        private static readonly Func<object, Task> _pseudoAsyncTaskFunc = (obj) => _pseudoAsyncTask;

        private TestHttp1Connection _http1Connection;
        private DuplexPipe.DuplexPipePair _pair;
        private MemoryPool<byte> _memoryPool;

        private readonly byte[] _readData = Encoding.ASCII.GetBytes(new string('a', 100));

        [GlobalSetup]
        public void GlobalSetup()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();
            _http1Connection = MakeHttp1Connection();
        }

        [Params(true, false)]
        public bool WithHeaders { get; set; }

        //[Params(true, false)]
        //public bool Chunked { get; set; }

        [Params(Startup.None, Startup.Sync, Startup.Async)]
        public Startup OnStarting { get; set; }

        [IterationSetup]
        public void Setup()
        {
            _http1Connection.Reset();

            _http1Connection.RequestHeaders.ContentLength = _readData.Length;

            if (!WithHeaders)
            {
                _http1Connection.FlushAsync().GetAwaiter().GetResult();
            }

            ResetState();
        }

        private void ResetState()
        {
            if (WithHeaders)
            {
                _http1Connection.ResetState();

                switch (OnStarting)
                {
                    case Startup.Sync:
                        _http1Connection.OnStarting(_syncTaskFunc, null);
                        break;
                    case Startup.Async:
                        _http1Connection.OnStarting(_pseudoAsyncTaskFunc, null);
                        break;
                }
            }
        }

        [Benchmark]
        public Task ReadAsync()
        {
            ResetState();

            return _http1Connection.ResponseBody.ReadAsync(new byte[100], default(CancellationToken)).AsTask();
        }

        private TestHttp1Connection MakeHttp1Connection()
        {
            var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
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
            http1Connection.InitializeBodyControl(new Http1ContentLengthMessageBody(keepAlive: true, 100, http1Connection));
            serviceContext.DateHeaderValueManager.OnHeartbeat(DateTimeOffset.UtcNow);

            return http1Connection;
        }

        [IterationCleanup]
        public void Cleanup()
        {
            var reader = _pair.Application.Input;
            if (reader.TryRead(out var readResult))
            {
                reader.AdvanceTo(readResult.Buffer.End);
            }
        }

        public enum Startup
        {
            None,
            Sync,
            Async
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _memoryPool?.Dispose();
        }
    }
}
