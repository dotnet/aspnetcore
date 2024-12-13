// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3FrameWriter
{
    // These bytes represent a ":status: 100" continue response header frame encoded with
    // QPACK. To arrive at this, we first take the index in the QPACK static table for status
    // 100 (https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#appendix-A), which
    // is 63, and encode it to get ff 00 (see QPackEncoder.EncodeStaticIndexedHeaderField).
    // The two zero bytes are for the section prefix
    // (https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#header-prefix)
    private static ReadOnlySpan<byte> ContinueBytes => [0x00, 0x00, 0xff, 0x00];

    // Size based on HTTP/2 default frame size
    private const int MaxDataFrameSize = 16 * 1024;
    private const int HeaderBufferSize = 16 * 1024;

    private readonly Lock _writeLock = new();

    private readonly int _maxTotalHeaderSize;
    private readonly ConnectionContext _connectionContext;
    private readonly ITimeoutControl _timeoutControl;
    private readonly MinDataRate? _minResponseDataRate;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly KestrelTrace _log;
    private readonly IStreamIdFeature _streamIdFeature;
    private readonly IHttp3Stream _http3Stream;
    private readonly Http3RawFrame _outgoingFrame;
    private readonly TimingPipeFlusher _flusher;

    private PipeWriter _outputWriter = default!;
    private string _connectionId = default!;

    // HTTP/3 doesn't have a max frame size (peer can optionally specify a size).
    // Write headers to a buffer that can grow. Possible performance improvement
    // by writing directly to output writer (difficult as frame length is prefixed).
    private readonly ArrayBufferWriter<byte> _headerEncodingBuffer;
    private readonly Http3HeadersEnumerator _headersEnumerator = new();
    private int _headersTotalSize;

    private long _unflushedBytes;
    private bool _completed;
    private bool _aborted;

    public Http3FrameWriter(ConnectionContext connectionContext, ITimeoutControl timeoutControl, MinDataRate? minResponseDataRate, MemoryPool<byte> memoryPool, KestrelTrace log, IStreamIdFeature streamIdFeature, Http3PeerSettings clientPeerSettings, IHttp3Stream http3Stream)
    {
        _connectionContext = connectionContext;
        _timeoutControl = timeoutControl;
        _minResponseDataRate = minResponseDataRate;
        _memoryPool = memoryPool;
        _log = log;
        _streamIdFeature = streamIdFeature;
        _http3Stream = http3Stream;
        _outgoingFrame = new Http3RawFrame();
        _flusher = new TimingPipeFlusher(timeoutControl, log);
        _headerEncodingBuffer = new ArrayBufferWriter<byte>(HeaderBufferSize);

        // Note that max total header size value doesn't react to settings change during a stream.
        // Unlikely to be a problem in practice:
        // - Settings rarely change after the start of a connection.
        // - Response header size limits are a best-effort requirement in the spec.
        _maxTotalHeaderSize = clientPeerSettings.MaxRequestHeaderFieldSectionSize > int.MaxValue
            ? int.MaxValue
            : (int)clientPeerSettings.MaxRequestHeaderFieldSectionSize;
    }

    public void Reset(PipeWriter output, string connectionId)
    {
        _outputWriter = output;
        _flusher.Initialize(output);
        _connectionId = connectionId;

        _headersTotalSize = 0;
        _headerEncodingBuffer.Clear();
        _unflushedBytes = 0;
        _completed = false;
        _aborted = false;
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

        return _outputWriter.FlushAsync().GetAsTask();
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
        return _outputWriter.FlushAsync().GetAsTask();
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

        if (dataLength > MaxDataFrameSize)
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

            var dataPayloadLength = (int)MaxDataFrameSize;

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
        var headerLength = WriteHeader(_outgoingFrame.Type, _outgoingFrame.Length, _outputWriter);

        // We assume the payload will be written prior to the next flush.
        _unflushedBytes += headerLength + _outgoingFrame.Length;
    }

    public ValueTask<FlushResult> Write100ContinueAsync()
    {
        lock (_writeLock)
        {
            if (_completed)
            {
                return default;
            }

            _outgoingFrame.PrepareHeaders();
            _outgoingFrame.Length = ContinueBytes.Length;
            WriteHeaderUnsynchronized();
            _outputWriter.Write(ContinueBytes);
            return TimeFlushUnsynchronizedAsync();
        }
    }

    internal static int WriteHeader(Http3FrameType frameType, long frameLength, PipeWriter output)
    {
        // max size of the header is 16, most likely it will be smaller.
        var buffer = output.GetSpan(16);

        var typeLength = VariableLengthIntegerHelper.WriteInteger(buffer, (int)frameType);

        buffer = buffer.Slice(typeLength);

        var lengthLength = VariableLengthIntegerHelper.WriteInteger(buffer, (int)frameLength);

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
                _headersEnumerator.Initialize(headers);
                _headersTotalSize = 0;
                _headerEncodingBuffer.Clear();

                _outgoingFrame.PrepareHeaders();
                var buffer = _headerEncodingBuffer.GetSpan(HeaderBufferSize);
                var done = QPackHeaderWriter.BeginEncodeHeaders(_headersEnumerator, buffer, ref _headersTotalSize, out var payloadLength);
                FinishWritingHeaders(payloadLength, done);
            }
            // Any exception from the QPack encoder can leave the dynamic table in a corrupt state.
            // Since we allow custom header encoders we don't know what type of exceptions to expect.
            catch (Exception ex)
            {
                _log.QPackEncodingError(_connectionId, streamId, ex);
                _connectionContext.Abort(new ConnectionAbortedException(ex.Message, ex));
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

    internal void WriteResponseHeaders(int statusCode, HttpResponseHeaders headers)
    {
        lock (_writeLock)
        {
            if (_completed)
            {
                return;
            }

            try
            {
                _headersEnumerator.Initialize(headers);

                _outgoingFrame.PrepareHeaders();
                var buffer = _headerEncodingBuffer.GetSpan(HeaderBufferSize);
                var done = QPackHeaderWriter.BeginEncodeHeaders(statusCode, _headersEnumerator, buffer, ref _headersTotalSize, out var payloadLength);
                FinishWritingHeaders(payloadLength, done);
            }
            // Any exception from the QPack encoder can leave the dynamic table in a corrupt state.
            // Since we allow custom header encoders we don't know what type of exceptions to expect.
            catch (Exception ex)
            {
                _log.QPackEncodingError(_connectionId, _http3Stream.StreamId, ex);
                _connectionContext.Abort(new ConnectionAbortedException(ex.Message, ex));
                _http3Stream.Abort(new ConnectionAbortedException(ex.Message, ex), Http3ErrorCode.InternalError);
                throw new InvalidOperationException(ex.Message, ex); // Report the error to the user if this was the first write.
            }
        }
    }

    private void FinishWritingHeaders(int payloadLength, bool done)
    {
        _headerEncodingBuffer.Advance(payloadLength);

        while (!done)
        {
            ValidateHeadersTotalSize();
            var buffer = _headerEncodingBuffer.GetSpan(HeaderBufferSize);
            done = QPackHeaderWriter.Encode(_headersEnumerator!, buffer, ref _headersTotalSize, out payloadLength);
            _headerEncodingBuffer.Advance(payloadLength);
        }

        ValidateHeadersTotalSize();

        _outgoingFrame.Length = _headerEncodingBuffer.WrittenCount;
        WriteHeaderUnsynchronized();
        _outputWriter.Write(_headerEncodingBuffer.WrittenSpan);

        void ValidateHeadersTotalSize()
        {
            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.3
            if (_headersTotalSize > _maxTotalHeaderSize)
            {
                throw new QPackEncodingException($"The encoded HTTP headers length exceeds the limit specified by the peer of {_maxTotalHeaderSize} bytes.");
            }
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
}
