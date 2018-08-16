// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http2TestBase : TestApplicationErrorLoggerLoggedTest, IDisposable, IHttpHeadersHandler
    {
        protected static readonly string _largeHeaderValue = new string('a', HPackDecoder.MaxStringOctets);

        protected static readonly IEnumerable<KeyValuePair<string, string>> _browserRequestHeaders = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
            new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
            new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
            new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
            new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
        };

        private readonly MemoryPool<byte> _memoryPool = KestrelMemoryPool.Create();
        internal readonly DuplexPipe.DuplexPipePair _pair;

        protected readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();
        protected readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        protected readonly HPackDecoder _hpackDecoder;

        protected readonly ConcurrentDictionary<int, TaskCompletionSource<object>> _runningStreams = new ConcurrentDictionary<int, TaskCompletionSource<object>>();
        protected readonly Dictionary<string, string> _receivedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        protected readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        protected readonly HashSet<int> _abortedStreamIds = new HashSet<int>();
        protected readonly object _abortedStreamIdsLock = new object();
        protected readonly TaskCompletionSource<object> _closingStateReached = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        protected readonly TaskCompletionSource<object> _closedStateReached = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        protected readonly RequestDelegate _noopApplication;
        protected readonly RequestDelegate _readHeadersApplication;
        protected readonly RequestDelegate _readTrailersApplication;
        protected readonly RequestDelegate _bufferingApplication;
        protected readonly RequestDelegate _echoApplication;
        protected readonly RequestDelegate _echoWaitForAbortApplication;
        protected readonly RequestDelegate _largeHeadersApplication;
        protected readonly RequestDelegate _waitForAbortApplication;
        protected readonly RequestDelegate _waitForAbortFlushingApplication;
        protected readonly RequestDelegate _waitForAbortWithDataApplication;
        protected readonly RequestDelegate _echoMethod;
        protected readonly RequestDelegate _echoHost;
        protected readonly RequestDelegate _echoPath;

        protected Http2ConnectionContext _connectionContext;
        protected Http2Connection _connection;
        protected Task _connectionTask;

        public Http2TestBase()
        {
            // Always dispatch test code back to the ThreadPool. This prevents deadlocks caused by continuing
            // Http2Connection.ProcessRequestsAsync() loop with writer locks acquired. Run product code inline to make
            // it easier to verify request frames are processed correctly immediately after sending the them.
            var inputPipeOptions = new PipeOptions(
                pool: _memoryPool,
                readerScheduler: PipeScheduler.Inline,
                writerScheduler: PipeScheduler.ThreadPool,
                useSynchronizationContext: false
            );
            var outputPipeOptions = new PipeOptions(
                pool: _memoryPool,
                readerScheduler: PipeScheduler.ThreadPool,
                writerScheduler: PipeScheduler.Inline,
                useSynchronizationContext: false
            );

            _pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);
            _hpackDecoder = new HPackDecoder((int)_clientSettings.HeaderTableSize);

            _noopApplication = context => Task.CompletedTask;

            _readHeadersApplication = context =>
            {
                foreach (var header in context.Request.Headers)
                {
                    _receivedHeaders[header.Key] = header.Value.ToString();
                }

                return Task.CompletedTask;
            };

            _readTrailersApplication = async context =>
            {
                using (var ms = new MemoryStream())
                {
                    // Consuming the entire request body guarantees trailers will be available
                    await context.Request.Body.CopyToAsync(ms);
                }

                foreach (var header in context.Request.Headers)
                {
                    _receivedHeaders[header.Key] = header.Value.ToString();
                }
            };

            _bufferingApplication = async context =>
            {
                var data = new List<byte>();
                var buffer = new byte[1024];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    data.AddRange(new ArraySegment<byte>(buffer, 0, received));
                }

                await context.Response.Body.WriteAsync(data.ToArray(), 0, data.Count);
            };

            _echoApplication = async context =>
            {
                var buffer = new byte[Http2Frame.MinAllowedMaxFrameSize];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, received);
                }
            };

            _echoWaitForAbortApplication = async context =>
            {
                var buffer = new byte[Http2Frame.MinAllowedMaxFrameSize];
                var received = 0;

                while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, received);
                }

                var sem = new SemaphoreSlim(0);

                context.RequestAborted.Register(() =>
                {
                    sem.Release();
                });

                await sem.WaitAsync().DefaultTimeout();
            };

            _largeHeadersApplication = context =>
            {
                foreach (var name in new[] { "a", "b", "c", "d", "e", "f", "g", "h" })
                {
                    context.Response.Headers[name] = _largeHeaderValue;
                }

                return Task.CompletedTask;
            };

            _waitForAbortApplication = async context =>
            {
                var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();
                var sem = new SemaphoreSlim(0);

                context.RequestAborted.Register(() =>
                {
                    lock (_abortedStreamIdsLock)
                    {
                        _abortedStreamIds.Add(streamIdFeature.StreamId);
                    }

                    sem.Release();
                });

                await sem.WaitAsync().DefaultTimeout();

                _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
            };

            _waitForAbortFlushingApplication = async context =>
            {
                var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();
                var sem = new SemaphoreSlim(0);

                context.RequestAborted.Register(() =>
                {
                    lock (_abortedStreamIdsLock)
                    {
                        _abortedStreamIds.Add(streamIdFeature.StreamId);
                    }

                    sem.Release();
                });

                await sem.WaitAsync().DefaultTimeout();

                await context.Response.Body.FlushAsync();

                _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
            };

            _waitForAbortWithDataApplication = async context =>
            {
                var streamIdFeature = context.Features.Get<IHttp2StreamIdFeature>();
                var sem = new SemaphoreSlim(0);

                context.RequestAborted.Register(() =>
                {
                    lock (_abortedStreamIdsLock)
                    {
                        _abortedStreamIds.Add(streamIdFeature.StreamId);
                    }

                    sem.Release();
                });

                await sem.WaitAsync().DefaultTimeout();

                await context.Response.Body.WriteAsync(new byte[10], 0, 10);

                _runningStreams[streamIdFeature.StreamId].TrySetResult(null);
            };

            _echoMethod = context =>
            {
                context.Response.Headers["Method"] = context.Request.Method;

                return Task.CompletedTask;
            };

            _echoHost = context =>
            {
                context.Response.Headers[HeaderNames.Host] = context.Request.Headers[HeaderNames.Host];

                return Task.CompletedTask;
            };

            _echoPath = context =>
            {
                context.Response.Headers["path"] = context.Request.Path.ToString();
                context.Response.Headers["rawtarget"] = context.Features.Get<IHttpRequestFeature>().RawTarget;

                return Task.CompletedTask;
            };
        }

        public override void Initialize(MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(methodInfo, testMethodArguments, testOutputHelper);

            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(m => m.Http2ConnectionClosing(It.IsAny<string>()))
                .Callback(() => _closingStateReached.SetResult(null));
            mockKestrelTrace
                .Setup(m => m.Http2ConnectionClosed(It.IsAny<string>(), It.IsAny<int>()))
                .Callback(() => _closedStateReached.SetResult(null));

            _connectionContext = new Http2ConnectionContext
            {
                ConnectionFeatures = new FeatureCollection(),
                ServiceContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object),
                MemoryPool = _memoryPool,
                Application = _pair.Application,
                Transport = _pair.Transport
            };

            _connection = new Http2Connection(_connectionContext);
        }

        public override void Dispose()
        {
            _pair.Application.Input.Complete();
            _pair.Application.Output.Complete();
            _pair.Transport.Input.Complete();
            _pair.Transport.Output.Complete();
            _memoryPool.Dispose();

            base.Dispose();
        }

        void IHttpHeadersHandler.OnHeader(Span<byte> name, Span<byte> value)
        {
            _decodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiStringNonNullCharacters();
        }

        protected async Task InitializeConnectionAsync(RequestDelegate application)
        {
            _connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(application));

            await SendPreambleAsync().ConfigureAwait(false);
            await SendSettingsAsync();

            await ExpectAsync(Http2FrameType.SETTINGS,
                withLength: 6,
                withFlags: 0,
                withStreamId: 0);

            await ExpectAsync(Http2FrameType.SETTINGS,
                withLength: 0,
                withFlags: (byte)Http2SettingsFrameFlags.ACK,
                withStreamId: 0);
        }

        protected async Task StartStreamAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);
            var done = _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);
            frame.Length = length;

            if (done)
            {
                frame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;
            }

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);

            while (!done)
            {
                frame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);
                done = _hpackEncoder.Encode(frame.HeadersPayload, out length);
                frame.Length = length;

                if (done)
                {
                    frame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
                }

                await SendAsync(frame.Raw);
            }
        }

        protected async Task SendHeadersWithPaddingAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED, streamId);
            frame.HeadersPadLength = padLength;

            _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);

            frame.Length = 1 + length + padLength;
            frame.Payload.Slice(1 + length).Fill(0);

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);
        }

        protected async Task SendHeadersWithPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte priority, int streamDependency, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PRIORITY, streamId);
            frame.HeadersPriority = priority;
            frame.HeadersStreamDependency = streamDependency;

            _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);

            frame.Length = 5 + length;

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);
        }

        protected async Task SendHeadersWithPaddingAndPriorityAsync(int streamId, IEnumerable<KeyValuePair<string, string>> headers, byte padLength, byte priority, int streamDependency, bool endStream)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runningStreams[streamId] = tcs;

            var frame = new Http2Frame();
            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.PADDED | Http2HeadersFrameFlags.PRIORITY, streamId);
            frame.HeadersPadLength = padLength;
            frame.HeadersPriority = priority;
            frame.HeadersStreamDependency = streamDependency;

            _hpackEncoder.BeginEncode(headers, frame.HeadersPayload, out var length);

            frame.Length = 6 + length + padLength;
            frame.Payload.Slice(6 + length).Fill(0);

            if (endStream)
            {
                frame.HeadersFlags |= Http2HeadersFrameFlags.END_STREAM;
            }

            await SendAsync(frame.Raw);
        }

        protected Task WaitForAllStreamsAsync()
        {
            return Task.WhenAll(_runningStreams.Values.Select(tcs => tcs.Task)).DefaultTimeout();
        }

        protected Task SendAsync(ReadOnlySpan<byte> span)
        {
            var writableBuffer = _pair.Application.Output;
            writableBuffer.Write(span);
            return FlushAsync(writableBuffer);
        }

        protected static async Task FlushAsync(PipeWriter writableBuffer)
        {
            await writableBuffer.FlushAsync();
        }

        protected Task SendPreambleAsync() => SendAsync(new ArraySegment<byte>(Http2Connection.ClientPreface));

        protected Task SendSettingsAsync()
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE, _clientSettings.GetNonProtocolDefaults());
            return SendAsync(frame.Raw);
        }

        protected Task SendSettingsAckWithInvalidLengthAsync(int length)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.ACK);
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        protected Task SendSettingsWithInvalidStreamIdAsync(int streamId)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE, _clientSettings.GetNonProtocolDefaults());
            frame.StreamId = streamId;
            return SendAsync(frame.Raw);
        }

        protected Task SendSettingsWithInvalidLengthAsync(int length)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE, _clientSettings.GetNonProtocolDefaults());
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        protected Task SendSettingsWithInvalidParameterValueAsync(Http2SettingsParameter parameter, uint value)
        {
            var frame = new Http2Frame();
            frame.PrepareSettings(Http2SettingsFrameFlags.NONE);
            frame.Length = 6;

            frame.Payload[0] = (byte)((ushort)parameter >> 8);
            frame.Payload[1] = (byte)(ushort)parameter;
            frame.Payload[2] = (byte)(value >> 24);
            frame.Payload[3] = (byte)(value >> 16);
            frame.Payload[4] = (byte)(value >> 8);
            frame.Payload[5] = (byte)value;

            return SendAsync(frame.Raw);
        }

        protected Task SendPushPromiseFrameAsync()
        {
            var frame = new Http2Frame();
            frame.Length = 0;
            frame.Type = Http2FrameType.PUSH_PROMISE;
            frame.StreamId = 1;
            return SendAsync(frame.Raw);
        }

        protected async Task<bool> SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, IEnumerable<KeyValuePair<string, string>> headers)
        {
            var frame = new Http2Frame();

            frame.PrepareHeaders(flags, streamId);
            var done = _hpackEncoder.BeginEncode(headers, frame.Payload, out var length);
            frame.Length = length;

            await SendAsync(frame.Raw);

            return done;
        }

        protected Task SendHeadersAsync(int streamId, Http2HeadersFrameFlags flags, byte[] headerBlock)
        {
            var frame = new Http2Frame();

            frame.PrepareHeaders(flags, streamId);
            frame.Length = headerBlock.Length;
            headerBlock.CopyTo(frame.HeadersPayload);

            return SendAsync(frame.Raw);
        }

        protected Task SendInvalidHeadersFrameAsync(int streamId, int frameLength, byte padLength)
        {
            Assert.True(padLength >= frameLength, $"{nameof(padLength)} must be greater than or equal to {nameof(frameLength)} to create an invalid frame.");

            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.PADDED, streamId);
            frame.Payload[0] = padLength;

            // Set length last so .Payload can be written to
            frame.Length = frameLength;

            return SendAsync(frame.Raw);
        }

        protected Task SendIncompleteHeadersFrameAsync(int streamId)
        {
            var frame = new Http2Frame();

            frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
            frame.Length = 3;

            // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
            // with an incomplete new name
            frame.Payload[0] = 0;
            frame.Payload[1] = 2;
            frame.Payload[2] = (byte)'a';

            return SendAsync(frame.Raw);
        }

        protected async Task<bool> SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            var done = _hpackEncoder.Encode(frame.Payload, out var length);
            frame.Length = length;

            await SendAsync(frame.Raw);

            return done;
        }

        protected async Task SendContinuationAsync(int streamId, Http2ContinuationFrameFlags flags, byte[] payload)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            frame.Length = payload.Length;
            payload.CopyTo(frame.Payload);

            await SendAsync(frame.Raw);
        }

        protected Task SendEmptyContinuationFrameAsync(int streamId, Http2ContinuationFrameFlags flags)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(flags, streamId);
            frame.Length = 0;

            return SendAsync(frame.Raw);
        }

        protected Task SendIncompleteContinuationFrameAsync(int streamId)
        {
            var frame = new Http2Frame();

            frame.PrepareContinuation(Http2ContinuationFrameFlags.END_HEADERS, streamId);
            frame.Length = 3;

            // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
            // with an incomplete new name
            frame.Payload[0] = 0;
            frame.Payload[1] = 2;
            frame.Payload[2] = (byte)'a';

            return SendAsync(frame.Raw);
        }

        protected Task SendDataAsync(int streamId, Span<byte> data, bool endStream)
        {
            var frame = new Http2Frame();

            frame.PrepareData(streamId);
            frame.Length = data.Length;
            frame.DataFlags = endStream ? Http2DataFrameFlags.END_STREAM : Http2DataFrameFlags.NONE;
            data.CopyTo(frame.DataPayload);

            return SendAsync(frame.Raw);
        }

        protected Task SendDataWithPaddingAsync(int streamId, Span<byte> data, byte padLength, bool endStream)
        {
            var frame = new Http2Frame();

            frame.PrepareData(streamId, padLength);
            frame.Length = data.Length + 1 + padLength;
            data.CopyTo(frame.DataPayload);

            if (endStream)
            {
                frame.DataFlags |= Http2DataFrameFlags.END_STREAM;
            }

            return SendAsync(frame.Raw);
        }

        protected Task SendInvalidDataFrameAsync(int streamId, int frameLength, byte padLength)
        {
            Assert.True(padLength >= frameLength, $"{nameof(padLength)} must be greater than or equal to {nameof(frameLength)} to create an invalid frame.");

            var frame = new Http2Frame();

            frame.PrepareData(streamId);
            frame.DataFlags = Http2DataFrameFlags.PADDED;
            frame.Payload[0] = padLength;

            // Set length last so .Payload can be written to
            frame.Length = frameLength;

            return SendAsync(frame.Raw);
        }

        protected Task SendPingAsync(Http2PingFrameFlags flags)
        {
            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(flags);
            return SendAsync(pingFrame.Raw);
        }

        protected Task SendPingWithInvalidLengthAsync(int length)
        {
            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(Http2PingFrameFlags.NONE);
            pingFrame.Length = length;
            return SendAsync(pingFrame.Raw);
        }

        protected Task SendPingWithInvalidStreamIdAsync(int streamId)
        {
            Assert.NotEqual(0, streamId);

            var pingFrame = new Http2Frame();
            pingFrame.PreparePing(Http2PingFrameFlags.NONE);
            pingFrame.StreamId = streamId;
            return SendAsync(pingFrame.Raw);
        }

        protected Task SendPriorityAsync(int streamId, int streamDependency = 0)
        {
            var priorityFrame = new Http2Frame();
            priorityFrame.PreparePriority(streamId, streamDependency: streamDependency, exclusive: false, weight: 0);
            return SendAsync(priorityFrame.Raw);
        }

        protected Task SendInvalidPriorityFrameAsync(int streamId, int length)
        {
            var priorityFrame = new Http2Frame();
            priorityFrame.PreparePriority(streamId, streamDependency: 0, exclusive: false, weight: 0);
            priorityFrame.Length = length;
            return SendAsync(priorityFrame.Raw);
        }

        protected Task SendRstStreamAsync(int streamId)
        {
            var rstStreamFrame = new Http2Frame();
            rstStreamFrame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
            return SendAsync(rstStreamFrame.Raw);
        }

        protected Task SendInvalidRstStreamFrameAsync(int streamId, int length)
        {
            var frame = new Http2Frame();
            frame.PrepareRstStream(streamId, Http2ErrorCode.CANCEL);
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        protected Task SendGoAwayAsync()
        {
            var frame = new Http2Frame();
            frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
            return SendAsync(frame.Raw);
        }

        protected Task SendInvalidGoAwayFrameAsync()
        {
            var frame = new Http2Frame();
            frame.PrepareGoAway(0, Http2ErrorCode.NO_ERROR);
            frame.StreamId = 1;
            return SendAsync(frame.Raw);
        }

        protected Task SendWindowUpdateAsync(int streamId, int sizeIncrement)
        {
            var frame = new Http2Frame();
            frame.PrepareWindowUpdate(streamId, sizeIncrement);
            return SendAsync(frame.Raw);
        }

        protected Task SendInvalidWindowUpdateAsync(int streamId, int sizeIncrement, int length)
        {
            var frame = new Http2Frame();
            frame.PrepareWindowUpdate(streamId, sizeIncrement);
            frame.Length = length;
            return SendAsync(frame.Raw);
        }

        protected Task SendUnknownFrameTypeAsync(int streamId, int frameType)
        {
            var frame = new Http2Frame();
            frame.StreamId = streamId;
            frame.Type = (Http2FrameType)frameType;
            frame.Length = 0;
            return SendAsync(frame.Raw);
        }

        protected async Task<Http2Frame> ReceiveFrameAsync()
        {
            var frame = new Http2Frame();

            while (true)
            {
                var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End;

                try
                {
                    Assert.True(buffer.Length > 0);

                    if (Http2FrameReader.ReadFrame(buffer, frame, 16_384, out consumed, out examined))
                    {
                        return frame;
                    }

                    if (result.IsCompleted)
                    {
                        throw new IOException("The reader completed without returning a frame.");
                    }
                }
                finally
                {
                    _pair.Application.Input.AdvanceTo(consumed, examined);
                }
            }
        }

        protected async Task<Http2Frame> ExpectAsync(Http2FrameType type, int withLength, byte withFlags, int withStreamId)
        {
            var frame = await ReceiveFrameAsync();

            Assert.Equal(type, frame.Type);
            Assert.Equal(withLength, frame.Length);
            Assert.Equal(withFlags, frame.Flags);
            Assert.Equal(withStreamId, frame.StreamId);

            return frame;
        }

        protected Task StopConnectionAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
        {
            _pair.Application.Output.Complete();

            return WaitForConnectionStopAsync(expectedLastStreamId, ignoreNonGoAwayFrames);
        }

        protected Task WaitForConnectionStopAsync(int expectedLastStreamId, bool ignoreNonGoAwayFrames)
        {
            return WaitForConnectionErrorAsync<Exception>(ignoreNonGoAwayFrames, expectedLastStreamId, Http2ErrorCode.NO_ERROR, expectedErrorMessage: null);
        }

        protected void VerifyGoAway(Http2Frame frame, int expectedLastStreamId, Http2ErrorCode expectedErrorCode)
        {
            Assert.Equal(Http2FrameType.GOAWAY, frame.Type);
            Assert.Equal(8, frame.Length);
            Assert.Equal(0, frame.Flags);
            Assert.Equal(0, frame.StreamId);
            Assert.Equal(expectedLastStreamId, frame.GoAwayLastStreamId);
            Assert.Equal(expectedErrorCode, frame.GoAwayErrorCode);
        }

        protected async Task WaitForConnectionErrorAsync<TException>(bool ignoreNonGoAwayFrames, int expectedLastStreamId, Http2ErrorCode expectedErrorCode, string expectedErrorMessage)
            where TException : Exception
        {
            var frame = await ReceiveFrameAsync();

            if (ignoreNonGoAwayFrames)
            {
                while (frame.Type != Http2FrameType.GOAWAY)
                {
                    frame = await ReceiveFrameAsync();
                }
            }

            VerifyGoAway(frame, expectedLastStreamId, expectedErrorCode);

            if (expectedErrorMessage != null)
            {
                var message = Assert.Single(TestApplicationErrorLogger.Messages, m => m.Exception is TException);
                Assert.Contains(expectedErrorMessage, message.Exception.Message);
            }

            await _connectionTask;
            _pair.Application.Output.Complete();
        }

        protected async Task WaitForStreamErrorAsync(int expectedStreamId, Http2ErrorCode expectedErrorCode, string expectedErrorMessage)
        {
            var frame = await ReceiveFrameAsync();

            Assert.Equal(Http2FrameType.RST_STREAM, frame.Type);
            Assert.Equal(4, frame.Length);
            Assert.Equal(0, frame.Flags);
            Assert.Equal(expectedStreamId, frame.StreamId);
            Assert.Equal(expectedErrorCode, frame.RstStreamErrorCode);

            if (expectedErrorMessage != null)
            {
                Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
            }
        }

        protected void VerifyDecodedRequestHeaders(IEnumerable<KeyValuePair<string, string>> expectedHeaders)
        {
            foreach (var header in expectedHeaders)
            {
                Assert.True(_receivedHeaders.TryGetValue(header.Key, out var value), header.Key);
                Assert.Equal(header.Value, value, ignoreCase: true);
            }
        }
    }
}
