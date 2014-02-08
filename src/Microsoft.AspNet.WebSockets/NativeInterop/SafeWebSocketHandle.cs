//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.AspNet.WebSockets;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.WebSockets
{
    // This class is a wrapper for a WSPC (WebSocket protocol component) session. WebSocketCreateClientHandle and WebSocketCreateServerHandle return a PVOID and not a real handle
    // but we use a SafeHandle because it provides us the guarantee that WebSocketDeleteHandle will always get called.
    internal sealed class SafeWebSocketHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeWebSocketHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            if (this.IsInvalid)
            {
                return true;
            }

            UnsafeNativeMethods.WebSocketProtocolComponent.WebSocketDeleteHandle(this.handle);
            return true;
        }
    }
}
