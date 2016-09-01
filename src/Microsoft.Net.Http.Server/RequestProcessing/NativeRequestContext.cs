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
// <copyright file="HttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Server
{
    internal unsafe class NativeRequestContext : IDisposable
    {
        private const int DefaultBufferSize = 4096;
        private const int AlignmentPadding = 8;
        private HttpApi.HTTP_REQUEST* _nativeRequest;
        private IntPtr _originalBufferAddress;
        private byte[] _backingBuffer;
        private int _bufferAlignment;
        private SafeNativeOverlapped _nativeOverlapped;
        private AsyncAcceptContext _acceptResult;

        internal NativeRequestContext(AsyncAcceptContext result)
        {
            _acceptResult = result;
            AllocateNativeRequest();
        }

        internal SafeNativeOverlapped NativeOverlapped => _nativeOverlapped;

        internal HttpApi.HTTP_REQUEST* NativeRequest
        {
            get
            {
                Debug.Assert(_nativeRequest != null || _backingBuffer == null, "native request accessed after ReleasePins().");
                return _nativeRequest;
            }
        }

        private HttpApi.HTTP_REQUEST_V2* NativeRequestV2
        {
            get
            {
                Debug.Assert(_nativeRequest != null || _backingBuffer == null, "native request accessed after ReleasePins().");
                return (HttpApi.HTTP_REQUEST_V2*)_nativeRequest;
            }
        }

        internal ulong RequestId
        {
            get { return NativeRequest->RequestId; }
            set { NativeRequest->RequestId = value; }
        }

        internal ulong ConnectionId => NativeRequest->ConnectionId;

        internal HttpApi.HTTP_VERB VerbId => NativeRequest->Verb;

        internal ulong UrlContext => NativeRequest->UrlContext;

        internal ushort UnknownHeaderCount => NativeRequest->Headers.UnknownHeaderCount;

        internal SslStatus SslStatus
        {
            get
            {
                return NativeRequest->pSslInfo == null ? SslStatus.Insecure :
                    NativeRequest->pSslInfo->SslClientCertNegotiated == 0 ? SslStatus.NoClientCert :
                    SslStatus.ClientCert;
            }
        }

        internal uint Size
        {
            get { return (uint)_backingBuffer.Length - AlignmentPadding; }
        }

        // ReleasePins() should be called exactly once.  It must be called before Dispose() is called, which means it must be called
        // before an object (Request) which closes the RequestContext on demand is returned to the application.
        internal void ReleasePins()
        {
            Debug.Assert(_nativeRequest != null || _backingBuffer == null, "RequestContextBase::ReleasePins()|ReleasePins() called twice.");
            _originalBufferAddress = (IntPtr)_nativeRequest;
            _nativeRequest = null;
            _nativeOverlapped?.Dispose();
            _nativeOverlapped = null;
        }

        public void Dispose()
        {
            Debug.Assert(_nativeRequest == null, "RequestContextBase::Dispose()|Dispose() called before ReleasePins().");
            _nativeOverlapped?.Dispose();
        }

        private void SetBuffer(int size)
        {
            Debug.Assert(size != 0, "unexpected size");

            _backingBuffer = new byte[size + AlignmentPadding];
        }

        private void AllocateNativeRequest(uint? size = null)
        {
            // We can't reuse overlapped objects
            _nativeOverlapped?.Dispose();

            uint newSize = size.HasValue ? size.Value : _backingBuffer == null ? DefaultBufferSize : Size;
            SetBuffer(checked((int)newSize));
            var boundHandle = _acceptResult.Server.RequestQueue.BoundHandle;
            _nativeOverlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(AsyncAcceptContext.IOCallback, _acceptResult, _backingBuffer));

            var requestAddress = Marshal.UnsafeAddrOfPinnedArrayElement(_backingBuffer, 0);

            // TODO:
            // Apparently the HttpReceiveHttpRequest memory alignment requirements for non - ARM processors
            // are different than for ARM processors. We have seen 4 - byte - aligned buffers allocated on
            // virtual x64/x86 machines which were accepted by HttpReceiveHttpRequest without errors. In
            // these cases the buffer alignment may cause reading values at invalid offset. Setting buffer
            // alignment to 0 for now.
            // 
            // _bufferAlignment = (int)(requestAddress.ToInt64() & 0x07);

            _bufferAlignment = 0;

            _nativeRequest = (HttpApi.HTTP_REQUEST*)(requestAddress + _bufferAlignment);
        }

        internal void Reset(ulong requestId = 0, uint? size = null)
        {
            Debug.Assert(_nativeRequest != null || _backingBuffer == null, "RequestContextBase::Dispose()|SetNativeRequest() called after ReleasePins().");
            AllocateNativeRequest(size);
            RequestId = requestId;
        }

        // These methods require the HTTP_REQUEST to still be pinned in its original location.

        internal string GetVerb()
        {
            var verb = NativeRequest->Verb;
            if (verb > HttpApi.HTTP_VERB.HttpVerbUnknown && verb < HttpApi.HTTP_VERB.HttpVerbMaximum)
            {
                return HttpApi.HttpVerbs[(int)verb];
            }
            else if (verb == HttpApi.HTTP_VERB.HttpVerbUnknown && NativeRequest->pUnknownVerb != null)
            {
                return HeaderEncoding.GetString(NativeRequest->pUnknownVerb, NativeRequest->UnknownVerbLength);
            }

            return null;
        }

        internal string GetRawUrl()
        {
            if (NativeRequest->pRawUrl != null && NativeRequest->RawUrlLength > 0)
            {
                return Marshal.PtrToStringAnsi((IntPtr)NativeRequest->pRawUrl, NativeRequest->RawUrlLength);
            }
            return null;
        }

        internal CookedUrl GetCookedUrl()
        {
            return new CookedUrl(NativeRequest->CookedUrl);
        }

        internal Version GetVersion()
        {
            var major = NativeRequest->Version.MajorVersion;
            var minor = NativeRequest->Version.MinorVersion;
            if (major == 1 && minor == 1)
            {
                return Constants.V1_1;
            }
            else if (major == 1 && minor == 0)
            {
                return Constants.V1_0;
            }
            return new Version(major, minor);
        }

        internal bool CheckAuthenticated()
        {
            var requestInfo = NativeRequestV2->pRequestInfo;
            var infoCount = NativeRequestV2->RequestInfoCount;

            for (int i = 0; i < infoCount; i++)
            {
                var info = &requestInfo[i];
                if (info != null
                    && info->InfoType == HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth
                    && info->pInfo->AuthStatus == HttpApi.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
                {
                    return true;
                }
            }
            return false;
        }

        internal ClaimsPrincipal GetUser()
        {
            var requestInfo = NativeRequestV2->pRequestInfo;
            var infoCount = NativeRequestV2->RequestInfoCount;

            for (int i = 0; i < infoCount; i++)
            {
                var info = &requestInfo[i];
                if (info != null
                    && info->InfoType == HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth
                    && info->pInfo->AuthStatus == HttpApi.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
                {
                    return new WindowsPrincipal(new WindowsIdentity(info->pInfo->AccessToken,
                        GetAuthTypeFromRequest(info->pInfo->AuthType).ToString()));
                }
            }
            return new ClaimsPrincipal(new ClaimsIdentity()); // Anonymous / !IsAuthenticated
        }

        private static AuthenticationSchemes GetAuthTypeFromRequest(HttpApi.HTTP_REQUEST_AUTH_TYPE input)
        {
            switch (input)
            {
                case HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeBasic:
                    return AuthenticationSchemes.Basic;
                // case HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeDigest:
                //  return AuthenticationSchemes.Digest;
                case HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNTLM:
                    return AuthenticationSchemes.NTLM;
                case HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNegotiate:
                    return AuthenticationSchemes.Negotiate;
                case HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeKerberos:
                    return AuthenticationSchemes.Kerberos;
                default:
                    throw new NotImplementedException(input.ToString());
            }
        }

        // These methods are for accessing the request structure after it has been unpinned. They need to adjust addresses
        // in case GC has moved the original object.

        internal string GetKnownHeader(HttpSysRequestHeader header)
        {
            fixed (byte* pMemoryBlob = _backingBuffer)
            {
                var request = (HttpApi.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                long fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                int headerIndex = (int)header;
                string value = null;

                HttpApi.HTTP_KNOWN_HEADER* pKnownHeader = (&request->Headers.KnownHeaders) + headerIndex;
                // For known headers, when header value is empty, RawValueLength will be 0 and
                // pRawValue will point to empty string ("\0")
                if (pKnownHeader->pRawValue != null)
                {
                    value = HeaderEncoding.GetString(pKnownHeader->pRawValue + fixup, pKnownHeader->RawValueLength);
                }

                return value;
            }
        }

        internal void GetUnknownHeaders(IDictionary<string, StringValues> unknownHeaders)
        {
            // Return value.
            fixed (byte* pMemoryBlob = _backingBuffer)
            {
                var request = (HttpApi.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                long fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                int index;

                // unknown headers
                if (request->Headers.UnknownHeaderCount != 0)
                {
                    var pUnknownHeader = (HttpApi.HTTP_UNKNOWN_HEADER*)(fixup + (byte*)request->Headers.pUnknownHeaders);
                    for (index = 0; index < request->Headers.UnknownHeaderCount; index++)
                    {
                        // For unknown headers, when header value is empty, RawValueLength will be 0 and
                        // pRawValue will be null.
                        if (pUnknownHeader->pName != null && pUnknownHeader->NameLength > 0)
                        {
                            var headerName = HeaderEncoding.GetString(pUnknownHeader->pName + fixup, pUnknownHeader->NameLength);
                            string headerValue;
                            if (pUnknownHeader->pRawValue != null && pUnknownHeader->RawValueLength > 0)
                            {
                                headerValue = HeaderEncoding.GetString(pUnknownHeader->pRawValue + fixup, pUnknownHeader->RawValueLength);
                            }
                            else
                            {
                                headerValue = string.Empty;
                            }
                            // Note that Http.Sys currently collapses all headers of the same name to a single coma separated string,
                            // so we can just call Set.
                            unknownHeaders[headerName] = headerValue;
                        }
                        pUnknownHeader++;
                    }
                }
            }
        }

        internal SocketAddress GetRemoteEndPoint()
        {
            return GetEndPoint(localEndpoint: false);
        }

        internal SocketAddress GetLocalEndPoint()
        {
            return GetEndPoint(localEndpoint: true);
        }

        private SocketAddress GetEndPoint(bool localEndpoint)
        {
            fixed (byte* pMemoryBlob = _backingBuffer)
            {
                var request = (HttpApi.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                var source = localEndpoint ? (byte*)request->Address.pLocalAddress : (byte*)request->Address.pRemoteAddress;

                if (source == null)
                {
                    return null;
                }
                var address = (IntPtr)(pMemoryBlob + _bufferAlignment - (byte*)_originalBufferAddress + source);
                return CopyOutAddress(address);
            }
        }

        private static SocketAddress CopyOutAddress(IntPtr address)
        {
            ushort addressFamily = *((ushort*)address);
            if (addressFamily == (ushort)AddressFamily.InterNetwork)
            {
                var v4address = new SocketAddress(AddressFamily.InterNetwork, SocketAddress.IPv4AddressSize);
                fixed (byte* pBuffer = v4address.Buffer)
                {
                    for (int index = 2; index < SocketAddress.IPv4AddressSize; index++)
                    {
                        pBuffer[index] = ((byte*)address)[index];
                    }
                }
                return v4address;
            }
            if (addressFamily == (ushort)AddressFamily.InterNetworkV6)
            {
                var v6address = new SocketAddress(AddressFamily.InterNetworkV6, SocketAddress.IPv6AddressSize);
                fixed (byte* pBuffer = v6address.Buffer)
                {
                    for (int index = 2; index < SocketAddress.IPv6AddressSize; index++)
                    {
                        pBuffer[index] = ((byte*)address)[index];
                    }
                }
                return v6address;
            }

            return null;
        }

        internal uint GetChunks(ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size)
        {
            // Return value.
            uint dataRead = 0;
            fixed (byte* pMemoryBlob = _backingBuffer)
            {
                var request = (HttpApi.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                long fixup = pMemoryBlob - (byte*)_originalBufferAddress;

                if (request->EntityChunkCount > 0 && dataChunkIndex < request->EntityChunkCount && dataChunkIndex != -1)
                {
                    var pDataChunk = (HttpApi.HTTP_DATA_CHUNK*)(fixup + (byte*)&request->pEntityChunks[dataChunkIndex]);

                    fixed (byte* pReadBuffer = buffer)
                    {
                        byte* pTo = &pReadBuffer[offset];

                        while (dataChunkIndex < request->EntityChunkCount && dataRead < size)
                        {
                            if (dataChunkOffset >= pDataChunk->fromMemory.BufferLength)
                            {
                                dataChunkOffset = 0;
                                dataChunkIndex++;
                                pDataChunk++;
                            }
                            else
                            {
                                byte* pFrom = (byte*)pDataChunk->fromMemory.pBuffer + dataChunkOffset + fixup;

                                uint bytesToRead = pDataChunk->fromMemory.BufferLength - (uint)dataChunkOffset;
                                if (bytesToRead > (uint)size)
                                {
                                    bytesToRead = (uint)size;
                                }
                                for (uint i = 0; i < bytesToRead; i++)
                                {
                                    *(pTo++) = *(pFrom++);
                                }
                                dataRead += bytesToRead;
                                dataChunkOffset += bytesToRead;
                            }
                        }
                    }
                }
                // we're finished.
                if (dataChunkIndex == request->EntityChunkCount)
                {
                    dataChunkIndex = -1;
                }
            }

            return dataRead;
        }
    }
}
