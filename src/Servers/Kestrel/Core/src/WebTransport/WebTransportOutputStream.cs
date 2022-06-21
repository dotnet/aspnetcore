// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

/// <summary>
/// Represents an inbound unidirectional stream for the WebTransport protocol.
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
public class WebTransportOutputStream : WebTransportBaseStream
{
    private readonly Http3FrameWriter _frameWriter;
    private Pipe RequestBodyPipe { get; set; } = default!;
    private int _isClosed;
    private readonly object _dataWriterLock = new();
    //private readonly ValueTask<FlushResult> _dataWriteProcessingTask;
    private readonly TimingPipeFlusher _flusher;

    internal WebTransportOutputStream(Http3StreamContext context) : base(context)
    {
        _frameWriter = new Http3FrameWriter(
                context.StreamContext,
                context.TimeoutControl,
                context.ServiceContext.ServerOptions.Limits.MinResponseDataRate,
                context.MemoryPool,
                context.ServiceContext.Log,
                _streamIdFeature,
                context.ClientPeerSettings,
                this);

        RequestBodyPipe = CreateRequestBodyPipe(64 * 1024);

        _flusher = new TimingPipeFlusher(timeoutControl: null, Log);
        _flusher.Initialize(RequestBodyPipe.Writer);

        //_dataWriteProcessingTask = ProcessDataWrites().Preserve();
    }

    internal override void AbortCore(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        _isClosed = 1;

        base.AbortCore(abortReason, errorCode);

        RequestBodyPipe.Writer.Complete();
        lock (_dataWriterLock)
        {
            RequestBodyPipe.Writer.Complete(new OperationCanceledException());
        }
        _frameWriter.Abort(abortReason);
    }

    // TODO I don't think I need this. This just empties the pipe by chunking the data and flushing when necessary.
    // However, as I am sending and flushing directly, this can be removed right?
    //private async ValueTask<FlushResult> ProcessDataWrites()
    //{
    //    FlushResult flushResult = default;
    //    try
    //    {
    //        ReadResult readResult;

    //        do
    //        {
    //            readResult = await RequestBodyPipe.Reader.ReadAsync();

    //            if (readResult.IsCompleted)
    //            {
    //                // Output is ending and there are trailers to write
    //                // Write any remaining content then write trailers
    //                if (readResult.Buffer.Length > 0)
    //                {
    //                    flushResult = await _frameWriter.WriteDataAsync(readResult.Buffer);
    //                }
    //            }
    //            else if (readResult.IsCompleted)
    //            {
    //                if (readResult.Buffer.Length != 0)
    //                {
    //                    ThrowUnexpectedState();
    //                }

    //                // Headers have already been written and there is no other content to write

    //                // Need to complete framewriter immediately as CompleteAsync could be called
    //                // in the app delegate and we don't want to wait for the app delegate to
    //                // finish before sending response.
    //                await _frameWriter.CompleteAsync();
    //                flushResult = default;
    //            }
    //            else
    //            {
    //                flushResult = await _frameWriter.WriteDataAsync(readResult.Buffer);
    //            }

    //            RequestBodyPipe.Reader.AdvanceTo(readResult.Buffer.End);
    //        } while (!readResult.IsCompleted);
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        // Writes should not throw for aborted streams/connections.
    //    }
    //    catch (Exception ex)
    //    {
    //        Log.LogCritical(ex, nameof(Http3OutputProducer) + "." + nameof(ProcessDataWrites) + " observed an unexpected exception.");
    //    }

    //    await RequestBodyPipe.Reader.CompleteAsync();

    //    return flushResult;

    //    static void ThrowUnexpectedState()
    //    {
    //        throw new InvalidOperationException(nameof(Http3OutputProducer) + "." + nameof(ProcessDataWrites) + " observed an unexpected state where the streams output ended with data still remaining in the pipe.");
    //    }
    //}

    /// <summary>
    /// Writes data to the stream and flushes it.
    /// </summary>
    /// <param name="data">The data to write to the stream</param>
    /// <param name="cancellationToken">The cancellation token to abort the operation</param>
    /// <exception cref="ConnectionAbortedException">TODO find a more applicable one. Throws is the stream has already been closed</exception>
    /// <returns>A flush result of the operation</returns>
    public ValueTask<FlushResult> WriteDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (_isClosed == 1)
        {
            throw new ConnectionAbortedException("Attempting to send data over a closed stream");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<FlushResult>(Task.FromCanceled<FlushResult>(cancellationToken));
        }

        lock (_dataWriterLock)
        {
            // This length check is important because we don't want to set _startedWritingDataFrames unless a data
            // frame will actually be written causing the headers to be flushed.
            if (_isClosed == 1 || data.Length == 0)
            {
                return new ValueTask<FlushResult>(new FlushResult(false, true));
            }

            //RequestBodyPipe.Writer.Write(data.Span);
            return _frameWriter.WriteDataAsync(new(data));
            //return _flusher.FlushAsync();//FlushAsync(this, cancellationToken); todo do I still need to flush?
        }
    }
}
