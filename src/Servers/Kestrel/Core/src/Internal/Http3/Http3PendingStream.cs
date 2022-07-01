// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class Http3PendingStream
{
    private readonly CancellationTokenSource abortedToken = new();
    private ConnectionAbortedException? _abortedException;

    internal readonly Http3StreamContext Context;
    internal readonly bool IsRequestStream;
    internal readonly long StreamId;
    internal long StreamTimeoutTicks;

    public Http3PendingStream(Http3StreamContext context, long id, bool isRequestStream)
    {
        Context = context;
        StreamTimeoutTicks = 0;
        IsRequestStream = isRequestStream;
        StreamId = id;
    }

    public void Abort(ConnectionAbortedException exception)
    {
        _abortedException = exception;

        abortedToken.Cancel();
    }

    public async Task<long> ReadNextStreamHeader(Http3StreamContext context, long streamId, bool persist = false)
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

                    // if it is a webtransport stream we throw away the headers so we can 
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
            }
        }
        catch (Exception)
        {
            var exception = new Exception("Attempted to read header on aborted stream", _abortedException);
            exception.Data.Add("StreamId", streamId);
            throw exception;
        }

        return 0;
    }
}
