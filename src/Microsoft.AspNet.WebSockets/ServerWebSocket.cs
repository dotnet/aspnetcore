//------------------------------------------------------------------------------
// <copyright file="ServerWebSocket.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.WebSockets
{
    internal sealed class ServerWebSocket : WebSocketBase
    {
        private readonly SafeHandle _sessionHandle;
        private readonly UnsafeNativeMethods.WebSocketProtocolComponent.Property[] _properties;
        
        public ServerWebSocket(Stream innerStream,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
            : base(innerStream, subProtocol, keepAliveInterval, 
                WebSocketBuffer.CreateServerBuffer(internalBuffer, receiveBufferSize))
        {
            _properties = this.InternalBuffer.CreateProperties(false);
            _sessionHandle = this.CreateWebSocketHandle();

            if (_sessionHandle == null || _sessionHandle.IsInvalid)
            {
                WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
            }

            StartKeepAliveTimer();
        }

        internal override SafeHandle SessionHandle
        {
            get
            {
                Contract.Assert(_sessionHandle != null, "'m_SessionHandle MUST NOT be NULL.");
                return _sessionHandle;
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", 
            Justification = "No arbitrary data controlled by PT code is leaking into native code.")]
        private SafeHandle CreateWebSocketHandle()
        {
            Contract.Assert(_properties != null, "'m_Properties' MUST NOT be NULL.");
            SafeWebSocketHandle sessionHandle;
            UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketCreateServerHandle(_properties,
                _properties.Length, 
                out sessionHandle);
            Contract.Assert(sessionHandle != null, "'sessionHandle MUST NOT be NULL.");

            return sessionHandle;
        }
    }
}
