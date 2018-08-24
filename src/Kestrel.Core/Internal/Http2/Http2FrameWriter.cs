// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2FrameWriter
    {
        // Literal Header Field without Indexing - Indexed Name (Index 8 - :status)
        private static readonly byte[] _continueBytes = new byte[] { 0x08, 0x03, (byte)'1', (byte)'0', (byte)'0' };

        private uint _maxFrameSize = Http2Limits.MinAllowedMaxFrameSize;
        private Http2Frame _outgoingFrame;
        private readonly object _writeLock = new object();
        private readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        private readonly PipeWriter _outputWriter;
        private bool _aborted;
        private readonly ConnectionContext _connectionContext;
        private readonly OutputFlowControl _connectionOutputFlowControl;
        private readonly string _connectionId;
        private readonly IKestrelTrace _log;
        private readonly StreamSafePipeFlusher _flusher;

        private bool _completed;

        public Http2FrameWriter(
            PipeWriter outputPipeWriter,
            ConnectionContext connectionContext,
            OutputFlowControl connectionOutputFlowControl,
            ITimeoutControl timeoutControl,
            string connectionId,
            IKestrelTrace log)
        {
            _outputWriter = outputPipeWriter;
            _connectionContext = connectionContext;
            _connectionOutputFlowControl = connectionOutputFlowControl;
            _connectionId = connectionId;
            _log = log;
            _flusher = new StreamSafePipeFlusher(_outputWriter, timeoutControl);
            _outgoingFrame = new Http2Frame(_maxFrameSize);
        }

        public void UpdateMaxFrameSize(uint maxFrameSize)
        {
            lock (_writeLock)
            {
                _maxFrameSize = maxFrameSize;
                _outgoingFrame = new Http2Frame(maxFrameSize);
            }
        }

        public void Complete()
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                _connectionOutputFlowControl.Abort();
                _outputWriter.Complete();
            }
        }

        public void Abort(ConnectionAbortedException error)
        {
            lock (_writeLock)
            {
                if (_aborted)
                {
                    return;
                }

                _aborted = true;
                _connectionContext.Abort(error);

                Complete();
            }
        }

        public Task FlushAsync(IHttpOutputProducer outputProducer, CancellationToken cancellationToken)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                return _flusher.FlushAsync(0, outputProducer, cancellationToken);
            }
        }

        public Task Write100ContinueAsync(int streamId)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
                _outgoingFrame.PayloadLength = _continueBytes.Length;
                _continueBytes.CopyTo(_outgoingFrame.HeadersPayload);

                return WriteFrameUnsynchronizedAsync();
            }
        }

        public void WriteResponseHeaders(int streamId, int statusCode, IHeaderDictionary headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);

                var done = _hpackEncoder.BeginEncode(statusCode, EnumerateHeaders(headers), _outgoingFrame.Payload, out var payloadLength);
                _outgoingFrame.PayloadLength = payloadLength;

                if (done)
                {
                    _outgoingFrame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;
                }

                _log.Http2FrameSending(_connectionId, _outgoingFrame);
                _outputWriter.Write(_outgoingFrame.Raw);

                while (!done)
                {
                    _outgoingFrame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);

                    done = _hpackEncoder.Encode(_outgoingFrame.Payload, out var length);
                    _outgoingFrame.PayloadLength = length;

                    if (done)
                    {
                        _outgoingFrame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
                    }

                    _log.Http2FrameSending(_connectionId, _outgoingFrame);
                    _outputWriter.Write(_outgoingFrame.Raw);
                }
            }
        }

        public Task WriteDataAsync(int streamId, StreamOutputFlowControl flowControl, ReadOnlySequence<byte> data, bool endStream)
        {
            // The Length property of a ReadOnlySequence can be expensive, so we cache the value.
            var dataLength = data.Length;

            lock (_writeLock)
            {
                if (_completed || flowControl.IsAborted)
                {
                    return Task.CompletedTask;
                }

                // Zero-length data frames are allowed to be sent immediately even if there is no space available in the flow control window.
                // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.1
                if (dataLength != 0 && dataLength > flowControl.Available)
                {
                    return WriteDataAsyncAwaited(streamId, flowControl, data, dataLength, endStream);
                }

                // This cast is safe since if dataLength would overflow an int, it's guaranteed to be greater than the available flow control window.
                flowControl.Advance((int)dataLength);
                return WriteDataUnsynchronizedAsync(streamId, data, endStream);
            }
        }

        private Task WriteDataUnsynchronizedAsync(int streamId, ReadOnlySequence<byte> data, bool endStream)
        {
            _outgoingFrame.PrepareData(streamId);

            var payload = _outgoingFrame.Payload;
            var unwrittenPayloadLength = 0;

            foreach (var buffer in data)
            {
                var current = buffer;

                while (current.Length > payload.Length)
                {
                    current.Span.Slice(0, payload.Length).CopyTo(payload);
                    current = current.Slice(payload.Length);

                    _log.Http2FrameSending(_connectionId, _outgoingFrame);
                    _outputWriter.Write(_outgoingFrame.Raw);
                    payload = _outgoingFrame.Payload;
                    unwrittenPayloadLength = 0;
                }

                if (current.Length > 0)
                {
                    current.Span.CopyTo(payload);
                    payload = payload.Slice(current.Length);
                    unwrittenPayloadLength += current.Length;
                }
            }

            if (endStream)
            {
                _outgoingFrame.DataFlags = Http2DataFrameFlags.END_STREAM;
            }

            _outgoingFrame.PayloadLength = unwrittenPayloadLength;

            _log.Http2FrameSending(_connectionId, _outgoingFrame);
            _outputWriter.Write(_outgoingFrame.Raw);

            return _flusher.FlushAsync();
        }

        private async Task WriteDataAsyncAwaited(int streamId, StreamOutputFlowControl flowControl, ReadOnlySequence<byte> data, long dataLength, bool endStream)
        {
            while (dataLength > 0)
            {
                OutputFlowControlAwaitable availabilityAwaitable;
                var writeTask = Task.CompletedTask;

                lock (_writeLock)
                {
                    if (_completed || flowControl.IsAborted)
                    {
                        break;
                    }

                    var actual = flowControl.AdvanceUpToAndWait(dataLength, out availabilityAwaitable);

                    if (actual > 0)
                    {
                        if (actual < dataLength)
                        {
                            writeTask = WriteDataUnsynchronizedAsync(streamId, data.Slice(0, actual), endStream: false);
                            data = data.Slice(actual);
                            dataLength -= actual;
                        }
                        else
                        {
                            writeTask = WriteDataUnsynchronizedAsync(streamId, data, endStream);
                            dataLength = 0;
                        }
                    }
                }

                // This awaitable releases continuations in FIFO order when the window updates.
                // It should be very rare for a continuation to run without any availability.
                if (availabilityAwaitable != null)
                {
                    await availabilityAwaitable;
                }

                await writeTask;
            }

            // Ensure that the application continuation isn't executed inline by ProcessWindowUpdateFrameAsync.
            await ThreadPoolAwaitable.Instance;
        }

        public Task WriteWindowUpdateAsync(int streamId, int sizeIncrement)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareWindowUpdate(streamId, sizeIncrement);
                return WriteFrameUnsynchronizedAsync();
            }
        }

        public Task WriteRstStreamAsync(int streamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareRstStream(streamId, errorCode);
                return WriteFrameUnsynchronizedAsync();
            }
        }

        public Task WriteSettingsAsync(IList<Http2PeerSetting> settings)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.NONE, settings);
                return WriteFrameUnsynchronizedAsync();
            }
        }

        public Task WriteSettingsAckAsync()
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.ACK);
                return WriteFrameUnsynchronizedAsync();
            }
        }

        public Task WritePingAsync(Http2PingFrameFlags flags, ReadOnlySpan<byte> payload)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PreparePing(Http2PingFrameFlags.ACK);
                payload.CopyTo(_outgoingFrame.Payload);
                return WriteFrameUnsynchronizedAsync();
            }
        }

        public Task WriteGoAwayAsync(int lastStreamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareGoAway(lastStreamId, errorCode);
                return WriteFrameUnsynchronizedAsync();
            }
        }

        private Task WriteFrameUnsynchronizedAsync()
        {
            if (_completed)
            {
                return Task.CompletedTask;
            }

            _log.Http2FrameSending(_connectionId, _outgoingFrame);
            _outputWriter.Write(_outgoingFrame.Raw);
            return _flusher.FlushAsync();
        }

        public bool TryUpdateConnectionWindow(int bytes)
        {
            lock (_writeLock)
            {
                return _connectionOutputFlowControl.TryUpdateWindow(bytes);
            }
        }

        public bool TryUpdateStreamWindow(StreamOutputFlowControl flowControl, int bytes)
        {
            lock (_writeLock)
            {
                return flowControl.TryUpdateWindow(bytes);
            }
        }

        public void AbortPendingStreamDataWrites(StreamOutputFlowControl flowControl)
        {
            lock (_writeLock)
            {
                flowControl.Abort();
            }
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
