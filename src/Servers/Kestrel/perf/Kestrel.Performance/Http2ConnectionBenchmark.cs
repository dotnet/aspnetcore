// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
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
        private HttpRequestHeaders _httpRequestHeaders;
        private Http2Connection _connection;
        private Http2HeadersEnumerator _requestHeadersEnumerator;
        private int _currentStreamId;
        private byte[] _headersBuffer;
        private DuplexPipe.DuplexPipePair _connectionPair;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();

            var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

            _connectionPair = DuplexPipe.CreateConnectionPair(options, options);

            _httpRequestHeaders = new HttpRequestHeaders();
            _httpRequestHeaders.Append(HeaderNames.Method, new StringValues("GET"));
            _httpRequestHeaders.Append(HeaderNames.Path, new StringValues("/"));
            _httpRequestHeaders.Append(HeaderNames.Scheme, new StringValues("http"));
            _httpRequestHeaders.Append(HeaderNames.Authority, new StringValues("localhost:80"));

            _headersBuffer = new byte[1024 * 16];

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = new KestrelTrace(NullLogger.Instance),
                SystemClock = new MockSystemClock()
            };
            serviceContext.DateHeaderValueManager.OnHeartbeat(default);

            _connection = new Http2Connection(new HttpConnectionContext
            {
                MemoryPool = _memoryPool,
                ConnectionId = "TestConnectionId",
                Protocols = HttpProtocols.Http2,
                Transport = _connectionPair.Transport,
                ServiceContext = serviceContext,
                ConnectionFeatures = new FeatureCollection(),
                TimeoutControl = new MockTimeoutControl(),
            });

            _requestHeadersEnumerator = new Http2HeadersEnumerator();

            _currentStreamId = 1;

            _ = _connection.ProcessRequestsAsync(new DummyApplication(_ => Task.CompletedTask, new MockHttpContextFactory()));

            _connectionPair.Application.Output.Write(Http2Connection.ClientPreface);
            _connectionPair.Application.Output.WriteSettings(new Http2PeerSettings());
            _connectionPair.Application.Output.FlushAsync().GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task EmptyRequest()
        {
            _requestHeadersEnumerator.Initialize(_httpRequestHeaders);
            _requestHeadersEnumerator.MoveNext();
            _connectionPair.Application.Output.WriteStartStream(streamId: _currentStreamId, _requestHeadersEnumerator, _headersBuffer, endStream: true);
            _currentStreamId += 2;
            await _connectionPair.Application.Output.FlushAsync();

            var readResult = await _connectionPair.Application.Input.ReadAsync();
            _connectionPair.Application.Input.AdvanceTo(readResult.Buffer.End);
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _connectionPair.Application.Output.Complete();
            _memoryPool?.Dispose();
        }
    }
}
