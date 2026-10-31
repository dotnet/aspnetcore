// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Net.Http.Headers;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys.Tests;

public class RequestHeaderTests
{
    private const string CustomHeader1 = "X-Custom-1";
    private const string CustomHeader2 = "X-Custom-2";

    [Fact]
    public unsafe void RequestHeaders_PooledMemory_ReturnsKnownHeadersKeys()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetHostAndContentTypeHeaders(nativeMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.Equal(2, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
        Assert.Contains(HeaderNames.Host, requestHeaders.Keys);
        Assert.Contains(HeaderNames.ContentType, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_PinnedMemory_ReturnsKnownHeadersKeys()
    {
        var buffer = new byte[2048].AsMemory();
        using var handle = buffer.Pin();
        Span<byte> nativeMemory = buffer.Span;

        var requestStructure = new HTTP_REQUEST_V1();
        SetHostAndContentTypeHeaders(nativeMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var nativeContext = new NativeRequestContext((HTTP_REQUEST_V1*)handle.Pointer, false);
        var requestHeaders = new RequestHeaders(nativeContext);

        Assert.Equal(2, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
        Assert.Contains(HeaderNames.Host, requestHeaders.Keys);
        Assert.Contains(HeaderNames.ContentType, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_PooledMemory_ReturnsUnknownHeadersKeys()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1"), (CustomHeader2, null) });
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.Equal(2, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
        Assert.Contains(CustomHeader1, requestHeaders.Keys);
        Assert.Contains(CustomHeader2, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_PinnedMemory_ReturnsUnknownHeadersKeys()
    {
        var buffer = new byte[2048].AsMemory();
        using var handle = buffer.Pin();
        Span<byte> nativeMemory = buffer.Span;

        var requestStructure = new HTTP_REQUEST_V1();
        SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1"), (CustomHeader2, null) });
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var nativeContext = new NativeRequestContext((HTTP_REQUEST_V1*)handle.Pointer, false);
        var requestHeaders = new RequestHeaders(nativeContext);

        Assert.Equal(2, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
        Assert.Contains(CustomHeader1, requestHeaders.Keys);
        Assert.Contains(CustomHeader2, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_PooledMemory_DoesNotReturnInvalidKnownHeadersKeys()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetInvalidHostHeader(nativeMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        var result = requestHeaders.Count;
        Assert.Equal(0, result);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
    }

    [Fact]
    public unsafe void RequestHeaders_PooledMemory_DoesNotReturnInvalidUnknownHeadersKeys()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetInvalidUnknownHeaders(nativeMemory, ref requestStructure, new[] { CustomHeader1 });
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        var result = requestHeaders.Count;
        Assert.Equal(0, result);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
    }

    [Fact]
    public unsafe void RequestHeaders_PooledMemory_ReturnsKnownAndUnKnownHeadersKeys()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        var remainingMemory = SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1"), (CustomHeader2, null) });
        SetHostAndContentTypeHeaders(remainingMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.Equal(4, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
        Assert.Contains(HeaderNames.Host, requestHeaders.Keys);
        Assert.Contains(HeaderNames.ContentType, requestHeaders.Keys);
        Assert.Contains(CustomHeader1, requestHeaders.Keys);
        Assert.Contains(CustomHeader2, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_PinnedMemory_ReturnsKnownAndUnKnownHeadersKeys()
    {
        var buffer = new byte[2048].AsMemory();
        using var handle = buffer.Pin();
        Span<byte> nativeMemory = buffer.Span;

        var requestStructure = new HTTP_REQUEST_V1();
        var remainingMemory = SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1"), (CustomHeader2, null) });
        SetHostAndContentTypeHeaders(remainingMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var nativeContext = new NativeRequestContext((HTTP_REQUEST_V1*)handle.Pointer, false);
        var requestHeaders = new RequestHeaders(nativeContext);

        Assert.Equal(4, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.ToArray().Length);
        Assert.Contains(HeaderNames.Host, requestHeaders.Keys);
        Assert.Contains(HeaderNames.ContentType, requestHeaders.Keys);
        Assert.Contains(CustomHeader1, requestHeaders.Keys);
        Assert.Contains(CustomHeader2, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_RemoveUnknownHeader_DecreasesCount()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1"), (CustomHeader2, null) });
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.Equal(2, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);

        requestHeaders.Remove(CustomHeader1);

        var countAfterRemoval = requestHeaders.Count;
        Assert.Equal(1, countAfterRemoval);
    }

    [Fact]
    public unsafe void RequestHeaders_AddUnknownHeader_IncreasesCount()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1") });
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        var countBeforeAdd = requestHeaders.Count;
        Assert.Equal(1, countBeforeAdd);

        requestHeaders[CustomHeader2] = "2";

        var countAfterAdd = requestHeaders.Count;
        Assert.Equal(2, countAfterAdd);
    }

    [Fact]
    public unsafe void RequestHeaders_RemoveUnknownHeader_RemovesKey()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1"), (CustomHeader2, null) });
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.Contains(CustomHeader1, requestHeaders.Keys);
        Assert.Contains(CustomHeader2, requestHeaders.Keys);

        requestHeaders.Remove(CustomHeader1);

        Assert.DoesNotContain(CustomHeader1, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_AddUnknownHeader_AddsKey()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetUnknownHeaders(nativeMemory, ref requestStructure, new[] { (CustomHeader1, "1") });
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.DoesNotContain(CustomHeader2, requestHeaders.Keys);

        requestHeaders[CustomHeader2] = "2";

        Assert.Contains(CustomHeader2, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_RemoveKnownHeader_DecreasesCount()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetHostAndContentTypeHeaders(nativeMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.Equal(2, requestHeaders.Count);
        Assert.Equal(requestHeaders.Count, requestHeaders.Keys.Count);

        requestHeaders.Remove(HeaderNames.ContentType);

        var countAfterRemoval = requestHeaders.Count;
        Assert.Equal(1, countAfterRemoval);
    }

    [Fact]
    public unsafe void RequestHeaders_AddKnownHeader_IncreasesCount()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetHostAndContentTypeHeaders(nativeMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        var countBeforeAdd = requestHeaders.Count;
        Assert.Equal(2, countBeforeAdd);

        requestHeaders[HeaderNames.From] = "FromValue";

        var countAfterAdd = requestHeaders.Count;
        Assert.Equal(3, countAfterAdd);
    }

    [Fact]
    public unsafe void RequestHeaders_RemoveKnownHeader_RemovesKey()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetHostAndContentTypeHeaders(nativeMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.Contains(HeaderNames.Host, requestHeaders.Keys);
        Assert.Contains(HeaderNames.ContentType, requestHeaders.Keys);

        requestHeaders.Remove(HeaderNames.ContentType);

        Assert.DoesNotContain(HeaderNames.ContentType, requestHeaders.Keys);
    }

    [Fact]
    public unsafe void RequestHeaders_AddKnownHeader_AddsKey()
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HTTP_REQUEST_V1();
        SetHostAndContentTypeHeaders(nativeMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();

        Assert.DoesNotContain(HeaderNames.From, requestHeaders.Keys);

        requestHeaders[HeaderNames.From] = "FromValue";

        Assert.Contains(HeaderNames.From, requestHeaders.Keys);
    }

    private static unsafe Span<byte> SetHostAndContentTypeHeaders(Span<byte> nativeMemory, ref HTTP_REQUEST_V1 requestStructure)
    {
        // Writing localhost to Host header
        var dataDestination = nativeMemory.Slice(Marshal.SizeOf<HTTP_REQUEST_V1>());
        int length = Encoding.ASCII.GetBytes("localhost:5001", dataDestination);
        fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
        {
            requestStructure.Headers.KnownHeaders._28.pRawValue = (PCSTR)address;
            requestStructure.Headers.KnownHeaders._28.RawValueLength = (ushort)length;
        }

        // Writing application/json to Content-Type header
        dataDestination = dataDestination.Slice(length);
        length = Encoding.ASCII.GetBytes("application/json", dataDestination);
        fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
        {
            requestStructure.Headers.KnownHeaders._12.pRawValue = (PCSTR)address;
            requestStructure.Headers.KnownHeaders._12.RawValueLength = (ushort)length;
        }
        dataDestination = dataDestination.Slice(length);

        return dataDestination;
    }

    private static unsafe Span<byte> SetInvalidHostHeader(Span<byte> nativeMemory, ref HTTP_REQUEST_V1 requestStructure)
    {
        // Writing localhost to Host header
        var dataDestination = nativeMemory.Slice(Marshal.SizeOf<HTTP_REQUEST_V1>());
        int length = Encoding.ASCII.GetBytes("localhost:5001", dataDestination);
        fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
        {
            requestStructure.Headers.KnownHeaders._28.pRawValue = (PCSTR)address;

            // Set length to zero, to make it invalid.
            requestStructure.Headers.KnownHeaders._28.RawValueLength = 0;
        }
        dataDestination = dataDestination.Slice(length);

        return dataDestination;
    }

    /// <summary>
    /// Writes an array HTTP_UNKNOWN_HEADER and an array of header key-value pairs to nativeMemory. Pointers in the HTTP_UNKNOWN_HEADER structure points to the corresponding key-value pair.
    /// </summary>
    private static unsafe Span<byte> SetUnknownHeaders(Span<byte> nativeMemory, ref HTTP_REQUEST_V1 requestStructure, IReadOnlyCollection<(string Key, string Value)> headerNames)
    {
        var unknownHeaderStructureDestination = nativeMemory.Slice(Marshal.SizeOf<HTTP_REQUEST_V1>());
        fixed (byte* address = &MemoryMarshal.GetReference(unknownHeaderStructureDestination))
        {
            requestStructure.Headers.pUnknownHeaders = (HTTP_UNKNOWN_HEADER*)address;
        }
        requestStructure.Headers.UnknownHeaderCount += (ushort)headerNames.Count;

        var unknownHeadersSize = Marshal.SizeOf<HTTP_UNKNOWN_HEADER>();
        var dataDestination = unknownHeaderStructureDestination.Slice(unknownHeadersSize * headerNames.Count);
        foreach (var headerName in headerNames)
        {
            var unknownHeaderStructure = new HTTP_UNKNOWN_HEADER();
            int nameLength = Encoding.ASCII.GetBytes(headerName.Key, dataDestination);
            fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
            {
                unknownHeaderStructure.pName = (PCSTR)address;
                unknownHeaderStructure.NameLength = (ushort)nameLength;
            }
            dataDestination = dataDestination.Slice(nameLength);

            if (!string.IsNullOrEmpty(headerName.Value))
            {
                int valueLength = Encoding.ASCII.GetBytes(headerName.Value, dataDestination);
                fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
                {
                    unknownHeaderStructure.pRawValue = (PCSTR)address;
                    unknownHeaderStructure.RawValueLength = (ushort)valueLength;
                }
                dataDestination = dataDestination.Slice(nameLength);
            }
            MemoryMarshal.Write(unknownHeaderStructureDestination, in unknownHeaderStructure);
            unknownHeaderStructureDestination = unknownHeaderStructureDestination.Slice(unknownHeadersSize);
        }
        return dataDestination;
    }

    private static unsafe Span<byte> SetInvalidUnknownHeaders(Span<byte> nativeMemory, ref HTTP_REQUEST_V1 requestStructure, IReadOnlyCollection<string> headerNames)
    {
        var unknownHeaderStructureDestination = nativeMemory.Slice(Marshal.SizeOf<HTTP_REQUEST_V1>());
        fixed (byte* address = &MemoryMarshal.GetReference(unknownHeaderStructureDestination))
        {
            requestStructure.Headers.pUnknownHeaders = (HTTP_UNKNOWN_HEADER*)address;
        }

        // UnknownHeaderCount might be higher number to the headers that can be parsed out.
        requestStructure.Headers.UnknownHeaderCount += (ushort)headerNames.Count;

        var unknownHeadersSize = Marshal.SizeOf<HTTP_UNKNOWN_HEADER>();
        var dataDestination = unknownHeaderStructureDestination.Slice(unknownHeadersSize * headerNames.Count);
        foreach (var headerName in headerNames)
        {
            var unknownHeaderStructure = new HTTP_UNKNOWN_HEADER();
            int nameLength = Encoding.ASCII.GetBytes(headerName, dataDestination);
            fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
            {
                unknownHeaderStructure.pName = (PCSTR)address;

                // Set the length of the name to 0 to make it invalid.
                unknownHeaderStructure.NameLength = 0;
            }
            dataDestination = dataDestination.Slice(nameLength);

            MemoryMarshal.Write(unknownHeaderStructureDestination, in unknownHeaderStructure);
            unknownHeaderStructureDestination = unknownHeaderStructureDestination.Slice(unknownHeadersSize);
        }
        return dataDestination;
    }
}
