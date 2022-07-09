// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal class Http3PendingStream
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

    public async Task<long> ReadNextStreamHeaderAsync(Http3StreamContext context, long streamId, bool persist = false)
    {
        try
        {
            var Input = context.Transport.Input;
            var result = await Input.ReadAsync(abortedToken.Token);
            var readableBuffer = result.Buffer;
            var value = 0L;
            try
            {
                if (!readableBuffer.IsEmpty)
                {
                    value = VariableLengthIntegerHelper.GetInteger(readableBuffer, out var consumed, out var examined);

                    // If it is a WebTransport stream we throw away the headers so we can
                    // then pass the pipe reader and writer to the application without them.
                    if (persist || value == (long)Http3StreamType.WebTransportBidirectional || value == (long)Http3StreamType.WebTransportUnidirectional)
                    {
                        Input.AdvanceTo(consumed, examined);
                    }

                    return value;
                }
            }
            finally
            {
                if (!persist && (value != (long)Http3StreamType.WebTransportBidirectional && value != (long)Http3StreamType.WebTransportUnidirectional))
                {
                    Input.AdvanceTo(readableBuffer.Start);
                }
                StreamTimeoutTicks = default;
            }
        }
        catch (Exception)
        {
            var exception = new Exception(CoreStrings.AttemptedToReadHeaderOnAbortedStream, _abortedException);
            exception.Data.Add("StreamId", streamId);
            throw exception;
        }

        return 0;
    }
}
