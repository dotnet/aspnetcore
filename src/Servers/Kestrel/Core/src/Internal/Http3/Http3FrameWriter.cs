// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3FrameWriter
    {
        private readonly object _writeLock = new object();
        private readonly QPackEncoder _qpackEncoder = new QPackEncoder();

        private readonly PipeWriter _outputWriter;
        private readonly ConnectionContext _connectionContext;
        private readonly IHttp3Stream _http3Stream;
        private readonly ITimeoutControl _timeoutControl;
        private readonly MinDataRate _minResponseDataRate;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly IKestrelTrace _log;
        private readonly Http3Frame _outgoingFrame;
        private readonly TimingPipeFlusher _flusher;

        // TODO update max frame size
        private uint _maxFrameSize = 10000; //Http3PeerSettings.MinAllowedMaxFrameSize;
        private byte[] _headerEncodingBuffer;

        private long _unflushedBytes;
        private bool _completed;
        private bool _aborted;

        //private int _unflushedBytes;

        public Http3FrameWriter(PipeWriter output, ConnectionContext connectionContext, IHttp3Stream http3Stream, ITimeoutControl timeoutControl, MinDataRate minResponseDataRate, string connectionId, MemoryPool<byte> memoryPool, IKestrelTrace log)
        {
            _outputWriter = output;
            _connectionContext = connectionContext;
            _http3Stream = http3Stream;
            _timeoutControl = timeoutControl;
            _minResponseDataRate = minResponseDataRate;
            _memoryPool = memoryPool;
            _log = log;
            _outgoingFrame = new Http3Frame();
            _flusher = new TimingPipeFlusher(_outputWriter, timeoutControl, log);
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

        // TODO actually write settings here.
        internal Task WriteSettingsAsync(IList<Http3PeerSettings> settings)
        {
            _outgoingFrame.PrepareSettings();
            _outgoingFrame.Length = 0;
            var buffer = _outputWriter.GetSpan(1);
            buffer[0] = (byte)_outgoingFrame.Type;
            buffer[1] = 0;
            _outputWriter.Advance(2);

            return _outputWriter.FlushAsync().AsTask();
        }

        internal Task WriteStreamIdAsync(long id)
        {
            var buffer = _outputWriter.GetSpan(8);
            _outputWriter.Advance(VariableLengthIntegerHelper.WriteEncodedIntegerToSpan(buffer, id));
            return _outputWriter.FlushAsync().AsTask();
        }

        public ValueTask<FlushResult> WriteDataAsync(in ReadOnlySequence<byte> data)
        {
            // The Length property of a ReadOnlySequence can be expensive, so we cache the value.
            var dataLength = data.Length;

            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                // This cast is safe since if dataLength would overflow an int, it's guaranteed to be greater than the available flow control window.
                WriteDataUnsynchronized(data, dataLength);
                return TimeFlushUnsynchronizedAsync();
            }
        }

        private void WriteDataUnsynchronized(in ReadOnlySequence<byte> data, long dataLength)
        {
            Debug.Assert(dataLength == data.Length);

            // Note padding is not implemented
            _outgoingFrame.PrepareData();

            if (dataLength > _maxFrameSize) // Minus padding
            {
                TrimAndWriteDataUnsynchronized(in data, dataLength);
                return;
            }

            _outgoingFrame.Length = (int)dataLength;

            WriteHeaderUnsynchronized();

            foreach (var buffer in data)
            {
                _outputWriter.Write(buffer.Span);
            }

            // Plus padding
            return;

            void TrimAndWriteDataUnsynchronized(in ReadOnlySequence<byte> data, long dataLength)
            {
                Debug.Assert(dataLength == data.Length);

                var dataPayloadLength = (int)_maxFrameSize;

                Debug.Assert(dataLength > dataPayloadLength);

                var remainingData = data;
                do
                {
                    var currentData = remainingData.Slice(0, dataPayloadLength);
                    _outgoingFrame.Length = dataPayloadLength;

                    WriteHeaderUnsynchronized();

                    foreach (var buffer in currentData)
                    {
                        _outputWriter.Write(buffer.Span);
                    }

                    dataLength -= dataPayloadLength;
                    remainingData = remainingData.Slice(dataPayloadLength);

                } while (dataLength > dataPayloadLength);

                _outgoingFrame.Length = (int)dataLength;

                WriteHeaderUnsynchronized();

                foreach (var buffer in remainingData)
                {
                    _outputWriter.Write(buffer.Span);
                }
            }
        }

        private void WriteHeaderUnsynchronized()
        {
            var headerLength = WriteHeader(_outgoingFrame, _outputWriter);

            // We assume the payload will be written prior to the next flush.
            _unflushedBytes += headerLength + _outgoingFrame.Length;
        }

        internal static int WriteHeader(Http3Frame frame, PipeWriter output)
        {
            // max size of the header is 16, most likely it will be smaller.
            var buffer = output.GetSpan(16);

            var typeLength = VariableLengthIntegerHelper.WriteEncodedIntegerToSpan(buffer, (int)frame.Type);

            buffer = buffer.Slice(typeLength);

            var lengthLength = VariableLengthIntegerHelper.WriteEncodedIntegerToSpan(buffer, (int)frame.Length);

            var totalLength = typeLength + lengthLength;
            output.Advance(typeLength + lengthLength);

            return totalLength;
        }

        public ValueTask<FlushResult> WriteResponseTrailers(int streamId, HttpResponseTrailers headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                try
                {
                    _outgoingFrame.PrepareHeaders();
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = _qpackEncoder.BeginEncode(EnumerateHeaders(headers), buffer, out var payloadLength);
                    FinishWritingHeaders(streamId, payloadLength, done);
                }
                catch (QPackEncodingException hex)
                {
                    //_log.HPackEncodingError(_connectionId, streamId, hex);
                    _http3Stream.Abort(new ConnectionAbortedException(hex.Message, hex));
                }

                return TimeFlushUnsynchronizedAsync();
            }
        }

        private ValueTask<FlushResult> TimeFlushUnsynchronizedAsync()
        {
            var bytesWritten = _unflushedBytes;
            _unflushedBytes = 0;

            return _flusher.FlushAsync(_minResponseDataRate, bytesWritten);
        }

        public ValueTask<FlushResult> FlushAsync(IHttpOutputAborter outputAborter, CancellationToken cancellationToken)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                var bytesWritten = _unflushedBytes;
                _unflushedBytes = 0;

                return _flusher.FlushAsync(_minResponseDataRate, bytesWritten, outputAborter, cancellationToken);
            }
        }

        internal void WriteResponseHeaders(int streamId, int statusCode, IHeaderDictionary headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                try
                {
                    _outgoingFrame.PrepareHeaders();
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = _qpackEncoder.BeginEncode(statusCode, EnumerateHeaders(headers), buffer, out var payloadLength);
                    FinishWritingHeaders(streamId, payloadLength, done);
                }
                catch (QPackEncodingException hex)
                {
                    _http3Stream.Abort(new ConnectionAbortedException(hex.Message, hex));
                    throw new InvalidOperationException(hex.Message, hex); // Report the error to the user if this was the first write.
                }
            }
        }

        private void FinishWritingHeaders(int streamId, int payloadLength, bool done)
        {
            var buffer = _headerEncodingBuffer.AsSpan();
            _outgoingFrame.Length = payloadLength;

            WriteHeaderUnsynchronized();
            _outputWriter.Write(buffer.Slice(0, payloadLength));

            while (!done)
            {
                done = _qpackEncoder.Encode(buffer, out payloadLength);
                _outgoingFrame.Length = payloadLength;

                WriteHeaderUnsynchronized();
                _outputWriter.Write(buffer.Slice(0, payloadLength));
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
