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

namespace Microsoft.AspNet.WebSockets
{
    /// <summary>
    /// Standard keys and values for use within the OWIN interfaces
    /// </summary>
    internal static class Constants
    {
        internal const string WebSocketAcceptKey = "websocket.Accept";
        internal const string WebSocketSubProtocolKey = "websocket.SubProtocol";
        internal const string WebSocketSendAsyncKey = "websocket.SendAsync";
        internal const string WebSocketReceiveAyncKey = "websocket.ReceiveAsync";
        internal const string WebSocketCloseAsyncKey = "websocket.CloseAsync";
        internal const string WebSocketCallCancelledKey = "websocket.CallCancelled";
        internal const string WebSocketVersionKey = "websocket.Version";
        internal const string WebSocketVersion = "1.0";
        internal const string WebSocketCloseStatusKey = "websocket.ClientCloseStatus";
        internal const string WebSocketCloseDescriptionKey = "websocket.ClientCloseDescription";
    }
}
