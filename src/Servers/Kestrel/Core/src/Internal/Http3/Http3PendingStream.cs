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

    public async ValueTask<(long, SequencePosition, SequencePosition)> ReadNextStreamHeaderAsync(Http3StreamContext context, long streamId)
    {
        SequencePosition start = default;
        SequencePosition? end = null;
        var value = 0L;
        try
        {
            var Input = context.Transport.Input;
            var result = await Input.ReadAsync(abortedToken.Token);
            var readableBuffer = result.Buffer;
            start = readableBuffer.Start;
            if (!readableBuffer.IsEmpty)
            {
                value = VariableLengthIntegerHelper.GetInteger(readableBuffer, out var consumed, out var examined);
                end = consumed;
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
            StreamTimeoutTicks = default;
        }
        return (value, start, end ?? start);
    }
}
