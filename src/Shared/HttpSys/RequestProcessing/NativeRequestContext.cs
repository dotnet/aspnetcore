// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Primitives;
using Windows.Win32;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.HttpSys.Internal;

#pragma warning disable CA1852 // Seal internal types
internal unsafe class NativeRequestContext : IDisposable
#pragma warning restore CA1852 // Seal internal types
{
    private const int AlignmentPadding = 8;
    private const int DefaultBufferSize = 4096 - AlignmentPadding;
    private IntPtr _originalBufferAddress;
    private readonly bool _useLatin1;
    private HTTP_REQUEST_V1* _nativeRequest;
    private readonly IMemoryOwner<byte>? _backingBuffer;
    private MemoryHandle _memoryHandle;
    private readonly int _bufferAlignment;
    private readonly bool _permanentlyPinned;
    private bool _disposed;
    private IReadOnlyDictionary<int, ReadOnlyMemory<byte>>? _requestInfo;

    [MemberNotNullWhen(false, nameof(_backingBuffer))]
    private bool PermanentlyPinned => _permanentlyPinned;

    // To be used by HttpSys
    internal NativeRequestContext(MemoryPool<byte> memoryPool, uint? bufferSize, ulong requestId, bool useLatin1)
    {
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
        _backingBuffer.Memory.Span.Clear();
        _memoryHandle = _backingBuffer.Memory.Pin();
        _nativeRequest = (HTTP_REQUEST_V1*)((long)_memoryHandle.Pointer + _bufferAlignment);

        RequestId = requestId;
        _useLatin1 = useLatin1;
    }

    // To be used by IIS Integration.
    internal NativeRequestContext(HTTP_REQUEST_V1* request, bool useLatin1)
    {
        _useLatin1 = useLatin1;
        _nativeRequest = request;
        _bufferAlignment = 0;
        _permanentlyPinned = true;
    }

    public IReadOnlyDictionary<int, ReadOnlyMemory<byte>> RequestInfo => _requestInfo ??= GetRequestInfo();

    public ReadOnlySpan<long> Timestamps
    {
        get
        {
            /*
                Below is the definition of the timing info structure we are accessing the memory for.
                ULONG is 32-bit and maps to int. ULONGLONG is 64-bit and maps to long.

                typedef struct _HTTP_REQUEST_TIMING_INFO
                {
                    ULONG RequestTimingCount;
                    ULONGLONG RequestTiming[HttpRequestTimingTypeMax];

                } HTTP_REQUEST_TIMING_INFO, *PHTTP_REQUEST_TIMING_INFO;
            */

            if (!RequestInfo.TryGetValue((int)HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeRequestTiming, out var timingInfo))
            {
                return ReadOnlySpan<long>.Empty;
            }

            var timingCount = MemoryMarshal.Read<int>(timingInfo.Span);

            // Note that even though RequestTimingCount is an int, the compiler enforces alignment of data in the struct which causes 4 bytes
            // of padding to be added after RequestTimingCount, so we need to skip 64-bits before we get to the start of the RequestTiming array
            return MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(timingInfo.Span[sizeof(long)..])),
                timingCount);
        }
    }

    internal HTTP_REQUEST_V1* NativeRequest
    {
        get
        {
            Debug.Assert(_nativeRequest != null || _backingBuffer == null, "native request accessed after ReleasePins().");
            return _nativeRequest;
        }
    }

    internal HTTP_REQUEST_V2* NativeRequestV2
    {
        get
        {
            Debug.Assert(_nativeRequest != null || _backingBuffer == null, "native request accessed after ReleasePins().");
            return (HTTP_REQUEST_V2*)_nativeRequest;
        }
    }

    internal ulong RequestId
    {
        get { return NativeRequest->RequestId; }
        set { NativeRequest->RequestId = value; }
    }

    internal ulong ConnectionId => NativeRequest->ConnectionId;

    internal ulong RawConnectionId => NativeRequest->RawConnectionId;

    internal HTTP_VERB VerbId => NativeRequest->Verb;

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

    internal bool IsHttp2 => (NativeRequest->Flags & PInvoke.HTTP_REQUEST_FLAG_HTTP2) != 0;

    internal bool IsHttp3 => (NativeRequest->Flags & PInvoke.HTTP_REQUEST_FLAG_HTTP3) != 0;

    // Assumes memory isn't pinned. Will fail if called by IIS.
    internal uint Size
    {
        get
        {
            Debug.Assert(_backingBuffer != null);
            return (uint)_backingBuffer.Memory.Length - AlignmentPadding;
        }
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
    }

    public bool TryGetTimestamp(HttpSysRequestTimingType timestampType, out long timestamp)
    {
        var index = (int)timestampType;
        var timestamps = Timestamps;
        if (index < timestamps.Length && timestamps[index] > 0)
        {
            timestamp = timestamps[index];
            return true;
        }

        timestamp = default;
        return false;
    }

    public bool TryGetElapsedTime(HttpSysRequestTimingType startingTimestampType, HttpSysRequestTimingType endingTimestampType, out TimeSpan elapsed)
    {
        if (TryGetTimestamp(startingTimestampType, out var startTimestamp) && TryGetTimestamp(endingTimestampType, out var endTimestamp))
        {
            elapsed = Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
            return true;
        }

        elapsed = default;
        return false;
    }

    public virtual void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Debug.Assert(_nativeRequest == null, "RequestContextBase::Dispose()|Dispose() called before ReleasePins().");
            _memoryHandle.Dispose();
            _backingBuffer?.Dispose();
        }
    }

    // These methods require the HTTP_REQUEST to still be pinned in its original location.

    internal string? GetVerb()
    {
        var verb = NativeRequest->Verb;
        Debug.Assert((int)HTTP_VERB.HttpVerbMaximum == HttpVerbs.Length);
        if (verb > HTTP_VERB.HttpVerbUnknown && verb < HTTP_VERB.HttpVerbMaximum)
        {
            return HttpVerbs[(int)verb];
        }
        else if (verb == HTTP_VERB.HttpVerbUnknown && !NativeRequest->pUnknownVerb.Equals(null))
        {
            // Never use Latin1 for the VERB
            return HeaderEncoding.GetString(NativeRequest->pUnknownVerb, NativeRequest->UnknownVerbLength, useLatin1: false);
        }

        return null;
    }

    // Maps HTTP_VERB to strings
    internal static readonly string?[] HttpVerbs =
    [
        null,
        "Unknown",
        "Invalid",
        HttpMethods.Options,
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Delete,
        HttpMethods.Trace,
        HttpMethods.Connect,
        "TRACK",
        "MOVE",
        "COPY",
        "PROPFIND",
        "PROPPATCH",
        "MKCOL",
        "LOCK",
        "UNLOCK",
        "SEARCH",
    ];

    internal string? GetRawUrl()
    {
        if (!NativeRequest->pRawUrl.Equals(null) && NativeRequest->RawUrlLength > 0)
        {
            return Marshal.PtrToStringAnsi((IntPtr)NativeRequest->pRawUrl.Value, NativeRequest->RawUrlLength);
        }
        return null;
    }

    internal Span<byte> GetRawUrlInBytes()
    {
        if (!NativeRequest->pRawUrl.Equals(null) && NativeRequest->RawUrlLength > 0)
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
        if (IsHttp3)
        {
            return HttpVersion.Version30;
        }
        if (IsHttp2)
        {
            return HttpVersion.Version20;
        }
        var major = NativeRequest->Version.MajorVersion;
        var minor = NativeRequest->Version.MinorVersion;
        if (major == 1 && minor == 1)
        {
            return HttpVersion.Version11;
        }
        else if (major == 1 && minor == 0)
        {
            return HttpVersion.Version10;
        }
        return new Version(major, minor);
    }

    internal bool CheckAuthenticated()
    {
        var requestInfo = NativeRequestV2->pRequestInfo;
        var infoCount = NativeRequestV2->RequestInfoCount;

        for (var i = 0; i < infoCount; i++)
        {
            var info = &requestInfo[i];
            if (info != null
                && info->InfoType == HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth)
            {
                var authInfo = (HTTP_REQUEST_AUTH_INFO*)info->pInfo;
                if (authInfo->AuthStatus == HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
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

        for (var i = 0; i < infoCount; i++)
        {
            var info = &requestInfo[i];
            if (info != null
                && info->InfoType == HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth)
            {
                var authInfo = (HTTP_REQUEST_AUTH_INFO*)info->pInfo;
                if (authInfo->AuthStatus == HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
                {
                    // Duplicates AccessToken
                    var identity = new WindowsIdentity(authInfo->AccessToken, GetAuthTypeFromRequest(authInfo->AuthType));

                    // Close the original
                    PInvoke.CloseHandle(authInfo->AccessToken);

                    return new WindowsPrincipal(identity);
                }
            }
        }

        return new WindowsPrincipal(WindowsIdentity.GetAnonymous()); // Anonymous / !IsAuthenticated
    }

    internal HTTP_SSL_PROTOCOL_INFO GetTlsHandshake()
    {
        var requestInfo = NativeRequestV2->pRequestInfo;
        var infoCount = NativeRequestV2->RequestInfoCount;

        for (var i = 0; i < infoCount; i++)
        {
            var info = &requestInfo[i];
            if (info != null
                && info->InfoType == HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeSslProtocol)
            {
                var authInfo = *(HTTP_SSL_PROTOCOL_INFO*)info->pInfo;
                SetSslProtocol(&authInfo);
                return authInfo;
            }
        }

        return default;
    }

    private static void SetSslProtocol(HTTP_SSL_PROTOCOL_INFO* protocolInfo)
    {
        var protocol = protocolInfo->Protocol;
        // The OS considers client and server TLS as different enum values. SslProtocols choose to combine those for some reason.
        // We need to fill in the client bits so the enum shows the expected protocol.
        // https://learn.microsoft.com/windows/desktop/api/schannel/ns-schannel-_secpkgcontext_connectioninfo
        // Compare to https://referencesource.microsoft.com/#System/net/System/Net/SecureProtocols/_SslState.cs,8905d1bf17729de3
#pragma warning disable CS0618 // Type or member is obsolete
        if ((protocol & (uint)SslProtocols.Ssl2) != 0)
        {
            protocol |= (uint)SslProtocols.Ssl2;
        }
        if ((protocol & (uint)SslProtocols.Ssl3) != 0)
        {
            protocol |= (uint)SslProtocols.Ssl3;
        }
#pragma warning restore CS0618 // Type or Prmember is obsolete
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        if ((protocol & (uint)SslProtocols.Tls) != 0)
        {
            protocol |= (uint)SslProtocols.Tls;
        }
        if ((protocol & (uint)SslProtocols.Tls11) != 0)
        {
            protocol |= (uint)SslProtocols.Tls11;
        }
#pragma warning restore SYSLIB0039
        if ((protocol & (uint)SslProtocols.Tls12) != 0)
        {
            protocol |= (uint)SslProtocols.Tls12;
        }
        if ((protocol & (uint)SslProtocols.Tls13) != 0)
        {
            protocol |= (uint)SslProtocols.Tls13;
        }

        protocolInfo->Protocol = protocol;
    }

    private static string GetAuthTypeFromRequest(HTTP_REQUEST_AUTH_TYPE input)
    {
        return input switch
        {
            HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeBasic => "Basic",
            HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNTLM => "NTLM",
            // case HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeDigest => "Digest";
            HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNegotiate => "Negotiate",
            HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeKerberos => "Kerberos",
            _ => throw new NotImplementedException(input.ToString()),
        };
    }

    internal bool HasKnownHeader(HttpSysRequestHeader header)
    {
        if (PermanentlyPinned)
        {
            return HasKnowHeaderHelper(header, 0, _nativeRequest);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V1*)(pMemoryBlob + _bufferAlignment);
                var fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                return HasKnowHeaderHelper(header, fixup, request);
            }
        }
    }

    private bool HasKnowHeaderHelper(HttpSysRequestHeader header, long fixup, HTTP_REQUEST_V1* request)
    {
        var headerIndex = (int)header;

        var pKnownHeader = request->Headers.KnownHeaders.AsSpan()[headerIndex];
        // For known headers, when header value is empty, RawValueLength will be 0 and
        // pRawValue will point to empty string ("\0")
        if (pKnownHeader.RawValueLength > 0)
        {
            return true;
        }

        return false;
    }

    // These methods are for accessing the request structure after it has been unpinned. They need to adjust addresses
    // in case GC has moved the original object.

    internal string? GetKnownHeader(HttpSysRequestHeader header)
    {
        if (PermanentlyPinned)
        {
            return GetKnowHeaderHelper(header, 0, _nativeRequest);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V1*)(pMemoryBlob + _bufferAlignment);
                var fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                return GetKnowHeaderHelper(header, fixup, request);
            }
        }
    }  

    private string? GetKnowHeaderHelper(HttpSysRequestHeader header, long fixup, HTTP_REQUEST_V1* request)
    {
        var headerIndex = (int)header;
        string? value = null;

        var pKnownHeader = request->Headers.KnownHeaders.AsSpan()[headerIndex];
        // For known headers, when header value is empty, RawValueLength will be 0 and
        // pRawValue will point to empty string ("\0")
        if (pKnownHeader.RawValueLength > 0)
        {
            value = HeaderEncoding.GetString((byte*)pKnownHeader.pRawValue + fixup, pKnownHeader.RawValueLength, _useLatin1);
        }

        return value;
    }

    internal void GetUnknownKeys(Span<string> destination)
    {
        if (PermanentlyPinned)
        {
            PopulateUnknownKeys(_nativeRequest, 0, destination);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V1*)(pMemoryBlob + _bufferAlignment);
                var fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                PopulateUnknownKeys(request, fixup, destination);
            }
        }
    }

    private void PopulateUnknownKeys(HTTP_REQUEST_V1* request, long fixup, Span<string> destination)
    {
        if (request->Headers.UnknownHeaderCount == 0)
        {
            return;
        }
        var pUnknownHeader = (HTTP_UNKNOWN_HEADER*)(fixup + (byte*)request->Headers.pUnknownHeaders);
        for (var index = 0; index < request->Headers.UnknownHeaderCount; index++)
        {
            if (!pUnknownHeader->pName.Equals(null) && pUnknownHeader->NameLength > 0)
            {
                var headerName = HeaderEncoding.GetString((byte*)pUnknownHeader->pName + fixup, pUnknownHeader->NameLength, _useLatin1);
                destination[index] = headerName;
            }
            pUnknownHeader++;
        }
    }

    internal int CountUnknownHeaders()
    {
        if (PermanentlyPinned)
        {
            return CountUnknownHeaders(_nativeRequest, 0);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V1*)(pMemoryBlob + _bufferAlignment);
                var fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                return CountUnknownHeaders(request, fixup);
            }
        }
    }

    private int CountUnknownHeaders(HTTP_REQUEST_V1* request, long fixup)
    {
        if (request->Headers.UnknownHeaderCount == 0)
        {
            return 0;
        }
        var count = 0;
        var pUnknownHeader = (HTTP_UNKNOWN_HEADER*)(fixup + (byte*)request->Headers.pUnknownHeaders);
        for (var index = 0; index < request->Headers.UnknownHeaderCount; index++)
        {
            // For unknown headers, when header value is empty, RawValueLength will be 0 and
            // pRawValue will be null.
            if (!pUnknownHeader->pName.Equals(null) && pUnknownHeader->NameLength > 0)
            {
                count++;
            }
            pUnknownHeader++;
        }
        return count;
    }

    internal void GetUnknownHeaders(IDictionary<string, StringValues> unknownHeaders)
    {
        if (PermanentlyPinned)
        {
            GetUnknownHeadersHelper(unknownHeaders, 0, _nativeRequest);
        }
        else
        {
            // Return value.
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V1*)(pMemoryBlob + _bufferAlignment);
                var fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                GetUnknownHeadersHelper(unknownHeaders, fixup, request);
            }
        }
    }

    private void GetUnknownHeadersHelper(IDictionary<string, StringValues> unknownHeaders, long fixup, HTTP_REQUEST_V1* request)
    {
        int index;

        // unknown headers
        if (request->Headers.UnknownHeaderCount != 0)
        {
            var pUnknownHeader = (HTTP_UNKNOWN_HEADER*)(fixup + (byte*)request->Headers.pUnknownHeaders);
            for (index = 0; index < request->Headers.UnknownHeaderCount; index++)
            {
                // For unknown headers, when header value is empty, RawValueLength will be 0 and
                // pRawValue will be null.
                if (!pUnknownHeader->pName.Equals(null) && pUnknownHeader->NameLength > 0)
                {
                    var headerName = HeaderEncoding.GetString((byte*)pUnknownHeader->pName + fixup, pUnknownHeader->NameLength, _useLatin1);
                    string headerValue;
                    if (!pUnknownHeader->pRawValue.Equals(null) && pUnknownHeader->RawValueLength > 0)
                    {
                        headerValue = HeaderEncoding.GetString((byte*)pUnknownHeader->pRawValue + fixup, pUnknownHeader->RawValueLength, _useLatin1);
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

    internal SocketAddress? GetRemoteEndPoint()
    {
        return GetEndPoint(localEndpoint: false);
    }

    internal SocketAddress? GetLocalEndPoint()
    {
        return GetEndPoint(localEndpoint: true);
    }

    private SocketAddress? GetEndPoint(bool localEndpoint)
    {
        if (PermanentlyPinned)
        {
            return GetEndPointHelper(localEndpoint, _nativeRequest, (byte*)0);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V1*)(pMemoryBlob + _bufferAlignment);
                return GetEndPointHelper(localEndpoint, request, pMemoryBlob);
            }
        }
    }

    private SocketAddress? GetEndPointHelper(bool localEndpoint, HTTP_REQUEST_V1* request, byte* pMemoryBlob)
    {
        var source = localEndpoint ? (byte*)request->Address.pLocalAddress : (byte*)request->Address.pRemoteAddress;

        if (source == null)
        {
            return null;
        }
        var address = (IntPtr)(pMemoryBlob + _bufferAlignment - (byte*)_originalBufferAddress + source);
        return SocketAddress.CopyOutAddress(address);
    }

    internal uint GetChunks(ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size)
    {
        // Return value.
        if (PermanentlyPinned)
        {
            return GetChunksHelper(ref dataChunkIndex, ref dataChunkOffset, buffer, offset, size, 0, _nativeRequest);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V1*)(pMemoryBlob + _bufferAlignment);
                var fixup = pMemoryBlob - (byte*)_originalBufferAddress;
                return GetChunksHelper(ref dataChunkIndex, ref dataChunkOffset, buffer, offset, size, fixup, request);
            }
        }
    }

    private uint GetChunksHelper(ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size, long fixup, HTTP_REQUEST_V1* request)
    {
        uint dataRead = 0;

        if (request->EntityChunkCount > 0 && dataChunkIndex < request->EntityChunkCount && dataChunkIndex != -1)
        {
            var pDataChunk = (HTTP_DATA_CHUNK*)(fixup + (byte*)&request->pEntityChunks[dataChunkIndex]);

            fixed (byte* pReadBuffer = buffer)
            {
                var pTo = &pReadBuffer[offset];

                while (dataChunkIndex < request->EntityChunkCount && dataRead < size)
                {
                    if (dataChunkOffset >= pDataChunk->Anonymous.FromMemory.BufferLength)
                    {
                        dataChunkOffset = 0;
                        dataChunkIndex++;
                        pDataChunk++;
                    }
                    else
                    {
                        var pFrom = (byte*)pDataChunk->Anonymous.FromMemory.pBuffer + dataChunkOffset + fixup;

                        var bytesToRead = pDataChunk->Anonymous.FromMemory.BufferLength - (uint)dataChunkOffset;
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
        if (PermanentlyPinned)
        {
            return GetRequestInfo((IntPtr)_nativeRequest, (HTTP_REQUEST_V2*)_nativeRequest);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V2*)(pMemoryBlob + _bufferAlignment);
                return GetRequestInfo(_originalBufferAddress, request);
            }
        }
    }

    private IReadOnlyDictionary<int, ReadOnlyMemory<byte>> GetRequestInfo(IntPtr baseAddress, HTTP_REQUEST_V2* nativeRequest)
    {
        var count = nativeRequest->RequestInfoCount;
        if (count == 0)
        {
            return ImmutableDictionary<int, ReadOnlyMemory<byte>>.Empty;
        }

        var info = new Dictionary<int, ReadOnlyMemory<byte>>(count);

        var fixup = (byte*)nativeRequest - (byte*)baseAddress;
        var pRequestInfo = (HTTP_REQUEST_INFO*)((byte*)nativeRequest->pRequestInfo + fixup);

        for (var i = 0; i < count; i++)
        {
            var requestInfo = pRequestInfo[i];

            var memory = PermanentlyPinned
                ? new PointerMemoryManager<byte>((byte*)requestInfo.pInfo, (int)requestInfo.InfoLength).Memory
                : _backingBuffer.Memory.Slice((int)((long)requestInfo.pInfo - (long)baseAddress), (int)requestInfo.InfoLength);

            info.Add((int)requestInfo.InfoType, memory);
        }

        return new ReadOnlyDictionary<int, ReadOnlyMemory<byte>>(info);
    }

    internal X509Certificate2? GetClientCertificate()
    {
        if (PermanentlyPinned)
        {
            return GetClientCertificate((IntPtr)_nativeRequest, (HTTP_REQUEST_V2*)_nativeRequest);
        }
        else
        {
            fixed (byte* pMemoryBlob = _backingBuffer.Memory.Span)
            {
                var request = (HTTP_REQUEST_V2*)(pMemoryBlob + _bufferAlignment);
                return GetClientCertificate(_originalBufferAddress, request);
            }
        }
    }

    // Throws CryptographicException
    private X509Certificate2? GetClientCertificate(IntPtr baseAddress, HTTP_REQUEST_V2* nativeRequest)
    {
        var request = nativeRequest->Base;
        var fixup = (byte*)nativeRequest - (byte*)baseAddress;
        if (request.pSslInfo == null)
        {
            return null;
        }

        var sslInfo = (HTTP_SSL_INFO*)((byte*)request.pSslInfo + fixup);
        if (sslInfo->SslClientCertNegotiated == 0 || sslInfo->pClientCertInfo == null)
        {
            return null;
        }

        var clientCertInfo = (HTTP_SSL_CLIENT_CERT_INFO*)((byte*)sslInfo->pClientCertInfo + fixup);
        if (clientCertInfo->pCertEncoded == null)
        {
            return null;
        }

        var clientCert = clientCertInfo->pCertEncoded + fixup;
        var certEncoded = new byte[clientCertInfo->CertEncodedSize];
        Marshal.Copy((IntPtr)clientCert, certEncoded, 0, certEncoded.Length);
        return new X509Certificate2(certEncoded);
    }

    // Copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Memory/PointerMemoryManager.cs
    private sealed unsafe class PointerMemoryManager<T> : MemoryManager<T> where T : struct
    {
        private readonly void* _pointer;
        private readonly int _length;

        internal PointerMemoryManager(void* pointer, int length)
        {
            _pointer = pointer;
            _length = length;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override Span<T> GetSpan()
        {
            return new Span<T>(_pointer, _length);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotSupportedException();
        }

        public override void Unpin()
        {
        }
    }
}
