// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.WebSockets.Internal
{
    /// <summary>
    /// Represents well-known WebSocket Close frame status codes.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc6455#section-7.4 for details
    /// </remarks>
    public enum WebSocketCloseStatus : ushort
    {
        /// <summary>
        /// Indicates that the purpose for the connection was fulfilled and thus the connection was closed normally.
        /// </summary>
        NormalClosure = 1000,

        /// <summary>
        /// Indicates that the other endpoint is going away, such as a server shutting down or a browser navigating to a new page.
        /// </summary>
        EndpointUnavailable = 1001,

        /// <summary>
        /// Indicates that a protocol error has occurred, causing the connection to be terminated.
        /// </summary>
        ProtocolError = 1002,

        /// <summary>
        /// Indicates an invalid message type was received. For example, if the end point only supports <see cref="WebSocketOpcode.Text"/> messages
        /// but received a <see cref="WebSocketOpcode.Binary"/> message.
        /// </summary>
        InvalidMessageType = 1003,

        /// <summary>
        /// Indicates that the Close frame did not have a status code. Not used in actual transmission.
        /// </summary>
        Empty = 1005,

        /// <summary>
        /// Indicates that the underlying transport connection was terminated without a proper close handshake. Not used in actual transmission.
        /// </summary>
        AbnormalClosure = 1006,

        /// <summary>
        /// Indicates that an invalid payload was encountered. For example, a frame of type <see cref="WebSocketOpcode.Text"/> contained non-UTF-8 data.
        /// </summary>
        InvalidPayloadData = 1007,

        /// <summary>
        /// Indicates that the connection is being terminated due to a violation of policy. This is a generic error code used whenever a party needs to terminate
        /// a connection without disclosing the specific reason.
        /// </summary>
        PolicyViolation = 1008,

        /// <summary>
        /// Indicates that the connection is being terminated due to an endpoint receiving a message that is too large.
        /// </summary>
        MessageTooBig = 1009,

        /// <summary>
        /// Indicates that the connection is being terminated due to being unable to negotiate a mandatory extension with the other party. Usually sent
        /// from the client to the server after the client finishes handshaking without negotiating the extension.
        /// </summary>
        MandatoryExtension = 1010,

        /// <summary>
        /// Indicates that a server is terminating the connection due to an internal error.
        /// </summary>
        InternalServerError = 1011,

        /// <summary>
        /// Indicates that the connection failed to establish because the TLS handshake failed. Not used in actual transmission.
        /// </summary>
        TLSHandshakeFailed = 1015
    }
}