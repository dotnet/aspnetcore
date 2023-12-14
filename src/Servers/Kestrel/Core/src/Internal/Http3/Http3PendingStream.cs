// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3PendingStream
{
    private ConnectionAbortedException? _abortedException;
    private bool _isClosed;

    internal readonly Http3StreamContext Context;
    internal readonly long StreamId;
    internal long StreamTimeoutTimestamp;

    public Http3PendingStream(Http3StreamContext context, long id)
    {
        Context = context;
        StreamTimeoutTimestamp = default;
        StreamId = id;
    }

    public void Abort(ConnectionAbortedException exception)
    {
        if (_isClosed)
        {
            return;
        }
        _isClosed = true;

        _abortedException = exception;

        Context.Transport.Input.CancelPendingRead();
        Context.Transport.Input.Complete(exception);
        Context.Transport.Output.Complete(exception);
    }

    public async ValueTask<long> ReadNextStreamHeaderAsync(Http3StreamContext context, long streamId, Http3StreamType? advanceOn)
    {
        var Input = context.Transport.Input;
        var advance = false;
        SequencePosition consumed = default;
        SequencePosition start = default;
        try
        {
            while (!_isClosed)
            {
                var result = await Input.ReadAsync();

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("The read operation was canceled.");
                }

                var readableBuffer = result.Buffer;
                consumed = readableBuffer.Start;
                start = readableBuffer.Start;

                if (!readableBuffer.IsEmpty)
                {
                    var value = VariableLengthIntegerHelper.GetInteger(readableBuffer, out consumed, out _);
                    if (value != -1)
                    {
                        if (!advanceOn.HasValue || value == (long)advanceOn)
                        {
                            advance = true;
                        }
                        return value;
                    }
                }

                if (result.IsCompleted)
                {
                    return -1L;
                }
            }
        }
        catch (Exception)
        {
            throw new Http3PendingStreamException(CoreStrings.AttemptedToReadHeaderOnAbortedStream, streamId, _abortedException);
        }
        finally
        {
            if (!_isClosed)
            {
                if (advance)
                {
                    Input.AdvanceTo(consumed);
                }
                else
                {
                    Input.AdvanceTo(start);
                }
            }

            StreamTimeoutTimestamp = default;
        }

        return -1L;
    }
}
