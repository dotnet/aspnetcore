// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3PendingStream
{
    private readonly CancellationTokenSource abortedToken = new();

    private ConnectionAbortedException? _abortedException;
    private bool _isClosed;

    internal readonly Http3StreamContext Context;
    internal readonly long StreamId;
    internal long StreamTimeoutTicks;

    public Http3PendingStream(Http3StreamContext context, long id)
    {
        Context = context;
        StreamTimeoutTicks = default;
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

        abortedToken.Cancel();

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
                var result = await Input.ReadAsync(abortedToken.Token);
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
            var exception = new Exception(CoreStrings.AttemptedToReadHeaderOnAbortedStream, _abortedException);
            exception.Data.Add("StreamId", streamId);
            throw exception;
        }
        finally
        {

            if (advance)
            {
                Input.AdvanceTo(consumed);
            }
            else
            {
                Input.AdvanceTo(start);
            }

            StreamTimeoutTicks = default;
        }

        return -1L;
    }
}
