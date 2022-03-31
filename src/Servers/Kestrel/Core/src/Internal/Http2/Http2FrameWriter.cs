// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http.HPack;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal class Http2FrameWriter
{
    // Literal Header Field without Indexing - Indexed Name (Index 8 - :status)
    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> ContinueBytes => new byte[] { 0x08, 0x03, (byte)'1', (byte)'0', (byte)'0' };

    private readonly object _writeLock = new object();
    private readonly Http2Frame _outgoingFrame;
    private readonly Http2HeadersEnumerator _headersEnumerator = new Http2HeadersEnumerator();
    private readonly ConcurrentPipeWriter _outputWriter;
    private readonly BaseConnectionContext _connectionContext;
    private readonly Http2Connection _http2Connection;
    private readonly string _connectionId;
    private readonly KestrelTrace _log;
    private readonly ITimeoutControl _timeoutControl;
    private readonly MinDataRate? _minResponseDataRate;
    private readonly TimingPipeFlusher _flusher;
    private readonly DynamicHPackEncoder _hpackEncoder;
    private readonly Channel<Http2OutputProducer> _channel;

    // This is only set to true by tests.
    private readonly bool _scheduleInline;

    private uint _maxFrameSize = Http2PeerSettings.MinAllowedMaxFrameSize;
    private byte[] _headerEncodingBuffer;
    private long _unflushedBytes;

    private bool _completed;
    private bool _aborted;

    private readonly object _windowUpdateLock = new();
    private long _window;
    private readonly Queue<Http2OutputProducer> _waitingForMoreWindow = new();
    private readonly Task _writeQueueTask;

    public Http2FrameWriter(
        PipeWriter outputPipeWriter,
        BaseConnectionContext connectionContext,
        Http2Connection http2Connection,
        long initialWindowSize,
        ITimeoutControl timeoutControl,
        MinDataRate? minResponseDataRate,
        string connectionId,
        MemoryPool<byte> memoryPool,
        ServiceContext serviceContext)
    {
        // Allow appending more data to the PipeWriter when a flush is pending.
        _outputWriter = new ConcurrentPipeWriter(outputPipeWriter, memoryPool, _writeLock);
        _connectionContext = connectionContext;
        _http2Connection = http2Connection;
        _connectionId = connectionId;
        _log = serviceContext.Log;
        _timeoutControl = timeoutControl;
        _minResponseDataRate = minResponseDataRate;
        _flusher = new TimingPipeFlusher(timeoutControl, serviceContext.Log);
        _flusher.Initialize(_outputWriter);
        _outgoingFrame = new Http2Frame();
        _headerEncodingBuffer = new byte[_maxFrameSize];

        _scheduleInline = serviceContext.Scheduler == PipeScheduler.Inline;

        _hpackEncoder = new DynamicHPackEncoder(serviceContext.ServerOptions.AllowResponseHeaderCompression);

        // In practice, this is bounded by the number of concurrent streams allowed + whatever overflow we allow
        _channel = Channel.CreateUnbounded<Http2OutputProducer>(new UnboundedChannelOptions()
        {
            AllowSynchronousContinuations = _scheduleInline,
            SingleReader = true
        });

        _window = initialWindowSize;

        _writeQueueTask = Task.Run(WriteToOutputPipe);
    }

    public void Schedule(Http2OutputProducer producer)
    {
        _channel.Writer.TryWrite(producer);
    }

    private async Task WriteToOutputPipe()
    {
        await foreach (var producer in _channel.Reader.ReadAllAsync())
        {
            try
            {
                var reader = producer.PipeReader;
                var stream = producer.Stream;

                var observed = Http2OutputProducer.State.None;

                if (!reader.TryRead(out var readResult))
                {
                    if (producer.WriteHeaders)
                    {
                        // Flush headers, we have nothing to look at on the pipe
                    }
                    else
                    {
                        readResult = await reader.ReadAsync();
                    }
                }

                var buffer = readResult.Buffer;

                if (producer.WriteHeaders)
                {
                    observed |= Http2OutputProducer.State.FlushHeaders;
                }

                if (readResult.IsCanceled)
                {
                    observed |= Http2OutputProducer.State.Canceled;
                }

                if (readResult.IsCompleted)
                {
                    observed |= Http2OutputProducer.State.Completed;
                }

                // Check the stream window
                var (actual, remainingStream) = producer.ConsumeWindow(buffer.Length);

                // Now check the connection window
                (actual, var remainingConnection) = ConsumeWindow(actual);

                // Write what we can
                if (actual < buffer.Length)
                {
                    buffer = buffer.Slice(0, actual);
                }

                var (hasMoreData, reschedule) = producer.Dequeue(buffer.Length, observed);

                FlushResult flushResult = default;

                if (readResult.IsCanceled)
                {
                    // Response body is aborted, complete reader for this output producer.
                    if (producer.WriteHeaders)
                    {
                        // write headers
                        WriteResponseHeaders(stream.StreamId, stream.StatusCode, Http2HeadersFrameFlags.NONE, (HttpResponseHeaders)stream.ResponseHeaders);
                    }
                }
                else if (readResult.IsCompleted && stream.ResponseTrailers is { Count: > 0 } && !hasMoreData)
                {
                    // Output is ending and there are trailers to write
                    // Write any remaining content then write trailers and there's no
                    // flow control back pressure being applied (hasMoreData)

                    stream.ResponseTrailers.SetReadOnly();
                    stream.DecrementActiveClientStreamCount();

                    // It is faster to write data and trailers together. Locking once reduces lock contention.
                    flushResult = await WriteDataAndTrailersAsync(stream, buffer, producer.WriteHeaders, stream.ResponseTrailers);
                }
                else if (readResult.IsCompleted && producer.StreamEnded)
                {
                    if (buffer.Length != 0)
                    {
                        // TODO: Use the right logger here.
                        _log.LogCritical(nameof(Http2OutputProducer) + "." + " observed an unexpected state where the streams output ended with data still remaining in the pipe.");
                    }
                    else
                    {
                        stream.DecrementActiveClientStreamCount();

                        // Headers have already been written and there is no other content to write
                        flushResult = await FlushAsync(stream, producer.WriteHeaders, outputAborter: null, cancellationToken: default);
                    }
                }
                else
                {
                    var endStream = readResult.IsCompleted && !hasMoreData;

                    if (endStream)
                    {
                        stream.DecrementActiveClientStreamCount();
                    }

                    flushResult = await WriteDataAsync(stream, buffer, buffer.Length, endStream, producer.WriteHeaders);
                }

                if (producer.IsTimingWrite)
                {
                    _timeoutControl.StopTimingWrite();
                }

                producer.WriteHeaders = false;

                reader.AdvanceTo(buffer.End);

                if ((readResult.IsCompleted && !hasMoreData) || readResult.IsCanceled)
                {
                    await reader.CompleteAsync();

                    producer.CompleteResponse();
                }
                // We're not going to schedule this again if there's no remaining window.
                // When the window update is sent, the producer will be re-queued if needed.
                else if (hasMoreData)
                {
                    // We have no more connection window, put this producer in a queue waiting for it to
                    // a window update to resume the connection.
                    if (remainingConnection == 0)
                    {
                        // Mark the output as waiting for a window upate to resume writing (there's still data)
                        producer.MarkWaitingForWindowUpdates(true);

                        lock (_windowUpdateLock)
                        {
                            _waitingForMoreWindow.Enqueue(producer);
                        }

                        // Include waiting for window updates in timing writes
                        if (_minResponseDataRate != null)
                        {
                            producer.IsTimingWrite = true;
                            _timeoutControl.StartTimingWrite();
                        }
                    }
                    else if (remainingStream > 0)
                    {
                        // Move this stream to the back of the queue so we're being fair to the other streams that have data
                        Schedule(producer);
                    }
                    else
                    {
                        // Mark the output as waiting for a window upate to resume writing (there's still data)
                        producer.MarkWaitingForWindowUpdates(true);

                        // Include waiting for window updates in timing writes
                        if (_minResponseDataRate != null)
                        {
                            producer.IsTimingWrite = true;
                            _timeoutControl.StartTimingWrite();
                        }
                    }
                }
                else if (reschedule)
                {
                    Schedule(producer);
                }
            }
            catch (Exception ex)
            {
                _log.LogCritical(ex, "The event loop in connection {ConnectionId} failed unexpectedly", _connectionId);
            }
        }

        _log.LogDebug("The connection processing loop for {ConnectionId} ended gracefully", _connectionId);
    }

    public void UpdateMaxHeaderTableSize(uint maxHeaderTableSize)
    {
        lock (_writeLock)
        {
            _hpackEncoder.UpdateMaxHeaderTableSize(maxHeaderTableSize);
        }
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
            AbortFlowControl();
            _outputWriter.Abort();
        }
    }

    public Task ShutdownAsync()
    {
        _channel.Writer.TryComplete();

        return _writeQueueTask;
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

    private ValueTask<FlushResult> FlushAsync(Http2Stream stream, bool writeHeaders, IHttpOutputAborter? outputAborter, CancellationToken cancellationToken)
    {
        lock (_writeLock)
        {
            if (_completed)
            {
                return default;
            }

            if (writeHeaders)
            {
                // write headers
                WriteResponseHeadersUnsynchronized(stream.StreamId, stream.StatusCode, Http2HeadersFrameFlags.END_STREAM, (HttpResponseHeaders)stream.ResponseHeaders);
            }

            var bytesWritten = _unflushedBytes;
            _unflushedBytes = 0;

            return _flusher.FlushAsync(_minResponseDataRate, bytesWritten, outputAborter, cancellationToken);
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
    public void WriteResponseHeaders(int streamId, int statusCode, Http2HeadersFrameFlags headerFrameFlags, HttpResponseHeaders headers)
    {
        lock (_writeLock)
        {
            if (_completed)
            {
                return;
            }

            WriteResponseHeadersUnsynchronized(streamId, statusCode, headerFrameFlags, headers);
        }
    }

    private void WriteResponseHeadersUnsynchronized(int streamId, int statusCode, Http2HeadersFrameFlags headerFrameFlags, HttpResponseHeaders headers)
    {
        try
        {
            _headersEnumerator.Initialize(headers);
            _outgoingFrame.PrepareHeaders(headerFrameFlags, streamId);
            var buffer = _headerEncodingBuffer.AsSpan();
            var done = HPackHeaderWriter.BeginEncodeHeaders(statusCode, _hpackEncoder, _headersEnumerator, buffer, out var payloadLength);
            FinishWritingHeaders(streamId, payloadLength, done);
        }
        // Any exception from the HPack encoder can leave the dynamic table in a corrupt state.
        // Since we allow custom header encoders we don't know what type of exceptions to expect.
        catch (Exception ex)
        {
            _log.HPackEncodingError(_connectionId, streamId, ex);
            _http2Connection.Abort(new ConnectionAbortedException(ex.Message, ex));
        }
    }

    private ValueTask<FlushResult> WriteDataAndTrailersAsync(Http2Stream stream, in ReadOnlySequence<byte> data, bool writeHeaders, HttpResponseTrailers headers)
    {
        // The Length property of a ReadOnlySequence can be expensive, so we cache the value.
        var dataLength = data.Length;

        lock (_writeLock)
        {
            if (_completed)
            {
                return default;
            }

            var streamId = stream.StreamId;

            if (writeHeaders)
            {
                WriteResponseHeadersUnsynchronized(streamId, stream.StatusCode, Http2HeadersFrameFlags.NONE, (HttpResponseHeaders)stream.ResponseHeaders);
            }

            if (dataLength > 0)
            {
                WriteDataUnsynchronized(streamId, data, dataLength, endStream: false);
            }

            try
            {
                _headersEnumerator.Initialize(headers);
                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.END_STREAM, streamId);
                var buffer = _headerEncodingBuffer.AsSpan();
                var done = HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _headersEnumerator, buffer, out var payloadLength);
                FinishWritingHeaders(streamId, payloadLength, done);
            }
            // Any exception from the HPack encoder can leave the dynamic table in a corrupt state.
            // Since we allow custom header encoders we don't know what type of exceptions to expect.
            catch (Exception ex)
            {
                _log.HPackEncodingError(_connectionId, streamId, ex);
                _http2Connection.Abort(new ConnectionAbortedException(ex.Message, ex));
            }

            return TimeFlushUnsynchronizedAsync();
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

            done = HPackHeaderWriter.ContinueEncodeHeaders(_hpackEncoder, _headersEnumerator, buffer, out payloadLength);
            _outgoingFrame.PayloadLength = payloadLength;

            if (done)
            {
                _outgoingFrame.ContinuationFlags = Http2ContinuationFrameFlags.END_HEADERS;
            }

            WriteHeaderUnsynchronized();
            _outputWriter.Write(buffer.Slice(0, payloadLength));
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

        data.CopyTo(_outputWriter);

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

    private ValueTask<FlushResult> WriteDataAsync(Http2Stream stream, ReadOnlySequence<byte> data, long dataLength, bool endStream, bool writeHeaders)
    {
        var writeTask = default(ValueTask<FlushResult>);

        lock (_writeLock)
        {
            if (_completed)
            {
                return ValueTask.FromResult<FlushResult>(default);
            }

            var shouldFlush = false;

            if (writeHeaders)
            {
                WriteResponseHeadersUnsynchronized(stream.StreamId, stream.StatusCode, Http2HeadersFrameFlags.NONE, (HttpResponseHeaders)stream.ResponseHeaders);

                shouldFlush = true;
            }

            if (dataLength > 0 || endStream)
            {
                WriteDataUnsynchronized(stream.StreamId, data, dataLength, endStream);

                shouldFlush = true;
            }

            if (_minResponseDataRate != null)
            {
                // Call BytesWrittenToBuffer before FlushAsync() to make testing easier, otherwise the Flush can cause test code to run before the timeout
                // control updates and if the test checks for a timeout it can fail
                _timeoutControl.BytesWrittenToBuffer(_minResponseDataRate, _unflushedBytes);
            }

            if (shouldFlush)
            {
                _unflushedBytes = 0;

                writeTask = _flusher.FlushAsync();
            }
        }

        if (writeTask.IsCompletedSuccessfully)
        {
            return new(writeTask.Result);
        }

        return FlushAsyncAwaited(writeTask, _timeoutControl, _minResponseDataRate);

        static async ValueTask<FlushResult> FlushAsyncAwaited(ValueTask<FlushResult> writeTask, ITimeoutControl timeoutControl, MinDataRate? minResponseDataRate)
        {
            if (minResponseDataRate != null)
            {
                timeoutControl.StartTimingWrite();
            }

            var flushResult = await writeTask;

            if (minResponseDataRate != null)
            {
                timeoutControl.StopTimingWrite();
            }
            return flushResult;
        }
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
    public ValueTask<FlushResult> WriteSettingsAsync(List<Http2PeerSetting> settings)
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

    internal static void WriteSettings(List<Http2PeerSetting> settings, Span<byte> destination)
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
        _log.Http2FrameSending(_connectionId, _outgoingFrame);
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
        var bytesWritten = _unflushedBytes;
        _unflushedBytes = 0;

        return _flusher.FlushAsync(_minResponseDataRate, bytesWritten);
    }

    internal (long, long) ConsumeWindow(long bytes)
    {
        lock (_windowUpdateLock)
        {
            var actual = Math.Min(bytes, _window);
            var remaining = _window -= actual;

            return (actual, remaining);
        }
    }

    private void AbortFlowControl()
    {
        lock (_windowUpdateLock)
        {
            while (_waitingForMoreWindow.TryDequeue(out var producer))
            {
                if (!producer.StreamCompleted)
                {
                    // Stop the output
                    producer.Stop();
                }
            }
        }
    }

    public bool TryUpdateConnectionWindow(int bytes)
    {
        lock (_windowUpdateLock)
        {
            var maxUpdate = Http2PeerSettings.MaxWindowSize - _window;

            if (bytes > maxUpdate)
            {
                return false;
            }

            _window += bytes;

            while (_waitingForMoreWindow.TryDequeue(out var producer))
            {
                if (!producer.StreamCompleted)
                {
                    // We're no longer waiting for the update
                    producer.MarkWaitingForWindowUpdates(false);

                    Schedule(producer);
                }
            }
        }
        return true;
    }
}
