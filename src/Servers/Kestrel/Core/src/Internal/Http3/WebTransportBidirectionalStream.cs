// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportBidirectionalStream<TContext> : Http3BidirectionalStream<TContext> where TContext : notnull
{
    private bool _inputOpen = true;
    private bool _outputOpen = true;

    public bool IsInputOpen => _inputOpen;
    public bool IsOutputOpen => _outputOpen;

    public WebTransportBidirectionalStream(IHttpApplication<TContext> application, Http3StreamContext context) : base(application, context)
    {
        context.StreamContext.ConnectionClosed.Register(state =>
        {
            var stream = (WebTransportBidirectionalStream<TContext>)state!;
            stream._context.WebTransportSession.TryRemoveStream(stream.StreamId);
        }, this);
    }

    // Override the existing bidirectional stream aborting to handle closing only 1 direction of the stream
    protected override void AbortCore(Exception exception, Http3ErrorCode errorCode)
    {
        // already aborted
        if (_inputOpen && _outputOpen)
        {
            return;
        }

        // deal with the error if possible
        switch (errorCode)
        {
            case Http3ErrorCode.RequestIncomplete: // JS API ref: stream.writable.close()
                _outputOpen = false;
                _context.Transport.Output.Complete();
                break;
            case Http3ErrorCode.RequestRejected: // JS API ref: stream.readable.cancel()
                _inputOpen = false;
                _context.Transport.Input.Complete();
                break;
            default:
                _inputOpen = _outputOpen = false;
                base.AbortCore(exception, Http3ErrorCode.NoError);
                break;
        }

        if (!_inputOpen && !_outputOpen)
        {
            // closes everythign gracefully by swallowing the handled errors
            base.AbortCore(exception, Http3ErrorCode.NoError);
        }
    }

    // TODO TEST that the readable one actually works. Maybe I don't need to handle that case?

    // TODO TEST that completed pipes can't be written or read from.
}
