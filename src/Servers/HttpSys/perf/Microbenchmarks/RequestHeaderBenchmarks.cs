// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.HttpSys.Internal;
using RequestHeaders = Microsoft.AspNetCore.HttpSys.Internal.RequestHeaders;

[SimpleJob, MemoryDiagnoser]
public class RequestHeaderBenchmarks
{
    private RequestHeaders _largeRequestHeaders;
    private RequestHeaders _smallRequestHeaders;

    [GlobalSetup]
    public unsafe void Setup()
    {
        _largeRequestHeaders = CreateRequestHeader(49);
        _smallRequestHeaders = CreateRequestHeader(0);
    }

    [Benchmark]
    public int CountSingleHeader()
    {
        _smallRequestHeaders.ResetFlags();
        return _smallRequestHeaders.Count;
    }

    [Benchmark]
    public int CountLargeHeaders()
    {
        _largeRequestHeaders.ResetFlags();
        return _largeRequestHeaders.Count;
    }

    [Benchmark]
    public ICollection<string> KeysSingleHeader()
    {
        _smallRequestHeaders.ResetFlags();
        return _smallRequestHeaders.Keys;
    }

    [Benchmark]
    public ICollection<string> KeysLargeHeaders()
    {
        _largeRequestHeaders.ResetFlags();
        return _largeRequestHeaders.Keys;
    }

    private unsafe RequestHeaders CreateRequestHeader(int unknowHeaderCount)
    {
        var nativeContext = new NativeRequestContext(MemoryPool<byte>.Shared, null, 0, false);
        var nativeMemory = new Span<byte>(nativeContext.NativeRequest, (int)nativeContext.Size + 8);

        var requestStructure = new HttpApiTypes.HTTP_REQUEST();
        var remainingMemory = SetUnknownHeaders(nativeMemory, ref requestStructure, GenerateUnknownHeaders(unknowHeaderCount));
        SetHostHeader(remainingMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, ref requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();
        return requestHeaders;
    }

    private unsafe Span<byte> SetHostHeader(Span<byte> nativeMemory, ref HttpApiTypes.HTTP_REQUEST requestStructure)
    {
        // Writing localhost to Host header
        var dataDestination = nativeMemory.Slice(Marshal.SizeOf<HttpApiTypes.HTTP_REQUEST>());
        int length = Encoding.ASCII.GetBytes("localhost:5001", dataDestination);
        fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
        {
            requestStructure.Headers.KnownHeaders_29.pRawValue = address;
            requestStructure.Headers.KnownHeaders_29.RawValueLength = (ushort)length;
        }
        return dataDestination;
    }

    /// <summary>
    /// Writes an array HTTP_UNKNOWN_HEADER and an array of header key-value pairs to nativeMemory. Pointers in the HTTP_UNKNOWN_HEADER structure points to the corresponding key-value pair.
    /// </summary>
    private unsafe Span<byte> SetUnknownHeaders(Span<byte> nativeMemory, ref HttpApiTypes.HTTP_REQUEST requestStructure, IReadOnlyCollection<(string Key, string Value)> headerNames)
    {
        var unknownHeaderStructureDestination = nativeMemory.Slice(Marshal.SizeOf<HttpApiTypes.HTTP_REQUEST>());
        fixed (byte* address = &MemoryMarshal.GetReference(unknownHeaderStructureDestination))
        {
            requestStructure.Headers.pUnknownHeaders = (HttpApiTypes.HTTP_UNKNOWN_HEADER*)address;
        }
        requestStructure.Headers.UnknownHeaderCount += (ushort)headerNames.Count;

        var unknownHeadersSize = Marshal.SizeOf<HttpApiTypes.HTTP_UNKNOWN_HEADER>();
        var dataDestination = unknownHeaderStructureDestination.Slice(unknownHeadersSize * headerNames.Count);
        foreach (var headerName in headerNames)
        {
            var unknownHeaderStructure = new HttpApiTypes.HTTP_UNKNOWN_HEADER();
            int nameLength = Encoding.ASCII.GetBytes(headerName.Key, dataDestination);
            fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
            {
                unknownHeaderStructure.pName = address;
                unknownHeaderStructure.NameLength = (ushort)nameLength;
            }
            dataDestination = dataDestination.Slice(nameLength);

            if (!string.IsNullOrEmpty(headerName.Value))
            {
                int valueLength = Encoding.ASCII.GetBytes(headerName.Value, dataDestination);
                fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
                {
                    unknownHeaderStructure.pRawValue = address;
                    unknownHeaderStructure.RawValueLength = (ushort)valueLength;
                }
                dataDestination = dataDestination.Slice(nameLength);
            }
            MemoryMarshal.Write(unknownHeaderStructureDestination, ref unknownHeaderStructure);
            unknownHeaderStructureDestination = unknownHeaderStructureDestination.Slice(unknownHeadersSize);
        }
        return dataDestination;
    }

    private IReadOnlyCollection<(string, string)> GenerateUnknownHeaders(int count)
    {
        var result = new List<(string, string)>();
        for (int i = 0; i < count; i++)
        {
            result.Add(($"X-Custom-{i}", $"Value-{i}"));
        }
        return result;
    }
}
