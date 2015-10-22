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
// <copyright file="WebSocketException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace Microsoft.Net.WebSockets
{
#if !DOTNET5_4
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
#endif
    internal sealed class WebSocketException : Win32Exception
    {
        private WebSocketError _WebSocketErrorCode;

        public WebSocketException()
            : this(Marshal.GetLastWin32Error())
        {         
        }

        public WebSocketException(WebSocketError error)
            : this(error, GetErrorMessage(error))
        {
        }

        public WebSocketException(WebSocketError error, string message) : base(message)
        {
            _WebSocketErrorCode = error;
        }

        public WebSocketException(WebSocketError error, Exception innerException)
            : this(error, GetErrorMessage(error), innerException)
        {
        }

        public WebSocketException(WebSocketError error, string message, Exception innerException) 
            : base(message, innerException)
        {
            _WebSocketErrorCode = error;
        }

        public WebSocketException(int nativeError)
            : base(nativeError)
        {
            _WebSocketErrorCode = !UnsafeNativeMethods.WebSocketProtocolComponent.Succeeded(nativeError) ? WebSocketError.NativeError : WebSocketError.Success;
            this.SetErrorCodeOnError(nativeError);
        }

        public WebSocketException(int nativeError, string message) 
            : base(nativeError, message)
        {
            _WebSocketErrorCode = !UnsafeNativeMethods.WebSocketProtocolComponent.Succeeded(nativeError) ? WebSocketError.NativeError : WebSocketError.Success;
            this.SetErrorCodeOnError(nativeError);
        }

        public WebSocketException(int nativeError, Exception innerException)
            : base(SR.GetString(SR.net_WebSockets_Generic), innerException)
        {
            _WebSocketErrorCode = !UnsafeNativeMethods.WebSocketProtocolComponent.Succeeded(nativeError) ? WebSocketError.NativeError : WebSocketError.Success;
            this.SetErrorCodeOnError(nativeError);
        }

        public WebSocketException(WebSocketError error, int nativeError)
            : this(error, nativeError, GetErrorMessage(error))
        {
        }

        public WebSocketException(WebSocketError error, int nativeError, string message)
            : base(message)
        {
            _WebSocketErrorCode = error;
            this.SetErrorCodeOnError(nativeError);
        }

        public WebSocketException(WebSocketError error, int nativeError, Exception innerException)
            : this(error, nativeError, GetErrorMessage(error), innerException)
        {
        }

        public WebSocketException(WebSocketError error, int nativeError, string message, Exception innerException)
            : base(message, innerException)
        {
            _WebSocketErrorCode = error;
            this.SetErrorCodeOnError(nativeError);
        }

        public WebSocketException(string message)
            : base(message)
        {
        }

        public WebSocketException(string message, Exception innerException)
            : base(message, innerException)
        { 
        }

        public override int ErrorCode
        {
            get
            {
                return base.NativeErrorCode;
            }
        }

        public WebSocketError WebSocketErrorCode
        {
            get
            {
                return _WebSocketErrorCode; 
            }
        }

        private static string GetErrorMessage(WebSocketError error)
        {
            // provide a canned message for the error type
            switch (error)
            {
                case WebSocketError.InvalidMessageType:
                    return SR.GetString(SR.net_WebSockets_InvalidMessageType_Generic,
                        typeof(WebSocket).Name + WebSocketBase.Methods.CloseAsync,
                        typeof(WebSocket).Name + WebSocketBase.Methods.CloseOutputAsync);
                case WebSocketError.Faulted:
                    return SR.GetString(SR.net_Websockets_WebSocketBaseFaulted);
                case WebSocketError.NotAWebSocket:
                    return SR.GetString(SR.net_WebSockets_NotAWebSocket_Generic);
                case WebSocketError.UnsupportedVersion:
                    return SR.GetString(SR.net_WebSockets_UnsupportedWebSocketVersion_Generic);
                case WebSocketError.UnsupportedProtocol:
                    return SR.GetString(SR.net_WebSockets_UnsupportedProtocol_Generic);
                case WebSocketError.HeaderError:
                    return SR.GetString(SR.net_WebSockets_HeaderError_Generic);
                case WebSocketError.ConnectionClosedPrematurely:
                    return SR.GetString(SR.net_WebSockets_ConnectionClosedPrematurely_Generic);
                case WebSocketError.InvalidState:
                    return SR.GetString(SR.net_WebSockets_InvalidState_Generic);
                default:
                    return SR.GetString(SR.net_WebSockets_Generic);
            }
        }

        // Set the error code only if there is an error (i.e. nativeError >= 0). Otherwise the code blows up on deserialization 
        // as the Exception..ctor() throws on setting HResult to 0. The default for HResult is -2147467259.
        private void SetErrorCodeOnError(int nativeError)
        {
            if (!UnsafeNativeMethods.WebSocketProtocolComponent.Succeeded(nativeError))
            {
                this.HResult = nativeError;
            }
        }
    }
}
