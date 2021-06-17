// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3FrameWriter
    {
        private readonly object _writeLock = new object();

        private IEnumerator<KeyValuePair<string, string>>? _headersEnumerator;
        private readonly PipeWriter _outputWriter;
        private readonly ConnectionContext _connectionContext;
        private readonly ITimeoutControl _timeoutControl;
        private readonly MinDataRate? _minResponseDataRate;
        private readonly string _connectionId;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly IKestrelTrace _log;
        private readonly IStreamIdFeature _streamIdFeature;
        private readonly IHttp3Stream _http3Stream;
        private readonly Http3RawFrame _outgoingFrame;
        private readonly TimingPipeFlusher _flusher;

        // TODO update max frame size
        private uint _maxFrameSize = 10000; //Http3PeerSettings.MinAllowedMaxFrameSize;
        private byte[] _headerEncodingBuffer;

        private long _unflushedBytes;
        private bool _completed;
        private bool _aborted;

        //private int _unflushedBytes;

        public Http3FrameWriter(PipeWriter output, ConnectionContext connectionContext, ITimeoutControl timeoutControl, MinDataRate? minResponseDataRate, string connectionId, MemoryPool<byte> memoryPool, IKestrelTrace log, IStreamIdFeature streamIdFeature, Http3PeerSettings clientPeerSettings, IHttp3Stream http3Stream)
        {
            _outputWriter = output;
            _connectionContext = connectionContext;
            _timeoutControl = timeoutControl;
            _minResponseDataRate = minResponseDataRate;
            _connectionId = connectionId;
            _memoryPool = memoryPool;
            _log = log;
            _streamIdFeature = streamIdFeature;
            _http3Stream = http3Stream;
            _outgoingFrame = new Http3RawFrame();
            _flusher = new TimingPipeFlusher(_outputWriter, timeoutControl, log);
            _headerEncodingBuffer = new byte[_maxFrameSize];
            _qpackEncoder = new QPackEncoder();

            // Note that QPack encoder doesn't react to settings change during a stream.
            // Unlikely to be a problem in practice:
            // - Settings rarely change after the start of a connection.
            // - Response header size limits are a best-effort requirement in the spec.
            _qpackEncoder.MaxTotalHeaderSize = clientPeerSettings.MaxRequestHeaderFieldSectionSize > int.MaxValue
                ? int.MaxValue
                : (int)clientPeerSettings.MaxRequestHeaderFieldSectionSize;
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

        internal Task WriteSettingsAsync(List<Http3PeerSetting> settings)
        {
            _outgoingFrame.PrepareSettings();

            // Calculate how long settings are before allocating.

            var settingsLength = CalculateSettingsSize(settings);

            // Call GetSpan with enough room for
            // - One encoded length int for setting size
            // - 1 byte for setting type
            // - settings length
            var buffer = _outputWriter.GetSpan(settingsLength + VariableLengthIntegerHelper.MaximumEncodedLength + 1);

            // Length start at 1 for type
            var totalLength = 1;

            // Write setting type
            buffer[0] = (byte)_outgoingFrame.Type;
            buffer = buffer[1..];

            // Write settings length
            var settingsBytesWritten = VariableLengthIntegerHelper.WriteInteger(buffer, settingsLength);
            buffer = buffer.Slice(settingsBytesWritten);

            totalLength += settingsBytesWritten + settingsLength;

            WriteSettings(settings, buffer);

            // Advance pipe writer and flush
            _outgoingFrame.Length = totalLength;
            _outputWriter.Advance(totalLength);

            return _outputWriter.FlushAsync().AsTask();
        }

        internal static int CalculateSettingsSize(List<Http3PeerSetting> settings)
        {
            var length = 0;
            foreach (var setting in settings)
            {
                length += VariableLengthIntegerHelper.GetByteCount((long)setting.Parameter);
                length += VariableLengthIntegerHelper.GetByteCount(setting.Value);
            }
            return length;
        }

        internal static void WriteSettings(List<Http3PeerSetting> settings, Span<byte> destination)
        {
            foreach (var setting in settings)
            {
                var parameterLength = VariableLengthIntegerHelper.WriteInteger(destination, (long)setting.Parameter);
                destination = destination.Slice(parameterLength);

                var valueLength = VariableLengthIntegerHelper.WriteInteger(destination, (long)setting.Value);
                destination = destination.Slice(valueLength);
            }
        }

        internal Task WriteStreamIdAsync(long id)
        {
            var buffer = _outputWriter.GetSpan(8);
            _outputWriter.Advance(VariableLengthIntegerHelper.WriteInteger(buffer, id));
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

                WriteDataUnsynchronized(data, dataLength);
                return TimeFlushUnsynchronizedAsync();
            }
        }

        private void WriteDataUnsynchronized(in ReadOnlySequence<byte> data, long dataLength)
        {
            Debug.Assert(dataLength == data.Length);

            _outgoingFrame.PrepareData();

            if (dataLength > _maxFrameSize)
            {
                SplitAndWriteDataUnsynchronized(in data, dataLength);
                return;
            }

            _outgoingFrame.Length = (int)dataLength;

            WriteHeaderUnsynchronized();

            foreach (var buffer in data)
            {
                _outputWriter.Write(buffer.Span);
            }

            return;

            void SplitAndWriteDataUnsynchronized(in ReadOnlySequence<byte> data, long dataLength)
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

        internal ValueTask<FlushResult> WriteGoAway(long id)
        {
            _outgoingFrame.PrepareGoAway();

            var length = VariableLengthIntegerHelper.GetByteCount(id);

            _outgoingFrame.Length = length;

            WriteHeaderUnsynchronized();

            var buffer = _outputWriter.GetSpan(8);
            VariableLengthIntegerHelper.WriteInteger(buffer, id);
            _outputWriter.Advance(length);
            return _outputWriter.FlushAsync();
        }

        private void WriteHeaderUnsynchronized()
        {
            _log.Http3FrameSending(_connectionId, _streamIdFeature.StreamId, _outgoingFrame);
            var headerLength = WriteHeader(_outgoingFrame, _outputWriter);

            // We assume the payload will be written prior to the next flush.
            _unflushedBytes += headerLength + _outgoingFrame.Length;
        }

        internal static int WriteHeader(Http3RawFrame frame, PipeWriter output)
        {
            // max size of the header is 16, most likely it will be smaller.
            var buffer = output.GetSpan(16);

            var typeLength = VariableLengthIntegerHelper.WriteInteger(buffer, (int)frame.Type);

            buffer = buffer.Slice(typeLength);

            var lengthLength = VariableLengthIntegerHelper.WriteInteger(buffer, (int)frame.Length);

            var totalLength = typeLength + lengthLength;
            output.Advance(typeLength + lengthLength);

            return totalLength;
        }

        public ValueTask<FlushResult> WriteResponseTrailersAsync(long streamId, HttpResponseTrailers headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                try
                {
                    _headersEnumerator = EnumerateHeaders(headers).GetEnumerator();

                    _outgoingFrame.PrepareHeaders();
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = QPackHeaderWriter.BeginEncode(_headersEnumerator, buffer, out var payloadLength);
                    FinishWritingHeaders(payloadLength, done);
                }
                catch (QPackEncodingException ex)
                {
                    _log.QPackEncodingError(_connectionId, streamId, ex);
                    _http3Stream.Abort(new ConnectionAbortedException(ex.Message, ex), Http3ErrorCode.InternalError);
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

        public ValueTask<FlushResult> FlushAsync(IHttpOutputAborter? outputAborter, CancellationToken cancellationToken)
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

        internal void WriteResponseHeaders(int statusCode, IHeaderDictionary headers)
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return;
                }

                try
                {
                    _headersEnumerator = EnumerateHeaders(headers).GetEnumerator();

                    _outgoingFrame.PrepareHeaders();
                    var buffer = _headerEncodingBuffer.AsSpan();
                    var done = QPackHeaderWriter.BeginEncode(statusCode, _headersEnumerator, buffer, out var payloadLength);
                    FinishWritingHeaders(payloadLength, done);
                }
                catch (QPackEncodingException ex)
                {
                    _http3Stream.Abort(new ConnectionAbortedException(ex.Message, ex), Http3ErrorCode.InternalError);
                    throw new InvalidOperationException(ex.Message, ex); // Report the error to the user if this was the first write.
                }
            }
        }

        private void FinishWritingHeaders(int payloadLength, bool done)
        {
            var buffer = _headerEncodingBuffer.AsSpan();
            _outgoingFrame.Length = payloadLength;

            WriteHeaderUnsynchronized();
            _outputWriter.Write(buffer.Slice(0, payloadLength));

            while (!done)
            {
                done = QPackHeaderWriter.Encode(_headersEnumerator!, buffer, out payloadLength);
                _outgoingFrame.Length = payloadLength;

                WriteHeaderUnsynchronized();
                _outputWriter.Write(buffer.Slice(0, payloadLength));
            }
        }

        public ValueTask CompleteAsync()
        {
            lock (_writeLock)
            {
                if (_completed)
                {
                    return default;
                }

                _completed = true;
                return _outputWriter.CompleteAsync();
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

                if (_completed)
                {
                    return;
                }

                _completed = true;
                _outputWriter.Complete();
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
