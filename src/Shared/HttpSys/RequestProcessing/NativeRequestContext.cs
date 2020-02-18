// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal unsafe class NativeRequestContext : IDisposable
    {
        private const int AlignmentPadding = 8;
        private const int DefaultBufferSize = 4096 - AlignmentPadding;
        private IntPtr _originalBufferAddress;
        private HttpApiTypes.HTTP_REQUEST* _nativeRequest;
        private IMemoryOwner<byte> _backingBuffer;
        private MemoryHandle _memoryHandle;
        private int _bufferAlignment;
        private SafeNativeOverlapped _nativeOverlapped;
        private bool _permanentlyPinned;
        private bool _disposed;

        // To be used by HttpSys
        internal NativeRequestContext(SafeNativeOverlapped nativeOverlapped, MemoryPool<Byte> memoryPool, uint? bufferSize, ulong requestId)
        {
            _nativeOverlapped = nativeOverlapped;

            // TODO:
            // Apparently the HttpReceiveHttpRequest memory alignment requirements for non - ARM processors
            // are different than for ARM processors. We have seen 4 - byte - aligned buffers allocated on
            // virtual x64/x86 machines which were accepted by HttpReceiveHttpRequest without errors. In
            // these cases the buffer alignment may cause reading values at invalid offset. Setting buffer
            // alignment to 0 for now.
            // 
            // _bufferAlignment = (int)(requestAddress.ToInt64() & 0x07);
            _bufferAlignment = 0;

            var newSize = (int)(bufferSize ?? DefaultBufferSize) + AlignmentPadding;
            if (newSize <= memoryPool.MaxBufferSize)
            {
                _backingBuffer = memoryPool.Rent(newSize);
            }
            else
            {
                // No size limit
                _backingBuffer = MemoryPool<byte>.Shared.Rent(newSize);
            }
            _backingBuffer.Memory.Span.Fill(0);// Zero the buffer
            _memoryHandle = _backingBuffer.Memory.Pin();
            _nativeRequest = (HttpApiTypes.HTTP_REQUEST*)((long)_memoryHandle.Pointer + _bufferAlignment);

            RequestId = requestId;
        }

        // To be used by IIS Integration.
        internal NativeRequestContext(HttpApiTypes.HTTP_REQUEST* request)
        {
            _nativeRequest = request;
            _bufferAlignment = 0;
            _permanentlyPinned = true;
        }

        internal SafeNativeOverlapped NativeOverlapped => _nativeOverlapped;

        internal HttpApiTypes.HTTP_REQUEST* NativeRequest
        {
            get
            {
                Debug.Assert(_nativeRequest != null || _backingBuffer == null, "native request accessed after ReleasePins().");
                return _nativeRequest;
            }
        }

        internal HttpApiTypes.HTTP_REQUEST_V2* NativeRequestV2
        {
            get
            {
                Debug.Assert(_nativeRequest != null || _backingBuffer == null, "native request accessed after ReleasePins().");
                return (HttpApiTypes.HTTP_REQUEST_V2*)_nativeRequest;
            }
        }

        internal ulong RequestId
        {
            get { return NativeRequest->RequestId; }
            set { NativeRequest->RequestId = value; }
        }

        internal ulong ConnectionId => NativeRequest->ConnectionId;

        internal HttpApiTypes.HTTP_VERB VerbId => NativeRequest->Verb;

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

        internal bool IsHttp2 => NativeRequest->Flags.HasFlag(HttpApiTypes.HTTP_REQUEST_FLAGS.Http2);

        internal uint Size
        {
            get { return (uint)_backingBuffer.Memory.Length - AlignmentPadding; }
        }

        // ReleasePins() should be called exactly once.  It must be called before Dispose() is called, which means it must be called
        // before an object (Request) which closes the RequestContext on demand is returned to the application.
        internal void ReleasePins()
        {
            Debug.Assert(_nativeRequest != null, "RequestContextBase::ReleasePins()|ReleasePins() called twice.");
            _originalBufferAddress = (IntPtr)_nativeRequest;
            _memoryHandle.Dispose();
            _memoryHandle = default;
            _nativeRequest = null;
            _nativeOverlapped?.Dispose();
            _nativeOverlapped = null;
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Debug.Assert(_nativeRequest == null, "RequestContextBase::Dispose()|Dispose() called before ReleasePins().");
                _nativeOverlapped?.Dispose();
                _memoryHandle.Dispose();
                _backingBuffer.Dispose();
            }
        }

        // These methods require the HTTP_REQUEST to still be pinned in its original location.

        internal string GetVerb()
        {
            var verb = NativeRequest->Verb;
            if (verb > HttpApiTypes.HTTP_VERB.HttpVerbUnknown && verb < HttpApiTypes.HTTP_VERB.HttpVerbMaximum)
            {
                return HttpApiTypes.HttpVerbs[(int)verb];
            }
            else if (verb == HttpApiTypes.HTTP_VERB.HttpVerbUnknown && NativeRequest->pUnknownVerb != null)
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

        internal Span<byte> GetRawUrlInBytes()
        {
            if (NativeRequest->pRawUrl != null && NativeRequest->RawUrlLength > 0)
            {
                return new Span<byte>(NativeRequest->pRawUrl, NativeRequest->RawUrlLength);
            }

            return default;
        }

        internal CookedUrl GetCookedUrl()
        {
            return new CookedUrl(NativeRequest->CookedUrl);
        }

        internal Version GetVersion()
        {
            if (IsHttp2)
            {
                return Constants.V2;
            }
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
                    && info->InfoType == HttpApiTypes.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth)
                {
                    var authInfo = (HttpApiTypes.HTTP_REQUEST_AUTH_INFO*)info->pInfo;
                    if (authInfo->AuthStatus == HttpApiTypes.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal WindowsPrincipal GetUser()
        {
            var requestInfo = NativeRequestV2->pRequestInfo;
            var infoCount = NativeRequestV2->RequestInfoCount;

            for (int i = 0; i < infoCount; i++)
            {
                var info = &requestInfo[i];
                if (info != null
                    && info->InfoType == HttpApiTypes.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth)
                {
                    var authInfo = (HttpApiTypes.HTTP_REQUEST_AUTH_INFO*)info->pInfo;
                    if (authInfo->AuthStatus == HttpApiTypes.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
                    {
                        // Duplicates AccessToken
                        var identity = new WindowsIdentity(authInfo->AccessToken, GetAuthTypeFromRequest(authInfo->AuthType));

                        // Close the original
                        UnsafeNclNativeMethods.SafeNetHandles.CloseHandle(authInfo->AccessToken);

                        return new WindowsPrincipal(identity);
                    }
                }
            }

            return new WindowsPrincipal(WindowsIdentity.GetAnonymous()); // Anonymous / !IsAuthenticated
        }

        internal HttpApiTypes.HTTP_SSL_PROTOCOL_INFO GetTlsHandshake()
        {
            var requestInfo = NativeRequestV2->pRequestInfo;
            var infoCount = NativeRequestV2->RequestInfoCount;

            for (int i = 0; i < infoCount; i++)
            {
                var info = &requestInfo[i];
                if (info != null
                    && info->InfoType == HttpApiTypes.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeSslProtocol)
                {
                    var authInfo = (HttpApiTypes.HTTP_SSL_PROTOCOL_INFO*)info->pInfo;
                    return *authInfo;
                }
            }

            return default;
        }

        private static string GetAuthTypeFromRequest(HttpApiTypes.HTTP_REQUEST_AUTH_TYPE input)
        {
            switch (input)
            {
                case HttpApiTypes.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeBasic:
                    return "Basic";
                case HttpApiTypes.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNTLM:
                    return "NTLM";
                    // case HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeDigest:
                    //  return "Digest";
                case HttpApiTypes.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNegotiate:
                    return "Negotiate";
                case HttpApiTypes.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeKerberos:
                    return "Kerberos";
                default:
                    throw new NotImplementedException(input.ToString());
            }
        }

        // These methods are for accessing the request structure after it has been unpinned. They need to adjust addresses
        // in case GC has moved the original object.

        internal string GetKnownHeader(HttpSysRequestHeader header)
        {
            if (_permanentlyPinned)
            {
                return GetKnowHeaderHelper(header, 0, _nativeRequest);
            }
            else
            {
                fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
                {
                    var request = (HttpApiTypes.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                    long fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                    return GetKnowHeaderHelper(header, fixup, request);
                }
            }
        }

        private string GetKnowHeaderHelper(HttpSysRequestHeader header, long fixup, HttpApiTypes.HTTP_REQUEST* request)
        {
            int headerIndex = (int)header;
            string value = null;

            HttpApiTypes.HTTP_KNOWN_HEADER* pKnownHeader = (&request->Headers.KnownHeaders) + headerIndex;
            // For known headers, when header value is empty, RawValueLength will be 0 and
            // pRawValue will point to empty string ("\0")
            if (pKnownHeader->RawValueLength > 0)
            {
                value = HeaderEncoding.GetString(pKnownHeader->pRawValue + fixup, pKnownHeader->RawValueLength);
            }

            return value;
        }

        internal void GetUnknownHeaders(IDictionary<string, StringValues> unknownHeaders)
        {
            if (_permanentlyPinned)
            {
                GetUnknownHeadersHelper(unknownHeaders, 0, _nativeRequest);
            }
            else
            {
                // Return value.
                fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
                {
                    var request = (HttpApiTypes.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                    long fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                    GetUnknownHeadersHelper(unknownHeaders, fixup, request);
                }
            }
        }

        private void GetUnknownHeadersHelper(IDictionary<string, StringValues> unknownHeaders, long fixup, HttpApiTypes.HTTP_REQUEST* request)
        {
            int index;

            // unknown headers
            if (request->Headers.UnknownHeaderCount != 0)
            {
                var pUnknownHeader = (HttpApiTypes.HTTP_UNKNOWN_HEADER*)(fixup + (byte*)request->Headers.pUnknownHeaders);
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
            if (_permanentlyPinned)
            {
                return GetEndPointHelper(localEndpoint, _nativeRequest, (byte *)0);
            }
            else
            {
                fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
                {
                    var request = (HttpApiTypes.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                    return GetEndPointHelper(localEndpoint, request, pMemoryBlob);
                }
            }
        }

        private SocketAddress GetEndPointHelper(bool localEndpoint, HttpApiTypes.HTTP_REQUEST* request, byte* pMemoryBlob)
        {
            var source = localEndpoint ? (byte*)request->Address.pLocalAddress : (byte*)request->Address.pRemoteAddress;

            if (source == null)
            {
                return null;
            }
            var address = (IntPtr)(pMemoryBlob + _bufferAlignment - (byte*)_originalBufferAddress + source);
            return CopyOutAddress(address);
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
            if (_permanentlyPinned)
            {
                return GetChunksHelper(ref dataChunkIndex, ref dataChunkOffset, buffer, offset, size, 0, _nativeRequest);
            }
            else
            {
                fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
                {
                    var request = (HttpApiTypes.HTTP_REQUEST*)(pMemoryBlob + _bufferAlignment);
                    long fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                    return GetChunksHelper(ref dataChunkIndex, ref dataChunkOffset, buffer, offset, size, fixup, request);
                }
            }
        }

        private uint GetChunksHelper(ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size, long fixup, HttpApiTypes.HTTP_REQUEST* request)
        {
            uint dataRead = 0;

            if (request->EntityChunkCount > 0 && dataChunkIndex < request->EntityChunkCount && dataChunkIndex != -1)
            {
                var pDataChunk = (HttpApiTypes.HTTP_DATA_CHUNK*)(fixup + (byte*)&request->pEntityChunks[dataChunkIndex]);

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
            return dataRead;
        }

        internal IReadOnlyDictionary<int, ReadOnlyMemory<byte>> GetRequestInfo()
        {
            if (_permanentlyPinned)
            {
                return GetRequestInfo((IntPtr)_nativeRequest, (HttpApiTypes.HTTP_REQUEST_V2*)_nativeRequest);
            }
            else
            {
                fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
                {
                    var request = (HttpApiTypes.HTTP_REQUEST_V2*)(pMemoryBlob + _bufferAlignment);
                    return GetRequestInfo(_originalBufferAddress, request);
                }
            }
        }

        private IReadOnlyDictionary<int, ReadOnlyMemory<byte>> GetRequestInfo(IntPtr baseAddress, HttpApiTypes.HTTP_REQUEST_V2* nativeRequest)
        {
            var count = nativeRequest->RequestInfoCount;
            if (count == 0)
            {
                return ImmutableDictionary<int, ReadOnlyMemory<byte>>.Empty;
            }

            var info = new Dictionary<int, ReadOnlyMemory<byte>>(count);

            for (var i = 0; i < count; i++)
            {
                var requestInfo = nativeRequest->pRequestInfo[i];
                var offset = (long)requestInfo.pInfo - (long)baseAddress;
                info.Add(
                    (int)requestInfo.InfoType,
                    _backingBuffer.Memory.Slice((int)offset, (int)requestInfo.InfoLength));
            }

            return new ReadOnlyDictionary<int, ReadOnlyMemory<byte>>(info);
        }

        internal X509Certificate2 GetClientCertificate()
        {
            if (_permanentlyPinned)
            {
                return GetClientCertificate((IntPtr)_nativeRequest, (HttpApiTypes.HTTP_REQUEST_V2*)_nativeRequest);
            }
            else
            {
                fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
                {
                    var request = (HttpApiTypes.HTTP_REQUEST_V2*)(pMemoryBlob + _bufferAlignment);
                    return GetClientCertificate(_originalBufferAddress, request);
                }
            }
        }

        // Throws CryptographicException
        private X509Certificate2 GetClientCertificate(IntPtr baseAddress, HttpApiTypes.HTTP_REQUEST_V2* nativeRequest)
        {
            var request = nativeRequest->Request;
            long fixup = (byte*)nativeRequest - (byte*)baseAddress;
            if (request.pSslInfo == null)
            {
                return null;
            }

            var sslInfo = (HttpApiTypes.HTTP_SSL_INFO*)((byte*)request.pSslInfo + fixup);
            if (sslInfo->SslClientCertNegotiated == 0 || sslInfo->pClientCertInfo == null)
            {
                return null;
            }

            var clientCertInfo = (HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO*)((byte*)sslInfo->pClientCertInfo + fixup);
            if (clientCertInfo->pCertEncoded == null)
            {
                return null;
            }

            var clientCert = clientCertInfo->pCertEncoded + fixup;
            byte[] certEncoded = new byte[clientCertInfo->CertEncodedSize];
            Marshal.Copy((IntPtr)clientCert, certEncoded, 0, certEncoded.Length);
            return new X509Certificate2(certEncoded);
        }
    }
}
