// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal enum Http3ErrorCode : uint
    {
        /*
         * https://quicwg.org/base-drafts/draft-ietf-quic-http.html#http-error-codes
            HTTP_NO_ERROR (0x100):
            No error. This is used when the connection or stream needs to be closed, but there is no error to signal.
            HTTP_GENERAL_PROTOCOL_ERROR (0x101):
            Peer violated protocol requirements in a way which doesn’t match a more specific error code, or endpoint declines to use the more specific error code.
            HTTP_INTERNAL_ERROR (0x102):
            An internal error has occurred in the HTTP stack.
            HTTP_STREAM_CREATION_ERROR (0x103):
            The endpoint detected that its peer created a stream that it will not accept.
            HTTP_CLOSED_CRITICAL_STREAM (0x104):
            A stream required by the connection was closed or reset.
            HTTP_UNEXPECTED_FRAME (0x105):
            A frame was received which was not permitted in the current state.
            HTTP_FRAME_ERROR (0x106):
            A frame that fails to satisfy layout requirements or with an invalid size was received.
            HTTP_EXCESSIVE_LOAD (0x107):
            The endpoint detected that its peer is exhibiting a behavior that might be generating excessive load.
            HTTP_WRONG_STREAM (0x108):
            A frame was received on a stream where it is not permitted.
            HTTP_ID_ERROR (0x109):
            A Stream ID, Push ID, or Placeholder ID was used incorrectly, such as exceeding a limit, reducing a limit, or being reused.
            HTTP_SETTINGS_ERROR (0x10A):
            An endpoint detected an error in the payload of a SETTINGS frame: a duplicate setting was detected, a client-only setting was sent by a server, or a server-only setting by a client.
            HTTP_MISSING_SETTINGS (0x10B):
            No SETTINGS frame was received at the beginning of the control stream.
            HTTP_REQUEST_REJECTED (0x10C):
            A server rejected a request without performing any application processing.
            HTTP_REQUEST_CANCELLED (0x10D):
            The request or its response (including pushed response) is cancelled.
            HTTP_REQUEST_INCOMPLETE (0x10E):
            The client’s stream terminated without containing a fully-formed request.
            HTTP_EARLY_RESPONSE (0x10F):
            The remainder of the client’s request is not needed to produce a response. For use in STOP_SENDING only.
            HTTP_CONNECT_ERROR (0x110):
            The connection established in response to a CONNECT request was reset or abnormally closed.
            HTTP_VERSION_FALLBACK (0x111):
            The requested operation cannot be served over HTTP/3. The peer should retry over HTTP/1.1.
         */
        NO_ERROR = 0x100,
        PROTOCOL_ERROR = 0x101,
        INTERNAL_ERROR = 0x102,
        STREAM_CREATION_ERROR = 0x103,
        CLOSED_CRITICAL_STREAM = 0x104,
        UNEXPECTED_FRAME = 0x105,
        FRAME_ERROR = 0x106,
        EXCESSIVE_LOAD = 0x107,
        WRONG_STREAM = 0x108,
        ID_ERROR = 0x109,
        SETTINGS_ERROR = 0x10a,
        MISSING_SETTINGS = 0x10b,
        REQUEST_REJECTED = 0x10c,
        REQUEST_CANCELLED = 0x10d,
        REQUEST_INCOMPLETE = 0x10e,
        EARLY_RESPONSE = 0x10f,
        CONNECT_ERROR = 0x110,
        VERSION_FALLBACK = 0x111,
    }
}
