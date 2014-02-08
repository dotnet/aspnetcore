//------------------------------------------------------------------------------
// <copyright from='1997' to='2001' company='Microsoft Corporation'>           
//    Copyright (c) Microsoft Corporation. All Rights Reserved.                
//    Information Contained Herein is Proprietary and Confidential.            
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System
{
    internal sealed class SR
    {
        internal const string net_servicePointAddressNotSupportedInHostMode = "net_servicePointAddressNotSupportedInHostMode";
        internal const string net_Websockets_AlreadyOneOutstandingOperation = "net_Websockets_AlreadyOneOutstandingOperation";
        internal const string net_Websockets_WebSocketBaseFaulted = "net_Websockets_WebSocketBaseFaulted";
        internal const string net_WebSockets_NativeSendResponseHeaders = "net_WebSockets_NativeSendResponseHeaders";
        internal const string net_WebSockets_Generic = "net_WebSockets_Generic";
        internal const string net_WebSockets_NotAWebSocket_Generic = "net_WebSockets_NotAWebSocket_Generic";
        internal const string net_WebSockets_UnsupportedWebSocketVersion_Generic = "net_WebSockets_UnsupportedWebSocketVersion_Generic";
        internal const string net_WebSockets_HeaderError_Generic = "net_WebSockets_HeaderError_Generic";
        internal const string net_WebSockets_UnsupportedProtocol_Generic = "net_WebSockets_UnsupportedProtocol_Generic";
        internal const string net_WebSockets_UnsupportedPlatform = "net_WebSockets_UnsupportedPlatform";
        internal const string net_WebSockets_AcceptNotAWebSocket = "net_WebSockets_AcceptNotAWebSocket";
        internal const string net_WebSockets_AcceptUnsupportedWebSocketVersion = "net_WebSockets_AcceptUnsupportedWebSocketVersion";
        internal const string net_WebSockets_AcceptHeaderNotFound = "net_WebSockets_AcceptHeaderNotFound";
        internal const string net_WebSockets_AcceptUnsupportedProtocol = "net_WebSockets_AcceptUnsupportedProtocol";
        internal const string net_WebSockets_ClientAcceptingNoProtocols = "net_WebSockets_ClientAcceptingNoProtocols";
        internal const string net_WebSockets_ClientSecWebSocketProtocolsBlank = "net_WebSockets_ClientSecWebSocketProtocolsBlank";
        internal const string net_WebSockets_ArgumentOutOfRange_TooSmall = "net_WebSockets_ArgumentOutOfRange_TooSmall";
        internal const string net_WebSockets_ArgumentOutOfRange_InternalBuffer = "net_WebSockets_ArgumentOutOfRange_InternalBuffer";
        internal const string net_WebSockets_ArgumentOutOfRange_TooBig = "net_WebSockets_ArgumentOutOfRange_TooBig";
        internal const string net_WebSockets_InvalidState_Generic = "net_WebSockets_InvalidState_Generic";
        internal const string net_WebSockets_InvalidState_ClosedOrAborted = "net_WebSockets_InvalidState_ClosedOrAborted";
        internal const string net_WebSockets_InvalidState = "net_WebSockets_InvalidState";
        internal const string net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync = "net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync";
        internal const string net_WebSockets_InvalidMessageType = "net_WebSockets_InvalidMessageType";
        internal const string net_WebSockets_InvalidBufferType = "net_WebSockets_InvalidBufferType";
        internal const string net_WebSockets_InvalidMessageType_Generic = "net_WebSockets_InvalidMessageType_Generic";
        internal const string net_WebSockets_Argument_InvalidMessageType = "net_WebSockets_Argument_InvalidMessageType";
        internal const string net_WebSockets_ConnectionClosedPrematurely_Generic = "net_WebSockets_ConnectionClosedPrematurely_Generic";
        internal const string net_WebSockets_InvalidCharInProtocolString = "net_WebSockets_InvalidCharInProtocolString";
        internal const string net_WebSockets_InvalidEmptySubProtocol = "net_WebSockets_InvalidEmptySubProtocol";
        internal const string net_WebSockets_ReasonNotNull = "net_WebSockets_ReasonNotNull";
        internal const string net_WebSockets_InvalidCloseStatusCode = "net_WebSockets_InvalidCloseStatusCode";
        internal const string net_WebSockets_InvalidCloseStatusDescription = "net_WebSockets_InvalidCloseStatusDescription";
        internal const string net_WebSockets_Scheme = "net_WebSockets_Scheme";
        internal const string net_WebSockets_AlreadyStarted = "net_WebSockets_AlreadyStarted";
        internal const string net_WebSockets_Connect101Expected = "net_WebSockets_Connect101Expected";
        internal const string net_WebSockets_InvalidResponseHeader = "net_WebSockets_InvalidResponseHeader";
        internal const string net_WebSockets_NotConnected = "net_WebSockets_NotConnected";
        internal const string net_WebSockets_InvalidRegistration = "net_WebSockets_InvalidRegistration";
        internal const string net_WebSockets_NoDuplicateProtocol = "net_WebSockets_NoDuplicateProtocol";
       
        internal const string NotReadableStream = "NotReadableStream";
        internal const string NotWriteableStream = "NotWriteableStream";
        
        public static string GetString(string name, params object[] args)
        {
            return name;
        }

        public static string GetString(string name)
        {
            return name;
        }
    }
}
