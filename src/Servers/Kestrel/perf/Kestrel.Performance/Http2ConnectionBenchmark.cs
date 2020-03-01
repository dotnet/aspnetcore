// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http.HPack;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http2ConnectionBenchmark
    {
        private MemoryPool<byte> _memoryPool;
        private Pipe _pipe;
        private HttpRequestHeaders _httpRequestHeaders;
        private Http2Connection _connection;
        private int _currentStreamId;
        private HPackEncoder _hpackEncoder;
        private byte[] _headersBuffer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();

            var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            _pipe = new Pipe(options);

            _httpRequestHeaders = new HttpRequestHeaders();
            _httpRequestHeaders.Append(HeaderNames.Method, new StringValues("GET"));
            _httpRequestHeaders.Append(HeaderNames.Path, new StringValues("/"));
            _httpRequestHeaders.Append(HeaderNames.Scheme, new StringValues("http"));
            _httpRequestHeaders.Append(HeaderNames.Authority, new StringValues("localhost:80"));

            _hpackEncoder = new HPackEncoder();
            _headersBuffer = new byte[1024 * 16];

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = new KestrelTrace(NullLogger.Instance),
                SystemClock = new MockSystemClock()
            };
            serviceContext.ServerOptions.Limits.Http2.MaxStreamsPerConnection = int.MaxValue;
            serviceContext.DateHeaderValueManager.OnHeartbeat(default);

            _connection = new Http2Connection(new HttpConnectionContext
            {
                MemoryPool = _memoryPool,
                ConnectionId = "TestConnectionId",
                Protocols = Core.HttpProtocols.Http2,
                Transport = new MockDuplexPipe(_pipe.Reader, new NullPipeWriter()),
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                TimeoutControl = new MockTimeoutControl(),
            });

            _currentStreamId = 1;

            _ = _connection.ProcessRequestsAsync(new DummyApplication());

            _pipe.Writer.Write(Http2Connection.ClientPreface);
            PipeWriterHttp2FrameExtensions.WriteSettings(_pipe.Writer, new Http2PeerSettings());
            _pipe.Writer.FlushAsync().GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task EmptyRequest()
        {
            PipeWriterHttp2FrameExtensions.WriteStartStream(_pipe.Writer, streamId: _currentStreamId, EnumerateHeaders(_httpRequestHeaders), _hpackEncoder, _headersBuffer, endStream: true);
            _currentStreamId += 2;
            await _pipe.Writer.FlushAsync();
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _pipe.Writer.Complete();
            _memoryPool?.Dispose();
        }

        private static IEnumerable<KeyValuePair<string, string>> EnumerateHeaders(IHeaderDictionary headers)
        {
            foreach (var header in headers)
            {
                foreach (var value in header.Value)
                {
                    yield return new KeyValuePair<string, string>(header.Key, value);
                }
            }
        }
    }
}
