// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http.HPack;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

namespace Microsoft.AspNetCore.Http2Cat
{
    internal class Http2ManualFrameWriter
    {
        // Literal Header Field without Indexing - Indexed Name (Index 8 - :status)
        private static ReadOnlySpan<byte> ContinueBytes => new byte[] { 0x08, 0x03, (byte)'1', (byte)'0', (byte)'0' };

        private readonly object _writeLock = new object();
        private readonly Http2Frame _outgoingFrame;
        private readonly HPackEncoder _hpackEncoder = new HPackEncoder();
        private readonly ConnectionContext _connectionContext;

        private uint _maxFrameSize = Http2PeerSettings.MinAllowedMaxFrameSize;
        private byte[] _headerEncodingBuffer;
        private long _unflushedBytes;
        private PipeWriter _outputWriter;

        private bool _completed;
        private bool _aborted;

        public Http2ManualFrameWriter(
            PipeWriter outputPipeWriter,
            ConnectionContext connectionContext)
        {
            // Allow appending more data to the PipeWriter when a flush is pending.
            _outputWriter = outputPipeWriter;
            _connectionContext = connectionContext;
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
                _outputWriter.Complete(new Exception("Aborted"));
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

        public ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }
                
                var bytesWritten = _unflushedBytes;
                _unflushedBytes = 0;

