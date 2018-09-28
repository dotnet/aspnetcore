// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
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

        private uint _maxFrameSize = Http2PeerSettings.MinAllowedMaxFrameSize;
        private byte[] _headerEncodingBuffer;
        private Http2Frame _outgoingFrame;
        private readonly object _writeLock = new object();
        private readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        private readonly PipeWriter _outputWriter;
        private bool _aborted;
        private readonly ConnectionContext _connectionContext;
        private readonly OutputFlowControl _connectionOutputFlowControl;
        private readonly string _connectionId;
        private readonly IKestrelTrace _log;
        private readonly TimingPipeFlusher _flusher;

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
            _flusher = new TimingPipeFlusher(_outputWriter, timeoutControl);
            _outgoingFrame = new Http2Frame();
            _headerEncodingBuffer = new byte[_maxFrameSize];
        }

        public void UpdateMaxFrameSize(uint maxFrameSize)
        {
            lock (_writeLock)
            {
                if (_maxFrameSize != maxFrameSize)
                {
                    _maxFrameSize = maxFrameSize;
                    _headerEncodingBuffer = new byte[_maxFrameSize];
                }
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

        public Task FlushAsync(IHttpOutputAborter outputAborter, CancellationToken cancellationToken)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                return _flusher.FlushAsync(outputAborter, cancellationToken);
            }
        }

        public Task Write100ContinueAsync(int streamId)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
                _outgoingFrame.PayloadLength = _continueBytes.Length;
                WriteHeaderUnsynchronized();
                _outputWriter.Write(_continueBytes);
                return _flusher.FlushAsync();
            }
        }

        // Optional header fields for padding and priority are not implemented.
        /* https://tools.ietf.org/html/rfc7540#section-6.2
            +---------------+
            |Pad Length? (8)|
            +-+-------------+-----------------------------------------------+
            |E|                 Stream Dependency? (31)                     |
            +-+-------------+-----------------------------------------------+
            |  Weight? (8)  |
            +-+-------------+-----------------------------------------------+
            |                   Header Block Fragment (*)                 ...
            +---------------------------------------------------------------+
            |                           Padding (*)                       ...
            +---------------------------------------------------------------+
        */
        public void WriteResponseHeaders(int streamId, int statusCode, IHeaderDictionary headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                try
                {
                    _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.NONE, streamId);
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = _hpackEncoder.BeginEncode(statusCode, EnumerateHeaders(headers), buffer, out var payloadLength);

                    _outgoingFrame.PayloadLength = payloadLength;

                    if (done)
                    {
                        _outgoingFrame.HeadersFlags = Http2HeadersFrameFlags.END_HEADERS;
                    }

                    WriteHeaderUnsynchronized();
                    _outputWriter.Write(buffer.Slice(0, payloadLength));

                    while (!done)
                    {
                        _outgoingFrame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);

                        done = _hpackEncoder.Encode(buffer, out payloadLength);
                        _outgoingFrame.PayloadLength = payloadLength;

                        if (done)
                        {
                            _outgoingFrame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
                        }

                        WriteHeaderUnsynchronized();
                        _outputWriter.Write(buffer.Slice(0, payloadLength));
                    }
                }
                catch (HPackEncodingException hex)
                {
                    // Header errors are fatal to the connection. We don't have a direct way to signal this to the Http2Connection.
                    _connectionContext.Abort(new ConnectionAbortedException("", hex));
                    throw new InvalidOperationException("", hex); // Report the error to the user if this was the first write.
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

        /*  Padding is not implemented
            +---------------+
            |Pad Length? (8)|
            +---------------+-----------------------------------------------+
            |                            Data (*)                         ...
            +---------------------------------------------------------------+
            |                           Padding (*)                       ...
            +---------------------------------------------------------------+
        */
        private Task WriteDataUnsynchronizedAsync(int streamId, ReadOnlySequence<byte> data, bool endStream)
        {
            // Note padding is not implemented
            _outgoingFrame.PrepareData(streamId);

            var dataPayloadLength = (int)_maxFrameSize; // Minus padding

            while (data.Length > dataPayloadLength)
            {
                var currentData = data.Slice(0, dataPayloadLength);
                _outgoingFrame.PayloadLength = dataPayloadLength; // Plus padding

                WriteHeaderUnsynchronized();

                foreach (var buffer in currentData)
                {
                    _outputWriter.Write(buffer.Span);
                }

                // Plus padding

                data = data.Slice(dataPayloadLength);
            }

            if (endStream)
            {
                _outgoingFrame.DataFlags |= Http2DataFrameFlags.END_STREAM;
            }

            _outgoingFrame.PayloadLength = (int)data.Length; // Plus padding

            WriteHeaderUnsynchronized();

            foreach (var buffer in data)
            {
                _outputWriter.Write(buffer.Span);
            }

            // Plus padding

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

        /* https://tools.ietf.org/html/rfc7540#section-6.9
            +-+-------------------------------------------------------------+
            |R|              Window Size Increment (31)                     |
            +-+-------------------------------------------------------------+
        */
        public Task WriteWindowUpdateAsync(int streamId, int sizeIncrement)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _outgoingFrame.PrepareWindowUpdate(streamId, sizeIncrement);
                WriteHeaderUnsynchronized();
                var buffer = _outputWriter.GetSpan(4);
                Bitshifter.WriteUInt31BigEndian(buffer, (uint)sizeIncrement);
                _outputWriter.Advance(4);
                return _flusher.FlushAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.4
            +---------------------------------------------------------------+
            |                        Error Code (32)                        |
            +---------------------------------------------------------------+
        */
        public Task WriteRstStreamAsync(int streamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _outgoingFrame.PrepareRstStream(streamId, errorCode);
                WriteHeaderUnsynchronized();
                var buffer = _outputWriter.GetSpan(4);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)errorCode);
                _outputWriter.Advance(4);

                return _flusher.FlushAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.5.1
            List of:
            +-------------------------------+
            |       Identifier (16)         |
            +-------------------------------+-------------------------------+
            |                        Value (32)                             |
            +---------------------------------------------------------------+
        */
        public Task WriteSettingsAsync(IList<Http2PeerSetting> settings)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.NONE);
                var settingsSize = settings.Count * Http2FrameReader.SettingSize;
                _outgoingFrame.PayloadLength = settingsSize;
                WriteHeaderUnsynchronized();

                var buffer = _outputWriter.GetSpan(settingsSize).Slice(0, settingsSize); // GetSpan isn't precise
                WriteSettings(settings, buffer);
                _outputWriter.Advance(settingsSize);

                return _flusher.FlushAsync();
            }
        }

        internal static void WriteSettings(IList<Http2PeerSetting> settings, Span<byte> destination)
        {
            foreach (var setting in settings)
            {
                BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)setting.Parameter);
                BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(2), setting.Value);
                destination = destination.Slice(Http2FrameReader.SettingSize);
            }
        }

        // No payload
        public Task WriteSettingsAckAsync()
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.ACK);
                WriteHeaderUnsynchronized();
                return _flusher.FlushAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.7
            +---------------------------------------------------------------+
            |                                                               |
            |                      Opaque Data (64)                         |
            |                                                               |
            +---------------------------------------------------------------+
        */
        public Task WritePingAsync(Http2PingFrameFlags flags, ReadOnlySequence<byte> payload)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return Task.CompletedTask;
                }

                _outgoingFrame.PreparePing(Http2PingFrameFlags.ACK);
                Debug.Assert(payload.Length == _outgoingFrame.PayloadLength); // 8
                WriteHeaderUnsynchronized();
                foreach (var segment in payload)
                {
                    _outputWriter.Write(segment.Span);
                }

                return _flusher.FlushAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.8
            +-+-------------------------------------------------------------+
            |R|                  Last-Stream-ID (31)                        |
            +-+-------------------------------------------------------------+
            |                      Error Code (32)                          |
            +---------------------------------------------------------------+
            |                  Additional Debug Data (*)                    | (not implemented)
            +---------------------------------------------------------------+
        */
        public Task WriteGoAwayAsync(int lastStreamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                _outgoingFrame.PrepareGoAway(lastStreamId, errorCode);
                WriteHeaderUnsynchronized();

                var buffer = _outputWriter.GetSpan(8);
                Bitshifter.WriteUInt31BigEndian(buffer, (uint)lastStreamId);
                buffer = buffer.Slice(4);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)errorCode);
                _outputWriter.Advance(8);

                return _flusher.FlushAsync();
            }
        }

        private void WriteHeaderUnsynchronized()
        {
            _log.Http2FrameSending(_connectionId, _outgoingFrame);
            WriteHeader(_outgoingFrame, _outputWriter);
        }

        /* https://tools.ietf.org/html/rfc7540#section-4.1
            +-----------------------------------------------+
            |                 Length (24)                   |
            +---------------+---------------+---------------+
            |   Type (8)    |   Flags (8)   |
            +-+-------------+---------------+-------------------------------+
            |R|                 Stream Identifier (31)                      |
            +=+=============================================================+
            |                   Frame Payload (0...)                      ...
            +---------------------------------------------------------------+
        */
        internal static void WriteHeader(Http2Frame frame, PipeWriter output)
        {
            var buffer = output.GetSpan(Http2FrameReader.HeaderLength);

            Bitshifter.WriteUInt24BigEndian(buffer, (uint)frame.PayloadLength);
            buffer = buffer.Slice(3);

            buffer[0] = (byte)frame.Type;
            buffer[1] = frame.Flags;
            buffer = buffer.Slice(2);

            Bitshifter.WriteUInt31BigEndian(buffer, (uint)frame.StreamId);

            output.Advance(Http2FrameReader.HeaderLength);
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
