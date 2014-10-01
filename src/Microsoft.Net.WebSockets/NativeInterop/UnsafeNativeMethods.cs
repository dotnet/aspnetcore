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
// <copyright file="UnsafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Net.WebSockets
{
    internal static class UnsafeNativeMethods
    {
#if ASPNETCORE50
        private const string api_ms_win_core_libraryloader_LIB = "api-ms-win-core-libraryloader-l1-1-0.dll";
#else    
        private const string KERNEL32 = "kernel32.dll";
#endif
        private const string WEBSOCKET = "websocket.dll";

        internal static class SafeNetHandles
        {
#if ASPNETCORE50
            [DllImport(api_ms_win_core_libraryloader_LIB, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, CharSet=CharSet.Unicode, SetLastError = true)]
#endif
            internal static extern unsafe SafeLoadLibrary LoadLibraryExW([In] string lpwLibFileName, [In] void* hFile, [In] uint dwFlags);

#if ASPNETCORE50
            [DllImport(api_ms_win_core_libraryloader_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            internal static extern unsafe bool FreeLibrary([In] IntPtr hModule);
        }

        internal static class WebSocketProtocolComponent
        {
            private static readonly string DllFileName;
            private static readonly string DummyWebsocketKeyBase64 = Convert.ToBase64String(new byte[16]);
            private static readonly SafeLoadLibrary WebSocketDllHandle;
            private static readonly string PrivateSupportedVersion;

            private static readonly HttpHeader[] InitialClientRequestHeaders = new HttpHeader[] 
            {
                new HttpHeader()
                { 
                    Name = HttpKnownHeaderNames.Connection, 
                    NameLength = (uint)HttpKnownHeaderNames.Connection.Length, 
                    Value = HttpKnownHeaderNames.Upgrade, 
                    ValueLength = (uint)HttpKnownHeaderNames.Upgrade.Length 
                },
                new HttpHeader()
                { 
                    Name = HttpKnownHeaderNames.Upgrade, 
                    NameLength = (uint)HttpKnownHeaderNames.Upgrade.Length, 
                    Value = WebSocketHelpers.WebSocketUpgradeToken, 
                    ValueLength = (uint)WebSocketHelpers.WebSocketUpgradeToken.Length 
                }
            };

            private static readonly HttpHeader[] ServerFakeRequestHeaders;

            internal static class Errors
            {
                internal const int E_INVALID_OPERATION = unchecked((int)0x80000050);
                internal const int E_INVALID_PROTOCOL_OPERATION = unchecked((int)0x80000051);
                internal const int E_INVALID_PROTOCOL_FORMAT = unchecked((int)0x80000052);
                internal const int E_NUMERIC_OVERFLOW = unchecked((int)0x80000053);
                internal const int E_FAIL = unchecked((int)0x80004005);
            }

            internal enum Action
            {
                NoAction = 0,
                SendToNetwork = 1,
                IndicateSendComplete = 2,
                ReceiveFromNetwork = 3,
                IndicateReceiveComplete = 4,
            }

            internal enum BufferType : uint
            {
                None = 0x00000000,
                UTF8Message = 0x80000000,
                UTF8Fragment = 0x80000001,
                BinaryMessage = 0x80000002,
                BinaryFragment = 0x80000003,
                Close = 0x80000004,
                PingPong = 0x80000005,
                UnsolicitedPong = 0x80000006
            }

            internal enum PropertyType
            {
                ReceiveBufferSize = 0,
                SendBufferSize = 1,
                DisableMasking = 2,
                AllocatedBuffer = 3,
                DisableUtf8Verification = 4,
                KeepAliveInterval = 5,
            }

            internal enum ActionQueue
            {
                Send = 1,
                Receive = 2,
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Property
            {
                internal PropertyType Type;
                internal IntPtr PropertyData;
                internal uint PropertySize;
            }

            [StructLayout(LayoutKind.Explicit)]
            internal struct Buffer
            {
                [FieldOffset(0)]
                internal DataBuffer Data;
                [FieldOffset(0)]
                internal CloseBuffer CloseStatus;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct DataBuffer
            {
                internal IntPtr BufferData;
                internal uint BufferLength;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct CloseBuffer
            {
                internal IntPtr ReasonData;
                internal uint ReasonLength;
                internal ushort CloseStatus;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct HttpHeader
            {
                [MarshalAs(UnmanagedType.LPStr)]
                internal string Name;
                internal uint NameLength;
                [MarshalAs(UnmanagedType.LPStr)]
                internal string Value;
                internal uint ValueLength;
            }

            static WebSocketProtocolComponent()
            {
#if ASPNET50
                DllFileName = Path.Combine(Environment.SystemDirectory, WEBSOCKET);
#else
                DllFileName = Path.Combine(Environment.GetEnvironmentVariable("SYSTEMROOT"), "System32", WEBSOCKET);
#endif
                WebSocketDllHandle = SafeLoadLibrary.LoadLibraryEx(DllFileName);

                if (!WebSocketDllHandle.IsInvalid)
                {
                    PrivateSupportedVersion = GetSupportedVersion();

                    ServerFakeRequestHeaders = new HttpHeader[] 
                {
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.Connection, 
                        NameLength = (uint)HttpKnownHeaderNames.Connection.Length, 
                        Value = HttpKnownHeaderNames.Upgrade, 
                        ValueLength = (uint)HttpKnownHeaderNames.Upgrade.Length 
                    },
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.Upgrade, 
                        NameLength = (uint)HttpKnownHeaderNames.Upgrade.Length, 
                        Value = WebSocketHelpers.WebSocketUpgradeToken, 
                        ValueLength = (uint)WebSocketHelpers.WebSocketUpgradeToken.Length 
                    },
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.Host, 
                        NameLength = (uint)HttpKnownHeaderNames.Host.Length, 
                        Value = string.Empty, 
                        ValueLength = 0 
                    },
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.SecWebSocketVersion, 
                        NameLength = (uint)HttpKnownHeaderNames.SecWebSocketVersion.Length, 
                        Value = SupportedVersion, 
                        ValueLength = (uint)SupportedVersion.Length 
                    },
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.SecWebSocketKey, 
                        NameLength = (uint)HttpKnownHeaderNames.SecWebSocketKey.Length, 
                        Value = DummyWebsocketKeyBase64, 
                        ValueLength = (uint)DummyWebsocketKeyBase64.Length 
                    }
                };
                }
            }

            internal static string SupportedVersion
            {
                get
                {
                    if (WebSocketDllHandle.IsInvalid)
                    {
                        WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
                    }

                    return PrivateSupportedVersion;
                }
            }

            internal static bool IsSupported
            {
                get
                {
                    return !WebSocketDllHandle.IsInvalid;
                }
            }

            internal static string GetSupportedVersion()
            {
                if (WebSocketDllHandle.IsInvalid)
                {
                    WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
                }

                SafeWebSocketHandle webSocketHandle = null;
                try
                {
                    int errorCode = WebSocketCreateClientHandle_Raw(null, 0, out webSocketHandle);
                    ThrowOnError(errorCode);

                    if (webSocketHandle == null ||
                        webSocketHandle.IsInvalid)
                    {
                        WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
                    }

                    IntPtr additionalHeadersPtr;
                    uint additionalHeaderCount;

                    errorCode = WebSocketBeginClientHandshake_Raw(webSocketHandle,
                        IntPtr.Zero,
                        0,
                        IntPtr.Zero,
                        0,
                        InitialClientRequestHeaders,
                        (uint)InitialClientRequestHeaders.Length,
                        out additionalHeadersPtr,
                        out additionalHeaderCount);
                    ThrowOnError(errorCode);

                    HttpHeader[] additionalHeaders = MarshalHttpHeaders(additionalHeadersPtr, (int)additionalHeaderCount);

                    string version = null;
                    foreach (HttpHeader header in additionalHeaders)
                    {
                        if (string.Compare(header.Name,
                                HttpKnownHeaderNames.SecWebSocketVersion,
                                StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            version = header.Value;
                            break;
                        }
                    }
                    Contract.Assert(version != null, "'version' MUST NOT be NULL.");

                    return version;
                }
                finally
                {
                    if (webSocketHandle != null)
                    {
                        webSocketHandle.Dispose();
                    }
                }
            }

            internal static void WebSocketCreateClientHandle(Property[] properties,
                out SafeWebSocketHandle webSocketHandle)
            {
                uint propertyCount = properties == null ? 0 : (uint)properties.Length;

                if (WebSocketDllHandle.IsInvalid)
                {
                    WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
                }

                int errorCode = WebSocketCreateClientHandle_Raw(properties, propertyCount, out webSocketHandle);
                ThrowOnError(errorCode);

                if (webSocketHandle == null ||
                    webSocketHandle.IsInvalid)
                {
                    WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
                }

                IntPtr additionalHeadersPtr;
                uint additionalHeaderCount;

                // Currently the WSPC doesn't allow to initiate a data session
                // without also being involved in the http handshake
                // There is no information whatsoever, which is needed by the
                // WSPC for parsing WebSocket frames from the HTTP handshake
                // In the managed implementation the HTTP header handling
                // will be done using the managed HTTP stack and we will
                // just fake an HTTP handshake for the WSPC  calling
                // WebSocketBeginClientHandshake and WebSocketEndClientHandshake
                // with statically defined dummy headers.
                errorCode = WebSocketBeginClientHandshake_Raw(webSocketHandle,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    InitialClientRequestHeaders,
                    (uint)InitialClientRequestHeaders.Length,
                    out additionalHeadersPtr,
                    out additionalHeaderCount);

                ThrowOnError(errorCode);

                HttpHeader[] additionalHeaders = MarshalHttpHeaders(additionalHeadersPtr, (int)additionalHeaderCount);

                string key = null;
                foreach (HttpHeader header in additionalHeaders)
                {
                    if (string.Compare(header.Name,
                            HttpKnownHeaderNames.SecWebSocketKey,
                            StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        key = header.Value;
                        break;
                    }
                }
                Contract.Assert(key != null, "'key' MUST NOT be NULL.");

                string acceptValue = WebSocketHelpers.GetSecWebSocketAcceptString(key);
                HttpHeader[] responseHeaders = new HttpHeader[] 
                {
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.Connection, 
                        NameLength = (uint)HttpKnownHeaderNames.Connection.Length, 
                        Value = HttpKnownHeaderNames.Upgrade, 
                        ValueLength = (uint)HttpKnownHeaderNames.Upgrade.Length 
                    },
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.Upgrade, 
                        NameLength = (uint)HttpKnownHeaderNames.Upgrade.Length, 
                        Value = WebSocketHelpers.WebSocketUpgradeToken, 
                        ValueLength = (uint)WebSocketHelpers.WebSocketUpgradeToken.Length 
                    },
                    new HttpHeader()
                    { 
                        Name = HttpKnownHeaderNames.SecWebSocketAccept, 
                        NameLength = (uint)HttpKnownHeaderNames.SecWebSocketAccept.Length, 
                        Value = acceptValue, 
                        ValueLength = (uint)acceptValue.Length 
                    }
                };

                errorCode = WebSocketEndClientHandshake_Raw(webSocketHandle,
                    responseHeaders,
                    (uint)responseHeaders.Length,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);

                ThrowOnError(errorCode);

                Contract.Assert(webSocketHandle != null, "'webSocketHandle' MUST NOT be NULL at this point.");
            }

            internal static void WebSocketCreateServerHandle(Property[] properties,
                int propertyCount,
                out SafeWebSocketHandle webSocketHandle)
            {
                Contract.Assert(propertyCount >= 0, "'propertyCount' MUST NOT be negative.");
                Contract.Assert((properties == null && propertyCount == 0) ||
                    (properties != null && propertyCount == properties.Length),
                    "'propertyCount' MUST MATCH 'properties.Length'.");

                if (WebSocketDllHandle.IsInvalid)
                {
                    WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
                }

                int errorCode = WebSocketCreateServerHandle_Raw(properties, (uint)propertyCount, out webSocketHandle);
                ThrowOnError(errorCode);

                if (webSocketHandle == null ||
                    webSocketHandle.IsInvalid)
                {
                    WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
                }

                IntPtr responseHeadersPtr;
                uint responseHeaderCount;

                // Currently the WSPC doesn't allow to initiate a data session
                // without also being involved in the http handshake
                // There is no information whatsoever, which is needed by the
                // WSPC for parsing WebSocket frames from the HTTP handshake
                // In the managed implementation the HTTP header handling
                // will be done using the managed HTTP stack and we will
                // just fake an HTTP handshake for the WSPC calling
                // WebSocketBeginServerHandshake and WebSocketEndServerHandshake
                // with statically defined dummy headers.
                errorCode = WebSocketBeginServerHandshake_Raw(webSocketHandle,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    ServerFakeRequestHeaders,
                    (uint)ServerFakeRequestHeaders.Length,
                    out responseHeadersPtr,
                    out responseHeaderCount);

                ThrowOnError(errorCode);

                HttpHeader[] responseHeaders = MarshalHttpHeaders(responseHeadersPtr, (int)responseHeaderCount);
                errorCode = WebSocketEndServerHandshake_Raw(webSocketHandle);

                ThrowOnError(errorCode);

                Contract.Assert(webSocketHandle != null, "'webSocketHandle' MUST NOT be NULL at this point.");
            }

            internal static void WebSocketAbortHandle(SafeHandle webSocketHandle)
            {
                Contract.Assert(webSocketHandle != null && !webSocketHandle.IsInvalid,
                    "'webSocketHandle' MUST NOT be NULL or INVALID.");

                WebSocketAbortHandle_Raw(webSocketHandle);

                DrainActionQueue(webSocketHandle, ActionQueue.Send);
                DrainActionQueue(webSocketHandle, ActionQueue.Receive);
            }

            internal static void WebSocketDeleteHandle(IntPtr webSocketPtr)
            {
                Contract.Assert(webSocketPtr != IntPtr.Zero, "'webSocketPtr' MUST NOT be IntPtr.Zero.");
                WebSocketDeleteHandle_Raw(webSocketPtr);
            }

            internal static void WebSocketSend(WebSocketBase webSocket,
                BufferType bufferType,
                Buffer buffer)
            {
                Contract.Assert(webSocket != null,
                    "'webSocket' MUST NOT be NULL or INVALID.");
                Contract.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                    "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");

                ThrowIfSessionHandleClosed(webSocket);

                int errorCode;
                try
                {
                    errorCode = WebSocketSend_Raw(webSocket.SessionHandle, bufferType, ref buffer, IntPtr.Zero);
                }
                catch (ObjectDisposedException innerException)
                {
                    throw ConvertObjectDisposedException(webSocket, innerException);
                }

                ThrowOnError(errorCode);
            }

            internal static void WebSocketSendWithoutBody(WebSocketBase webSocket,
                BufferType bufferType)
            {
                Contract.Assert(webSocket != null,
                    "'webSocket' MUST NOT be NULL or INVALID.");
                Contract.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                    "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");

                ThrowIfSessionHandleClosed(webSocket);

                int errorCode;
                try
                {
                    errorCode = WebSocketSendWithoutBody_Raw(webSocket.SessionHandle, bufferType, IntPtr.Zero, IntPtr.Zero);
                }
                catch (ObjectDisposedException innerException)
                {
                    throw ConvertObjectDisposedException(webSocket, innerException);
                }

                ThrowOnError(errorCode);
            }

            internal static void WebSocketReceive(WebSocketBase webSocket)
            {
                Contract.Assert(webSocket != null,
                    "'webSocket' MUST NOT be NULL or INVALID.");
                Contract.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                    "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");

                ThrowIfSessionHandleClosed(webSocket);

                int errorCode;
                try
                {
                    errorCode = WebSocketReceive_Raw(webSocket.SessionHandle, IntPtr.Zero, IntPtr.Zero);
                }
                catch (ObjectDisposedException innerException)
                {
                    throw ConvertObjectDisposedException(webSocket, innerException);
                }

                ThrowOnError(errorCode);
            }

            internal static void WebSocketGetAction(WebSocketBase webSocket,
                ActionQueue actionQueue,
                Buffer[] dataBuffers,
                ref uint dataBufferCount,
                out Action action,
                out BufferType bufferType,
                out IntPtr actionContext)
            {
                Contract.Assert(webSocket != null,
                    "'webSocket' MUST NOT be NULL or INVALID.");
                Contract.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                    "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");
                Contract.Assert(dataBufferCount >= 0, "'dataBufferCount' MUST NOT be negative.");
                Contract.Assert((dataBuffers == null && dataBufferCount == 0) ||
                    (dataBuffers != null && dataBufferCount == dataBuffers.Length),
                    "'dataBufferCount' MUST MATCH 'dataBuffers.Length'.");

                action = Action.NoAction;
                bufferType = BufferType.None;
                actionContext = IntPtr.Zero;

                IntPtr dummy;
                ThrowIfSessionHandleClosed(webSocket);

                int errorCode;
                try
                {
                    errorCode = WebSocketGetAction_Raw(webSocket.SessionHandle,
                        actionQueue,
                        dataBuffers,
                        ref dataBufferCount,
                        out action,
                        out bufferType,
                        out dummy,
                        out actionContext);
                }
                catch (ObjectDisposedException innerException)
                {
                    throw ConvertObjectDisposedException(webSocket, innerException);
                }
                ThrowOnError(errorCode);

                webSocket.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);

                Contract.Assert(dataBufferCount >= 0);
                Contract.Assert((dataBufferCount == 0 && dataBuffers == null) ||
                    (dataBufferCount <= dataBuffers.Length));
            }

            internal static void WebSocketCompleteAction(WebSocketBase webSocket,
                IntPtr actionContext,
                int bytesTransferred)
            {
                Contract.Assert(webSocket != null,
                    "'webSocket' MUST NOT be NULL or INVALID.");
                Contract.Assert(webSocket.SessionHandle != null && !webSocket.SessionHandle.IsInvalid,
                    "'webSocket.SessionHandle' MUST NOT be NULL or INVALID.");
                Contract.Assert(actionContext != IntPtr.Zero, "'actionContext' MUST NOT be IntPtr.Zero.");
                Contract.Assert(bytesTransferred >= 0, "'bytesTransferred' MUST NOT be negative.");

                if (webSocket.SessionHandle.IsClosed)
                {
                    return;
                }

                try
                {
                    WebSocketCompleteAction_Raw(webSocket.SessionHandle, actionContext, (uint)bytesTransferred);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            internal static TimeSpan WebSocketGetDefaultKeepAliveInterval()
            {
                uint result = 0;
                uint size = sizeof(uint);
                int errorCode = WebSocketGetGlobalProperty_Raw(PropertyType.KeepAliveInterval, ref result, ref size);
                if (!Succeeded(errorCode))
                {
                    Contract.Assert(errorCode == 0, "errorCode: " + errorCode);
                    return Timeout.InfiniteTimeSpan;
                }
                return TimeSpan.FromMilliseconds(result);
            }

            private static void DrainActionQueue(SafeHandle webSocketHandle, ActionQueue actionQueue)
            {
                Contract.Assert(webSocketHandle != null && !webSocketHandle.IsInvalid,
                    "'webSocketHandle' MUST NOT be NULL or INVALID.");

                IntPtr actionContext;
                IntPtr dummy;
                Action action;
                BufferType bufferType;

                while (true)
                {
                    Buffer[] dataBuffers = new Buffer[1];
                    uint dataBufferCount = 1;
                    int errorCode = WebSocketGetAction_Raw(webSocketHandle,
                        actionQueue,
                        dataBuffers,
                        ref dataBufferCount,
                        out action,
                        out bufferType,
                        out dummy,
                        out actionContext);

                    if (!Succeeded(errorCode))
                    {
                        Contract.Assert(errorCode == 0, "'errorCode' MUST be 0.");
                        return;
                    }

                    if (action == Action.NoAction)
                    {
                        return;
                    }

                    WebSocketCompleteAction_Raw(webSocketHandle, actionContext, 0);
                }
            }

            private static void MarshalAndVerifyHttpHeader(IntPtr httpHeaderPtr,
                ref HttpHeader httpHeader)
            {
                Contract.Assert(httpHeaderPtr != IntPtr.Zero, "'currentHttpHeaderPtr' MUST NOT be IntPtr.Zero.");

                IntPtr httpHeaderNamePtr = Marshal.ReadIntPtr(httpHeaderPtr);
                IntPtr lengthPtr = IntPtr.Add(httpHeaderPtr, IntPtr.Size);
                int length = Marshal.ReadInt32(lengthPtr);
                Contract.Assert(length >= 0, "'length' MUST NOT be negative.");

                if (httpHeaderNamePtr != IntPtr.Zero)
                {
                    httpHeader.Name = Marshal.PtrToStringAnsi(httpHeaderNamePtr, length);
                }

                if ((httpHeader.Name == null && length != 0) ||
                    (httpHeader.Name != null && length != httpHeader.Name.Length))
                {
                    Contract.Assert(false, "The length of 'httpHeader.Name' MUST MATCH 'length'.");
                    throw new AccessViolationException();
                }

                // structure of HttpHeader:
                //   Name = string*
                //   NameLength = uint*
                //   Value = string*
                //   ValueLength = uint*
                // NOTE - All fields in the object are pointers to the actual value, hence the use of 
                //        n * IntPtr.Size to get to the correct place in the object. 
                int valueOffset = 2 * IntPtr.Size;
                int lengthOffset = 3 * IntPtr.Size;

                IntPtr httpHeaderValuePtr =
                    Marshal.ReadIntPtr(IntPtr.Add(httpHeaderPtr, valueOffset));
                lengthPtr = IntPtr.Add(httpHeaderPtr, lengthOffset);
                length = Marshal.ReadInt32(lengthPtr);
                httpHeader.Value = Marshal.PtrToStringAnsi(httpHeaderValuePtr, (int)length);

                if ((httpHeader.Value == null && length != 0) ||
                    (httpHeader.Value != null && length != httpHeader.Value.Length))
                {
                    Contract.Assert(false, "The length of 'httpHeader.Value' MUST MATCH 'length'.");
                    throw new AccessViolationException();
                }
            }

            private static HttpHeader[] MarshalHttpHeaders(IntPtr nativeHeadersPtr,
                int nativeHeaderCount)
            {
                Contract.Assert(nativeHeaderCount >= 0, "'nativeHeaderCount' MUST NOT be negative.");
                Contract.Assert(nativeHeadersPtr != IntPtr.Zero || nativeHeaderCount == 0,
                    "'nativeHeaderCount' MUST be 0.");

                HttpHeader[] httpHeaders = new HttpHeader[nativeHeaderCount];

                // structure of HttpHeader:
                //   Name = string*
                //   NameLength = uint*
                //   Value = string*
                //   ValueLength = uint*
                // NOTE - All fields in the object are pointers to the actual value, hence the use of 
                //        4 * IntPtr.Size to get to the next header. 
                int httpHeaderStructSize = 4 * IntPtr.Size;

                for (int i = 0; i < nativeHeaderCount; i++)
                {
                    int offset = httpHeaderStructSize * i;
                    IntPtr currentHttpHeaderPtr = IntPtr.Add(nativeHeadersPtr, offset);
                    MarshalAndVerifyHttpHeader(currentHttpHeaderPtr, ref httpHeaders[i]);
                }

                Contract.Assert(httpHeaders != null);
                Contract.Assert(httpHeaders.Length == nativeHeaderCount);

                return httpHeaders;
            }

            public static bool Succeeded(int hr)
            {
                return (hr >= 0);
            }

            private static void ThrowOnError(int errorCode)
            {
                if (Succeeded(errorCode))
                {
                    return;
                }

                throw new WebSocketException(errorCode);
            }

            private static void ThrowIfSessionHandleClosed(WebSocketBase webSocket)
            {
                if (webSocket.SessionHandle.IsClosed)
                {
                    throw new WebSocketException(WebSocketError.InvalidState,
                        SR.GetString(SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State));
                }
            }

            private static WebSocketException ConvertObjectDisposedException(WebSocketBase webSocket, ObjectDisposedException innerException)
            {
                return new WebSocketException(WebSocketError.InvalidState,
                    SR.GetString(SR.net_WebSockets_InvalidState_ClosedOrAborted, webSocket.GetType().FullName, webSocket.State),
                    innerException);
            }

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketCreateClientHandle", ExactSpelling = true)]
            private static extern int WebSocketCreateClientHandle_Raw(
                [In]Property[] properties,
                [In] uint propertyCount,
                [Out] out SafeWebSocketHandle webSocketHandle);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketBeginClientHandshake", ExactSpelling = true)]
            private static extern int WebSocketBeginClientHandshake_Raw(
                [In] SafeHandle webSocketHandle,
                [In] IntPtr subProtocols,
                [In] uint subProtocolCount,
                [In] IntPtr extensions,
                [In] uint extensionCount,
                [In] HttpHeader[] initialHeaders,
                [In] uint initialHeaderCount,
                [Out] out IntPtr additionalHeadersPtr,
                [Out] out uint additionalHeaderCount);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketEndClientHandshake", ExactSpelling = true)]
            private static extern int WebSocketEndClientHandshake_Raw([In] SafeHandle webSocketHandle,
                [In] HttpHeader[] responseHeaders,
                [In] uint responseHeaderCount,
                [In, Out] IntPtr selectedExtensions,
                [In] IntPtr selectedExtensionCount,
                [In] IntPtr selectedSubProtocol);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketBeginServerHandshake", ExactSpelling = true)]
            private static extern int WebSocketBeginServerHandshake_Raw(
                [In] SafeHandle webSocketHandle,
                [In] IntPtr subProtocol,
                [In] IntPtr extensions,
                [In] uint extensionCount,
                [In] HttpHeader[] requestHeaders,
                [In] uint requestHeaderCount,
                [Out] out IntPtr responseHeadersPtr,
                [Out] out uint responseHeaderCount);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketEndServerHandshake", ExactSpelling = true)]
            private static extern int WebSocketEndServerHandshake_Raw([In] SafeHandle webSocketHandle);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketCreateServerHandle", ExactSpelling = true)]
            private static extern int WebSocketCreateServerHandle_Raw(
                [In]Property[] properties,
                [In] uint propertyCount,
                [Out] out SafeWebSocketHandle webSocketHandle);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketAbortHandle", ExactSpelling = true)]
            private static extern void WebSocketAbortHandle_Raw(
                [In] SafeHandle webSocketHandle);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketDeleteHandle", ExactSpelling = true)]
            private static extern void WebSocketDeleteHandle_Raw(
                [In] IntPtr webSocketHandle);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketSend", ExactSpelling = true)]
            private static extern int WebSocketSend_Raw(
                [In] SafeHandle webSocketHandle,
                [In] BufferType bufferType,
                [In] ref Buffer buffer,
                [In] IntPtr applicationContext);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketSend", ExactSpelling = true)]
            private static extern int WebSocketSendWithoutBody_Raw(
                [In] SafeHandle webSocketHandle,
                [In] BufferType bufferType,
                [In] IntPtr buffer,
                [In] IntPtr applicationContext);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketReceive", ExactSpelling = true)]
            private static extern int WebSocketReceive_Raw(
                [In] SafeHandle webSocketHandle,
                [In] IntPtr buffers,
                [In] IntPtr applicationContext);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketGetAction", ExactSpelling = true)]
            private static extern int WebSocketGetAction_Raw(
                [In] SafeHandle webSocketHandle,
                [In] ActionQueue actionQueue,
                [In, Out] Buffer[] dataBuffers,
                [In, Out] ref uint dataBufferCount,
                [Out] out Action action,
                [Out] out BufferType bufferType,
                [Out] out IntPtr applicationContext,
                [Out] out IntPtr actionContext);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketCompleteAction", ExactSpelling = true)]
            private static extern void WebSocketCompleteAction_Raw(
                [In] SafeHandle webSocketHandle,
                [In] IntPtr actionContext,
                [In] uint bytesTransferred);

            [DllImport(WEBSOCKET, EntryPoint = "WebSocketGetGlobalProperty", ExactSpelling = true)]
            private static extern int WebSocketGetGlobalProperty_Raw(
                [In] PropertyType property,
                [In, Out] ref uint value,
                [In, Out] ref uint size);
        }
    }
}