                return _outputWriter.FlushAsync(cancellationToken);
            }
        }

        public ValueTask<FlushResult> Write100ContinueAsync(int streamId)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, streamId);
                _outgoingFrame.PayloadLength = ContinueBytes.Length;
                WriteHeaderUnsynchronized();
                _outputWriter.Write(ContinueBytes);
                return TimeFlushUnsynchronizedAsync();
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
        public void WriteResponseHeaders(int streamId, int statusCode, Http2HeadersFrameFlags headerFrameFlags, IHeaderDictionary headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                try
                {
                    _outgoingFrame.PrepareHeaders(headerFrameFlags, streamId);
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = _hpackEncoder.BeginEncode(statusCode, EnumerateHeaders(headers), buffer, out var payloadLength);
                    FinishWritingHeaders(streamId, payloadLength, done);
                }
                catch (HPackEncodingException hex)
                {
                    throw new InvalidOperationException(hex.Message, hex); // Report the error to the user if this was the first write.
                }
            }
        }

        private void FinishWritingHeaders(int streamId, int payloadLength, bool done)
        {
            var buffer = _headerEncodingBuffer.AsSpan();
            _outgoingFrame.PayloadLength = payloadLength;
            if (done)
            {
                _outgoingFrame.HeadersFlags |= Http2HeadersFrameFlags.END_HEADERS;
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

        public ValueTask<FlushResult> WriteDataAsync(int streamId, in ReadOnlySequence<byte> data, bool endStream, bool forceFlush)
        {
            // The Length property of a ReadOnlySequence can be expensive, so we cache the value.
            var dataLength = data.Length;

            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                WriteDataUnsynchronized(streamId, data, dataLength, endStream);

                if (forceFlush)
                {
                    return TimeFlushUnsynchronizedAsync();
                }

                return default;
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
        private void WriteDataUnsynchronized(int streamId, in ReadOnlySequence<byte> data, long dataLength, bool endStream)
        {
            Debug.Assert(dataLength == data.Length);

            // Note padding is not implemented
            _outgoingFrame.PrepareData(streamId);

            if (dataLength > _maxFrameSize) // Minus padding
            {
                TrimAndWriteDataUnsynchronized(in data, dataLength, endStream);
                return;
            }

            if (endStream)
            {
                _outgoingFrame.DataFlags |= Http2DataFrameFlags.END_STREAM;
            }

            _outgoingFrame.PayloadLength = (int)dataLength; // Plus padding

            WriteHeaderUnsynchronized();

            foreach (var buffer in data)
            {
                _outputWriter.Write(buffer.Span);
            }

            // Plus padding
            return;

            void TrimAndWriteDataUnsynchronized(in ReadOnlySequence<byte> data, long dataLength, bool endStream)
            {
                Debug.Assert(dataLength == data.Length);

                var dataPayloadLength = (int)_maxFrameSize; // Minus padding

                Debug.Assert(dataLength > dataPayloadLength);

                var remainingData = data;
                do
                {
                    var currentData = remainingData.Slice(0, dataPayloadLength);
                    _outgoingFrame.PayloadLength = dataPayloadLength; // Plus padding

                    WriteHeaderUnsynchronized();

                    foreach (var buffer in currentData)
                    {
                        _outputWriter.Write(buffer.Span);
                    }

                    // Plus padding
                    dataLength -= dataPayloadLength;
                    remainingData = remainingData.Slice(dataPayloadLength);

                } while (dataLength > dataPayloadLength);

                if (endStream)
                {
                    _outgoingFrame.DataFlags |= Http2DataFrameFlags.END_STREAM;
                }

                _outgoingFrame.PayloadLength = (int)dataLength; // Plus padding

                WriteHeaderUnsynchronized();

                foreach (var buffer in remainingData)
                {
                    _outputWriter.Write(buffer.Span);
                }

                // Plus padding
            }
        }

        private async ValueTask<FlushResult> WriteDataAsync(int streamId, ReadOnlySequence<byte> data, long dataLength, bool endStream)
        {
            FlushResult flushResult = default;

            while (dataLength > 0)
            {
                var writeTask = default(ValueTask<FlushResult>);

                lock (_writeLock)
                {
                    if (_completed)
                    {
                        break;
                    }

                    var actual = dataLength;

                    if (actual > 0)
                    {
                        if (actual < dataLength)
                        {
                            WriteDataUnsynchronized(streamId, data.Slice(0, actual), actual, endStream: false);
                            data = data.Slice(actual);
                            dataLength -= actual;
                        }
                        else
                        {
                            WriteDataUnsynchronized(streamId, data, actual, endStream);
                            dataLength = 0;
                        }

                        writeTask = _outputWriter.FlushAsync();
                    }

                    _unflushedBytes = 0;
                }

                // Avoid timing writes that are already complete. This is likely to happen during the last iteration.
                if (writeTask.IsCompleted)
                {
                    continue;
                }

                flushResult = await writeTask;
            }

            return flushResult;
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.9
            +-+-------------------------------------------------------------+
            |R|              Window Size Increment (31)                     |
            +-+-------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WriteWindowUpdateAsync(int streamId, int sizeIncrement)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareWindowUpdate(streamId, sizeIncrement);
                WriteHeaderUnsynchronized();
                var buffer = _outputWriter.GetSpan(4);
                Bitshifter.WriteUInt31BigEndian(buffer, (uint)sizeIncrement, preserveHighestBit: false);
                _outputWriter.Advance(4);
                return TimeFlushUnsynchronizedAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.4
            +---------------------------------------------------------------+
            |                        Error Code (32)                        |
            +---------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WriteRstStreamAsync(int streamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareRstStream(streamId, errorCode);
                WriteHeaderUnsynchronized();
                var buffer = _outputWriter.GetSpan(4);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)errorCode);
                _outputWriter.Advance(4);

                return TimeFlushUnsynchronizedAsync();
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
        public ValueTask<FlushResult> WriteSettingsAsync(IList<Http2PeerSetting> settings)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.NONE);
                var settingsSize = settings.Count * Http2FrameReader.SettingSize;
                _outgoingFrame.PayloadLength = settingsSize;
                WriteHeaderUnsynchronized();

                var buffer = _outputWriter.GetSpan(settingsSize).Slice(0, settingsSize); // GetSpan isn't precise
                WriteSettings(settings, buffer);
                _outputWriter.Advance(settingsSize);

                return TimeFlushUnsynchronizedAsync();
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
        public ValueTask<FlushResult> WriteSettingsAckAsync()
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareSettings(Http2SettingsFrameFlags.ACK);
                WriteHeaderUnsynchronized();
                return TimeFlushUnsynchronizedAsync();
            }
        }

        /* https://tools.ietf.org/html/rfc7540#section-6.7
            +---------------------------------------------------------------+
            |                                                               |
            |                      Opaque Data (64)                         |
            |                                                               |
            +---------------------------------------------------------------+
        */
        public ValueTask<FlushResult> WritePingAsync(Http2PingFrameFlags flags, in ReadOnlySequence<byte> payload)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PreparePing(flags);
                Debug.Assert(payload.Length == _outgoingFrame.PayloadLength); // 8
                WriteHeaderUnsynchronized();
                foreach (var segment in payload)
                {
                    _outputWriter.Write(segment.Span);
                }

                return TimeFlushUnsynchronizedAsync();
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
        public ValueTask<FlushResult> WriteGoAwayAsync(int lastStreamId, Http2ErrorCode errorCode)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _outgoingFrame.PrepareGoAway(lastStreamId, errorCode);
                WriteHeaderUnsynchronized();

                var buffer = _outputWriter.GetSpan(8);
                Bitshifter.WriteUInt31BigEndian(buffer, (uint)lastStreamId, preserveHighestBit: false);
                buffer = buffer.Slice(4);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)errorCode);
                _outputWriter.Advance(8);

                return TimeFlushUnsynchronizedAsync();
            }
        }

        private void WriteHeaderUnsynchronized()
        {
            WriteHeader(_outgoingFrame, _outputWriter);

            // We assume the payload will be written prior to the next flush.
            _unflushedBytes += Http2FrameReader.HeaderLength + _outgoingFrame.PayloadLength;
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

            Bitshifter.WriteUInt31BigEndian(buffer, (uint)frame.StreamId, preserveHighestBit: false);

            output.Advance(Http2FrameReader.HeaderLength);
        }

        private ValueTask<FlushResult> TimeFlushUnsynchronizedAsync()
        {
            _unflushedBytes = 0;

            return _outputWriter.FlushAsync();
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
