// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.Win32.SafeHandles;

namespace Microsoft.Net.WebSockets
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
