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

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class Http2FrameWriter
{
    // Literal Header Field without Indexing - Indexed Name (Index 8 - :status)
    private static ReadOnlySpan<byte> ContinueBytes => [0x08, 0x03, (byte)'1', (byte)'0', (byte)'0'];

    /// Increase this value to be more lenient (disconnect fewer clients).
    /// A non-positive value will disable the limit.
    /// In practice, the default size is 4 * the maximum number of tracked streams per connection,
    /// which is double the maximum number of concurrent streams per connection, which is 100.
    /// That is, the default value is 800, unless <see cref="Http2Limits.MaxStreamsPerConnection"/> is modified.
    /// Choosing a value lower than the maximum number of tracked streams doesn't make sense,
    /// so such values will be adjusted upward.
    /// TODO (https://github.com/dotnet/aspnetcore/issues/51309): eliminate this limit.
    private const string MaximumFlowControlQueueSizeProperty = "Microsoft.AspNetCore.Server.Kestrel.Http2.MaxConnectionFlowControlQueueSize";

    private const int HeaderBufferSizeMultiplier = 2;

    private static readonly int? AppContextMaximumFlowControlQueueSize = GetAppContextMaximumFlowControlQueueSize();

    private static int? GetAppContextMaximumFlowControlQueueSize()
    {
        var data = AppContext.GetData(MaximumFlowControlQueueSizeProperty);

        // Programmatically-configured values are usually ints
        if (data is int count)
        {
            return count;
        }

        // msbuild-configured values are usually strings
        if (data is string countStr && int.TryParse(countStr, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private readonly int _maximumFlowControlQueueSize;

    private bool IsFlowControlQueueLimitEnabled => _maximumFlowControlQueueSize > 0;

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

    private int _maxFrameSize = Http2PeerSettings.MinAllowedMaxFrameSize;
    private byte[] _headerEncodingBuffer;

    // Keep track of the high-water mark of _headerEncodingBuffer's size so we don't have to grow
    // through intermediate sizes repeatedly.
    private int _headersEncodingLargeBufferSize = Http2PeerSettings.MinAllowedMaxFrameSize * HeaderBufferSizeMultiplier;
    private long _unflushedBytes;

    private bool _completed;
    private bool _aborted;

    private readonly object _windowUpdateLock = new();
    private long _connectionWindow;
    private readonly Queue<Http2OutputProducer> _waitingForMoreConnectionWindow = new();
    // This is the stream that consumed the last set of connection window
    private Http2OutputProducer? _lastWindowConsumer;
    private readonly Task _writeQueueTask;

    public Http2FrameWriter(
        PipeWriter outputPipeWriter,
        BaseConnectionContext connectionContext,
        Http2Connection http2Connection,
        int maxStreamsPerConnection,
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

        _maximumFlowControlQueueSize = AppContextMaximumFlowControlQueueSize is null
            ? 4 * maxStreamsPerConnection // 4 is a magic number to give us some padding above the expected maximum size
            : (int)AppContextMaximumFlowControlQueueSize;

        if (IsFlowControlQueueLimitEnabled && _maximumFlowControlQueueSize < maxStreamsPerConnection)
        {
            _log.Http2FlowControlQueueMaximumTooLow(_connectionContext.ConnectionId, maxStreamsPerConnection, _maximumFlowControlQueueSize);
            _maximumFlowControlQueueSize = maxStreamsPerConnection;
        }

        // This is bounded by the maximum number of concurrent Http2Streams per Http2Connection.
        // This isn't the same as SETTINGS_MAX_CONCURRENT_STREAMS, but typically double (with a floor of 100)
        // which is the max number of Http2Streams that can end up in the Http2Connection._streams dictionary.
        //
        // Setting a lower limit of SETTINGS_MAX_CONCURRENT_STREAMS might be sufficient because a stream shouldn't
        // be rescheduling itself after being completed or canceled, but we're going with the more conservative limit
        // in case there's some logic scheduling completed or canceled streams unnecessarily.
        _channel = Channel.CreateBounded<Http2OutputProducer>(new BoundedChannelOptions(maxStreamsPerConnection)
        {
            AllowSynchronousContinuations = _scheduleInline,
            SingleReader = true
        });

        _connectionWindow = Http2PeerSettings.DefaultInitialWindowSize;

        _writeQueueTask = Task.Run(WriteToOutputPipe);
    }

    public void Schedule(Http2OutputProducer producer)
    {
        if (!_channel.Writer.TryWrite(producer))
        {
            // This can happen if a client resets streams faster than we can clear them out - we end up with a backlog
            // exceeding the channel size.  Disconnecting seems appropriate in this case.
            var ex = new ConnectionAbortedException("HTTP/2 connection exceeded the output operations maximum queue size.");
            _log.Http2QueueOperationsExceeded(_connectionId, ex);
            _http2Connection.Abort(ex, Http2ErrorCode.INTERNAL_ERROR, ConnectionEndReason.OutputQueueSizeExceeded);
        }
    }

    private async Task WriteToOutputPipe()
    {
        while (await _channel.Reader.WaitToReadAsync())
        {
            // We need to handle the case where aborts can be scheduled while this loop is running and might be on the way to complete
            // the reader.
            while (_channel.Reader.TryRead(out var producer) && !producer.CompletedResponse)
            {
                try
                {
                    var reader = producer.PipeReader;
                    var stream = producer.Stream;

                    // We don't need to check the result because it's either
                    // - true because we have a result
                    // - false because we're flushing headers
                    reader.TryRead(out var readResult);
                    var buffer = readResult.Buffer;

                    // Check the stream window
                    var actual = producer.CheckStreamWindow(buffer.Length);

                    // Now check the connection window
                    actual = CheckConnectionWindow(actual);

                    // Write what we can
                    if (actual < buffer.Length)
                    {
                        buffer = buffer.Slice(0, actual);
                    }

                    // Consume the actual bytes resolved after checking both connection and stream windows
                    producer.ConsumeStreamWindow(actual);
                    ConsumeConnectionWindow(actual);

                    // Stash the unobserved state, we're going to mark this snapshot as observed
                    var observed = producer.UnobservedState;
                    var currentState = producer.CurrentState;

                    // Avoid boxing the enum (though the JIT optimizes this eventually)
                    static bool HasStateFlag(Http2OutputProducer.State state, Http2OutputProducer.State flags)
                        => (state & flags) == flags;

                    // Check if we need to write headers
                    var flushHeaders = HasStateFlag(observed, Http2OutputProducer.State.FlushHeaders) && !HasStateFlag(currentState, Http2OutputProducer.State.FlushHeaders);

                    (var hasMoreData, var reschedule, currentState, var waitingForWindowUpdates) = producer.ObserveDataAndState(buffer.Length, observed);

                    var aborted = HasStateFlag(currentState, Http2OutputProducer.State.Aborted);
                    var completed = HasStateFlag(currentState, Http2OutputProducer.State.Completed) && !hasMoreData;

                    FlushResult flushResult = default;

                    // We're not complete but we got the abort.
                    if (aborted && !completed)
                    {
                        // Response body is aborted, complete reader for this output producer.
                        if (flushHeaders)
                        {
                            // write headers
                            WriteResponseHeaders(stream.StreamId, stream.StatusCode, Http2HeadersFrameFlags.NONE, (HttpResponseHeaders)stream.ResponseHeaders);
                        }

                        if (actual > 0)
                        {
                            // actual > int.MaxValue should never be possible because it would exceed Http2PeerSettings.MaxWindowSize
                            // which is a protocol-defined limit. There's no way Kestrel would try to write more than that in one go.
                            Debug.Assert(actual <= int.MaxValue);

                            // If we got here it means we're going to cancel the write. Restore any consumed bytes to the connection window.
                            if (!TryUpdateConnectionWindow((int)actual))
                            {
                                // This branch can only ever be taken given both a buggy client and aborting streams mid-write. Even then, we're much more likely catch the
                                // error in Http2Connection.ProcessFrameAsync() before catching it here. This branch is technically possible though, so we defend against it.
                                await HandleFlowControlErrorAsync();
                                return;
                            }
                        }
                    }
                    else if (completed && stream.ResponseTrailers is { Count: > 0 })
                    {
                        // Output is ending and there are trailers to write
                        // Write any remaining content then write trailers and there's no
                        // flow control back pressure being applied (hasMoreData)

                        stream.ResponseTrailers.SetReadOnly();
                        stream.DecrementActiveClientStreamCount();

                        // It is faster to write data and trailers together. Locking once reduces lock contention.
                        flushResult = await WriteDataAndTrailersAsync(stream, buffer, flushHeaders, stream.ResponseTrailers);
                    }
                    else if (completed && producer.AppCompletedWithNoResponseBodyOrTrailers)
                    {
                        Debug.Assert(flushHeaders, "The app completed successfully without flushing headers!");

                        if (buffer.Length != 0)
                        {
                            _log.Http2UnexpectedDataRemaining(stream.StreamId, _connectionId);
                        }
                        else
                        {
                            stream.DecrementActiveClientStreamCount();

                            flushResult = await FlushEndOfStreamHeadersAsync(stream);
                        }
                    }
                    else
                    {
                        var endStream = completed;

                        if (endStream)
                        {
                            stream.DecrementActiveClientStreamCount();
                        }

                        flushResult = await WriteDataAsync(stream, buffer, buffer.Length, endStream, flushHeaders);
                    }

                    if (producer.IsTimingWrite)
                    {
                        _timeoutControl.StopTimingWrite();
                    }

                    reader.AdvanceTo(buffer.End);

                    if (completed || aborted)
                    {
                        await reader.CompleteAsync();

                        await producer.CompleteResponseAsync();
                    }
                    // We're not going to schedule this again if there's no remaining window.
                    // When the window update is sent, the producer will be re-queued if needed.
                    else if (hasMoreData && !aborted && !waitingForWindowUpdates)
                    {
                        // If we queued the connection for a window update or we were unable to schedule the next write
                        // then we're waiting for a window update to resume the scheduling.
                        if (TryQueueProducerForConnectionWindowUpdate(actual, producer) ||
                            !producer.TryScheduleNextWriteIfStreamWindowHasSpace())
                        {
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
                        producer.Schedule();
                    }
                }
                catch (Exception ex)
                {
                    _log.Http2UnexpectedConnectionQueueError(_connectionId, ex);
                }
            }
        }

        _log.Http2ConnectionQueueProcessingCompleted(_connectionId);
    }

    private async Task HandleFlowControlErrorAsync()
    {
        const ConnectionEndReason reason = ConnectionEndReason.InvalidWindowUpdateSize;
        const Http2ErrorCode http2ErrorCode = Http2ErrorCode.FLOW_CONTROL_ERROR;

        var connectionError = new Http2ConnectionErrorException(CoreStrings.Http2ErrorWindowUpdateSizeInvalid, http2ErrorCode, reason);
        _log.Http2ConnectionError(_connectionId, connectionError);
        await WriteGoAwayAsync(int.MaxValue, http2ErrorCode);

        // Prevent Abort() from writing an INTERNAL_ERROR GOAWAY frame after our FLOW_CONTROL_ERROR.
        Complete();
        // Stop processing any more requests and immediately close the connection.
        _http2Connection.Abort(new ConnectionAbortedException(CoreStrings.Http2ErrorWindowUpdateSizeInvalid, connectionError), http2ErrorCode, reason);
    }

    private bool TryQueueProducerForConnectionWindowUpdate(long actual, Http2OutputProducer producer)
    {
        lock (_windowUpdateLock)
        {
            // Check the connection window under a lock so that we don't miss window updates
            if (_connectionWindow == 0)
            {
                // We have no more connection window, put this producer in a queue waiting for
                // a window update to resume the producer.

                // In order to make scheduling more fair we want to make sure that streams that have data get a chance to run in a round robin manner.
                // To do this we will store the producer that consumed the window in a field and put it to the back of the queue.

                producer.SetWaitingForWindowUpdates();

                if (actual != 0 && _lastWindowConsumer is null)
                {
                    _lastWindowConsumer = producer;
                }
                else
                {
                    EnqueueWaitingForMoreConnectionWindow(producer);
                }

                return true;
            }
        }

        return false;
    }

    public void UpdateMaxHeaderTableSize(uint maxHeaderTableSize)
    {
        lock (_writeLock)
        {
            _hpackEncoder.UpdateMaxHeaderTableSize(maxHeaderTableSize);
        }
    }

    public void UpdateMaxFrameSize(int maxFrameSize)
    {
        lock (_writeLock)
        {
            if (_maxFrameSize != maxFrameSize)
            {
                // Safe multiply, MaxFrameSize is limited to 2^24-1 bytes by the protocol and by Http2PeerSettings.
                // Ref: https://datatracker.ietf.org/doc/html/rfc7540#section-4.2
                _headersEncodingLargeBufferSize = int.Max(_headersEncodingLargeBufferSize, maxFrameSize * HeaderBufferSizeMultiplier);
                _maxFrameSize = maxFrameSize;
                _headerEncodingBuffer = new byte[_maxFrameSize];
            }
        }
    }

    /// <summary>
    /// Call while in the <see cref="_writeLock"/>.
    /// </summary>
    /// <returns><c>true</c> if already completed.</returns>
    private bool CompleteUnsynchronized()
    {
        if (_completed)
        {
            return true;
        }

        _completed = true;
        _outputWriter.Abort();

        return false;
    }

    public void Complete()
    {
        lock (_writeLock)
        {
            if (CompleteUnsynchronized())
            {
                return;
            }
        }

        // Call outside of _writeLock as this can call Http2OutputProducer.Stop which can acquire Http2OutputProducer._dataWriterLock
        // which is not the desired lock order
        AbortConnectionFlowControl();
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

            if (CompleteUnsynchronized())
            {
                return;
            }
        }

        // Call outside of _writeLock as this can call Http2OutputProducer.Stop which can acquire Http2OutputProducer._dataWriterLock
        // which is not the desired lock order
        AbortConnectionFlowControl();
    }

    private ValueTask<FlushResult> FlushEndOfStreamHeadersAsync(Http2Stream stream)
    {
        lock (_writeLock)
        {
            if (_completed)
            {
                return default;
            }

            WriteResponseHeadersUnsynchronized(stream.StreamId, stream.StatusCode, Http2HeadersFrameFlags.END_STREAM, (HttpResponseHeaders)stream.ResponseHeaders);

            var bytesWritten = _unflushedBytes;
            _unflushedBytes = 0;

            return _flusher.FlushAsync(_minResponseDataRate, bytesWritten);
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
            // In the case of the headers, there is always a status header to be returned, so BeginEncodeHeaders will not return BufferTooSmall.
            _headersEnumerator.Initialize(headers);
            _outgoingFrame.PrepareHeaders(headerFrameFlags, streamId);
            var writeResult = HPackHeaderWriter.BeginEncodeHeaders(statusCode, _hpackEncoder, _headersEnumerator, _headerEncodingBuffer, out var payloadLength);
            Debug.Assert(writeResult != HeaderWriteResult.BufferTooSmall, "This always writes the status as the first header, and it should never be an over the buffer size.");
            FinishWritingHeadersUnsynchronized(streamId, payloadLength, writeResult);
        }
        // Any exception from the HPack encoder can leave the dynamic table in a corrupt state.
        // Since we allow custom header encoders we don't know what type of exceptions to expect.
        catch (Exception ex)
        {
            _log.HPackEncodingError(_connectionId, streamId, ex);
            _http2Connection.Abort(new ConnectionAbortedException(ex.Message, ex), Http2ErrorCode.INTERNAL_ERROR, ConnectionEndReason.ErrorWritingHeaders);
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
                // In the case of the trailers, there is no status header to be written, so even the first call to BeginEncodeHeaders can return BufferTooSmall.
                _outgoingFrame.PrepareHeaders(Http2HeadersFrameFlags.END_STREAM, streamId);
                _headersEnumerator.Initialize(headers);
                var writeResult = HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _headersEnumerator, _headerEncodingBuffer, out var payloadLength);
                FinishWritingHeadersUnsynchronized(streamId, payloadLength, writeResult);
            }
            // Any exception from the HPack encoder can leave the dynamic table in a corrupt state.
            // Since we allow custom header encoders we don't know what type of exceptions to expect.
            catch (Exception ex)
            {
                _log.HPackEncodingError(_connectionId, streamId, ex);
                _http2Connection.Abort(new ConnectionAbortedException(ex.Message, ex), Http2ErrorCode.INTERNAL_ERROR, ConnectionEndReason.ErrorWritingHeaders);
            }

            return TimeFlushUnsynchronizedAsync();
        }
    }

    private void SplitHeaderAcrossFrames(int streamId, ReadOnlySpan<byte> dataToFrame, bool endOfHeaders, bool isFramePrepared)
    {
        var shouldPrepareFrame = !isFramePrepared;
        while (dataToFrame.Length > 0)
        {
            if (shouldPrepareFrame)
            {
                _outgoingFrame.PrepareContinuation(Http2ContinuationFrameFlags.NONE, streamId);
            }

            // Should prepare continuation frames.
            shouldPrepareFrame = true;
            var currentSize = Math.Min(dataToFrame.Length, _maxFrameSize);
            _outgoingFrame.PayloadLength = currentSize;
            if (endOfHeaders && dataToFrame.Length == currentSize)
            {
                _outgoingFrame.HeadersFlags |= Http2HeadersFrameFlags.END_HEADERS;
            }

            WriteHeaderUnsynchronized();
            _outputWriter.Write(dataToFrame[..currentSize]);
            dataToFrame = dataToFrame.Slice(currentSize);
        }
    }

    private void FinishWritingHeadersUnsynchronized(int streamId, int payloadLength, HeaderWriteResult writeResult)
    {
        Debug.Assert(payloadLength <= _maxFrameSize, "The initial payload lengths is written to _headerEncodingBuffer with size of _maxFrameSize");
        byte[]? largeHeaderBuffer = null;
        Span<byte> buffer;
        if (writeResult == HeaderWriteResult.Done)
        {
            // Fast path, only a single HEADER frame.
            _outgoingFrame.PayloadLength = payloadLength;
            _outgoingFrame.HeadersFlags |= Http2HeadersFrameFlags.END_HEADERS;
            WriteHeaderUnsynchronized();
            _outputWriter.Write(_headerEncodingBuffer.AsSpan(0, payloadLength));
            return;
        }
        else if (writeResult == HeaderWriteResult.MoreHeaders)
        {
            _outgoingFrame.PayloadLength = payloadLength;
            WriteHeaderUnsynchronized();
            _outputWriter.Write(_headerEncodingBuffer.AsSpan(0, payloadLength));
        }
        else
        {
            // This may happen in case of the TRAILERS after the initial encode operation.
            // The _maxFrameSize sized _headerEncodingBuffer was too small.
            while (writeResult == HeaderWriteResult.BufferTooSmall)
            {
                Debug.Assert(payloadLength == 0, "Payload written even though buffer is too small");
                largeHeaderBuffer = ArrayPool<byte>.Shared.Rent(_headersEncodingLargeBufferSize);
                buffer = largeHeaderBuffer.AsSpan(0, _headersEncodingLargeBufferSize);
                writeResult = HPackHeaderWriter.RetryBeginEncodeHeaders(_hpackEncoder, _headersEnumerator, buffer, out payloadLength);
                if (writeResult != HeaderWriteResult.BufferTooSmall)
                {
                    SplitHeaderAcrossFrames(streamId, buffer[..payloadLength], endOfHeaders: writeResult == HeaderWriteResult.Done, isFramePrepared: true);
                }
                else
                {
                    _headersEncodingLargeBufferSize = checked(_headersEncodingLargeBufferSize * HeaderBufferSizeMultiplier);
                }
                ArrayPool<byte>.Shared.Return(largeHeaderBuffer);
                largeHeaderBuffer = null;
            }
            if (writeResult == HeaderWriteResult.Done)
            {
                return;
            }
        }

        // HEADERS and zero or more CONTINUATIONS sent - all subsequent frames are (unprepared) CONTINUATIONs
        buffer = _headerEncodingBuffer;
        while (writeResult != HeaderWriteResult.Done)
        {
            writeResult = HPackHeaderWriter.ContinueEncodeHeaders(_hpackEncoder, _headersEnumerator, buffer, out payloadLength);
            if (writeResult == HeaderWriteResult.BufferTooSmall)
            {
                if (largeHeaderBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(largeHeaderBuffer);
                    _headersEncodingLargeBufferSize = checked(_headersEncodingLargeBufferSize * HeaderBufferSizeMultiplier);
                }
                largeHeaderBuffer = ArrayPool<byte>.Shared.Rent(_headersEncodingLargeBufferSize);
                buffer = largeHeaderBuffer.AsSpan(0, _headersEncodingLargeBufferSize);
            }
            else
            {
                // In case of Done or MoreHeaders: write to output.
                SplitHeaderAcrossFrames(streamId, buffer[..payloadLength], endOfHeaders: writeResult == HeaderWriteResult.Done, isFramePrepared: false);
            }
        }
        if (largeHeaderBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(largeHeaderBuffer);
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

                currentData.CopyTo(_outputWriter);

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

            remainingData.CopyTo(_outputWriter);

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

    private long CheckConnectionWindow(long bytes)
    {
        lock (_windowUpdateLock)
        {
            return Math.Min(bytes, _connectionWindow);
        }
    }

    private void ConsumeConnectionWindow(long bytes)
    {
        lock (_windowUpdateLock)
        {
            _connectionWindow -= bytes;
        }
    }

    /// <summary>
    /// Do not call this method under the _writeLock.
    /// This method can call Http2OutputProducer.Stop which can acquire Http2OutputProducer._dataWriterLock
    /// which is not the desired lock order
    /// </summary>
    private void AbortConnectionFlowControl()
    {
        lock (_windowUpdateLock)
        {
            if (_lastWindowConsumer is { } producer)
            {
                _lastWindowConsumer = null;

                // Put the consumer of the connection window last
                EnqueueWaitingForMoreConnectionWindow(producer);
            }

            while (_waitingForMoreConnectionWindow.TryDequeue(out producer))
            {
                // Abort the stream
                producer.Stop();
            }
        }
    }

    public bool TryUpdateConnectionWindow(int bytes)
    {
        lock (_windowUpdateLock)
        {
            var maxUpdate = Http2PeerSettings.MaxWindowSize - _connectionWindow;

            if (bytes > maxUpdate)
            {
                return false;
            }

            _connectionWindow += bytes;

            if (_lastWindowConsumer is { } producer)
            {
                _lastWindowConsumer = null;

                // Put the consumer of the connection window last
                EnqueueWaitingForMoreConnectionWindow(producer);
            }

            while (_waitingForMoreConnectionWindow.TryDequeue(out producer))
            {
                producer.ScheduleResumeFromWindowUpdate();
            }
        }
        return true;
    }

    private void EnqueueWaitingForMoreConnectionWindow(Http2OutputProducer producer)
    {
        _waitingForMoreConnectionWindow.Enqueue(producer);
        // This is re-entrant because abort will cause a final enqueue.
        // Easier to check for that condition than to make each enqueuer reason about what to call.
        if (!_aborted && IsFlowControlQueueLimitEnabled && _waitingForMoreConnectionWindow.Count > _maximumFlowControlQueueSize)
        {
            _log.Http2FlowControlQueueOperationsExceeded(_connectionId, _maximumFlowControlQueueSize);
            _http2Connection.Abort(new ConnectionAbortedException("HTTP/2 connection exceeded the outgoing flow control maximum queue size."), Http2ErrorCode.INTERNAL_ERROR, ConnectionEndReason.FlowControlQueueSizeExceeded);
        }
    }
}
