// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal enum Http3ErrorCode : long
    {
        /// <summary>
        /// H3_NO_ERROR (0x100):
        /// No error. This is used when the connection or stream needs to be closed, but there is no error to signal.
        /// </summary>
        NoError = 0x100,
        /// <summary>
        /// H3_GENERAL_PROTOCOL_ERROR (0x101):
        /// Peer violated protocol requirements in a way which doesn't match a more specific error code,
        /// or endpoint declines to use the more specific error code.
        /// </summary>
        ProtocolError = 0x101,
        /// <summary>
        /// H3_INTERNAL_ERROR (0x102):
        /// An internal error has occurred in the HTTP stack.
        /// </summary>
        InternalError = 0x102,
        /// <summary>
        ///  H3_STREAM_CREATION_ERROR (0x103):
        /// The endpoint detected that its peer created a stream that it will not accept.
        /// </summary>
        StreamCreationError = 0x103,
        /// <summary>
        /// H3_CLOSED_CRITICAL_STREAM (0x104):
        /// A stream required by the connection was closed or reset.
        /// </summary>
        ClosedCriticalStream = 0x104,
        /// <summary>
        /// H3_FRAME_UNEXPECTED (0x105):
        /// A frame was received which was not permitted in the current state.
        /// </summary>
        UnexpectedFrame = 0x105,
        /// <summary>
        /// H3_FRAME_ERROR (0x106):
        /// A frame that fails to satisfy layout requirements or with an invalid size was received.
        /// </summary>
        FrameError = 0x106,
        /// <summary>
        /// H3_EXCESSIVE_LOAD (0x107):
        /// The endpoint detected that its peer is exhibiting a behavior that might be generating excessive load.
        /// </summary>
        ExcessiveLoad = 0x107,
        /// <summary>
        /// H3_ID_ERROR (0x109):
        /// A Stream ID, Push ID, or Placeholder ID was used incorrectly, such as exceeding a limit, reducing a limit, or being reused.
        /// </summary>
        IdError = 0x108,
        /// <summary>
        /// H3_SETTINGS_ERROR (0x109):
        /// An endpoint detected an error in the payload of a SETTINGS frame.
        /// </summary>
        SettingsError = 0x109,
        /// <summary>
        /// H3_MISSING_SETTINGS (0x10A):
        /// No SETTINGS frame was received at the beginning of the control stream.
        /// </summary>
        MissingSettings = 0x10a,
        /// <summary>
        /// H3_REQUEST_REJECTED (0x10B):
        /// A server rejected a request without performing any application processing.
        /// </summary>
        RequestRejected = 0x10b,
        /// <summary>
        /// H3_REQUEST_CANCELLED (0x10C):
        /// The request or its response (including pushed response) is cancelled.
        /// </summary>
        RequestCancelled = 0x10c,
        /// <summary>
        /// H3_REQUEST_INCOMPLETE (0x10D):
        /// The client's stream terminated without containing a fully-formed request.
        /// </summary>
        RequestIncomplete = 0x10d,
        /// <summary>
        /// H3_MESSAGE_ERROR (0x10E):
        /// An HTTP message was malformed and cannot be processed.
        /// </summary>
        MessageError = 0x10e,
        /// <summary>
        /// H3_CONNECT_ERROR (0x10F):
        /// The connection established in response to a CONNECT request was reset or abnormally closed.
        /// </summary>
        ConnectError = 0x10f,
        /// <summary>
        /// H3_VERSION_FALLBACK (0x110):
        /// The requested operation cannot be served over HTTP/3. The peer should retry over HTTP/1.1.
        /// </summary>
        VersionFallback = 0x110,
        /// <summary>
        /// H3_QPACK_DECOMPRESSION_FAILED (0x200):
        /// The decoder failed to interpret an encoded field section and is not able to continue decoding that field section.
        /// </summary>
        QPackDecompressionFailed = 0x200,
        /// <summary>
        /// H3_QPACK_ENCODER_STREAM_ERROR (0x201):
        /// The decoder failed to interpret an encoder instruction received on the encoder stream.
        /// </summary>
        QPackEncoderStreamError = 0x201,
        /// <summary>
        /// H3_QPACK_DECODER_STREAM_ERROR (0x202):
        /// The encoder failed to interpret an decoder instruction received on the decoder stream.
        /// </summary>
        QPackDecoderStreamError = 0x202,
    }
}
