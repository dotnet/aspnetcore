// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
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
        private Http2Frame _httpFrame;
        private string _responseData;
        private int _dataWritten;

        [Params(0, 10, 1024 * 1024)]
        public int ResponseDataLength { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();
            _httpFrame = new Http2Frame();
            _responseData = new string('!', ResponseDataLength);

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

            _ = _connection.ProcessRequestsAsync(new DummyApplication(c => ResponseDataLength == 0 ? Task.CompletedTask : c.Response.WriteAsync(_responseData), new MockHttpContextFactory()));

            _connectionPair.Application.Output.Write(Http2Connection.ClientPreface);
            _connectionPair.Application.Output.WriteSettings(new Http2PeerSettings
            {
                InitialWindowSize = 2147483647
            });
            _connectionPair.Application.Output.FlushAsync().GetAwaiter().GetResult();

            // Read past connection setup frames
            ReceiveFrameAsync(_connectionPair.Application.Input, _httpFrame).GetAwaiter().GetResult();
            Debug.Assert(_httpFrame.Type == Http2FrameType.SETTINGS);
            ReceiveFrameAsync(_connectionPair.Application.Input, _httpFrame).GetAwaiter().GetResult();
            Debug.Assert(_httpFrame.Type == Http2FrameType.WINDOW_UPDATE);
            ReceiveFrameAsync(_connectionPair.Application.Input, _httpFrame).GetAwaiter().GetResult();
            Debug.Assert(_httpFrame.Type == Http2FrameType.SETTINGS);
        }

        [Benchmark]
        public async Task EmptyRequest()
        {
            _requestHeadersEnumerator.Initialize(_httpRequestHeaders);
            _requestHeadersEnumerator.MoveNext();
            _connectionPair.Application.Output.WriteStartStream(streamId: _currentStreamId, _requestHeadersEnumerator, _headersBuffer, endStream: true, frame: _httpFrame);
            await _connectionPair.Application.Output.FlushAsync();

            while (true)
            {
                await ReceiveFrameAsync(_connectionPair.Application.Input, _httpFrame);

                if (_httpFrame.StreamId != _currentStreamId && _httpFrame.StreamId != 0)
                {
                    throw new Exception($"Unexpected stream ID: {_httpFrame.StreamId}");
                }

                if (_httpFrame.Type == Http2FrameType.DATA)
                {
                    _dataWritten += _httpFrame.DataPayloadLength;
                }

                if (_dataWritten > 1024 * 32)
                {
                    _connectionPair.Application.Output.WriteWindowUpdateAsync(streamId: 0, _dataWritten, _httpFrame);
                    await _connectionPair.Application.Output.FlushAsync();

                    _dataWritten = 0;
                }

                if ((_httpFrame.HeadersFlags & Http2HeadersFrameFlags.END_STREAM) == Http2HeadersFrameFlags.END_STREAM)
                {
                    break;
                }
            }

            _currentStreamId += 2;
        }

        internal async ValueTask ReceiveFrameAsync(PipeReader pipeReader, Http2Frame frame, uint maxFrameSize = Http2PeerSettings.DefaultMaxFrameSize)
        {
            while (true)
            {
                var result = await pipeReader.ReadAsync();
                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.Start;

                try
                {
                    if (Http2FrameReader.TryReadFrame(ref buffer, frame, maxFrameSize, out var framePayload))
                    {
                        consumed = examined = framePayload.End;
                        return;
                    }
                    else
                    {
                        examined = buffer.End;
                    }

                    if (result.IsCompleted)
                    {
                        throw new IOException("The reader completed without returning a frame.");
                    }
                }
                finally
                {
                    pipeReader.AdvanceTo(consumed, examined);
                }
            }
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _connectionPair.Application.Output.Complete();
            _memoryPool?.Dispose();
        }
    }
}
