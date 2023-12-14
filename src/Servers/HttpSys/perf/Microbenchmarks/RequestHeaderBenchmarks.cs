// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.HttpSys.Internal;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.HttpServer;
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

        var requestStructure = new HTTP_REQUEST_V1();
        var remainingMemory = SetUnknownHeaders(nativeMemory, ref requestStructure, GenerateUnknownHeaders(unknowHeaderCount));
        SetHostHeader(remainingMemory, ref requestStructure);
        MemoryMarshal.Write(nativeMemory, in requestStructure);

        var requestHeaders = new RequestHeaders(nativeContext);
        nativeContext.ReleasePins();
        return requestHeaders;
    }

    private unsafe Span<byte> SetHostHeader(Span<byte> nativeMemory, ref HTTP_REQUEST_V1 requestStructure)
    {
        // Writing localhost to Host header
        var dataDestination = nativeMemory[Marshal.SizeOf<HTTP_REQUEST_V1>()..];
        var length = Encoding.ASCII.GetBytes("localhost:5001", dataDestination);
        fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
        {
            requestStructure.Headers.KnownHeaders._28.pRawValue = (PCSTR)address;
            requestStructure.Headers.KnownHeaders._28.RawValueLength = (ushort)length;
        }
        return dataDestination;
    }

    /// <summary>
    /// Writes an array HTTP_UNKNOWN_HEADER and an array of header key-value pairs to nativeMemory. Pointers in the HTTP_UNKNOWN_HEADER structure points to the corresponding key-value pair.
    /// </summary>
    private unsafe Span<byte> SetUnknownHeaders(Span<byte> nativeMemory, ref HTTP_REQUEST_V1 requestStructure, IReadOnlyCollection<(string Key, string Value)> headerNames)
    {
        var unknownHeaderStructureDestination = nativeMemory[Marshal.SizeOf<HTTP_REQUEST_V1>()..];
        fixed (byte* address = &MemoryMarshal.GetReference(unknownHeaderStructureDestination))
        {
            requestStructure.Headers.pUnknownHeaders = (HTTP_UNKNOWN_HEADER*)address;
        }
        requestStructure.Headers.UnknownHeaderCount += (ushort)headerNames.Count;

        var unknownHeadersSize = Marshal.SizeOf<HTTP_UNKNOWN_HEADER>();
        var dataDestination = unknownHeaderStructureDestination[(unknownHeadersSize * headerNames.Count)..];
        foreach (var (headerKey, headerValue) in headerNames)
        {
            var unknownHeaderStructure = new HTTP_UNKNOWN_HEADER();
            var nameLength = Encoding.ASCII.GetBytes(headerKey, dataDestination);
            fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
            {
                unknownHeaderStructure.pName = (PCSTR)address;
                unknownHeaderStructure.NameLength = (ushort)nameLength;
            }
            dataDestination = dataDestination[nameLength..];

            if (!string.IsNullOrEmpty(headerValue))
            {
                var valueLength = Encoding.ASCII.GetBytes(headerValue, dataDestination);
                fixed (byte* address = &MemoryMarshal.GetReference(dataDestination))
                {
                    unknownHeaderStructure.pRawValue = (PCSTR)address;
                    unknownHeaderStructure.RawValueLength = (ushort)valueLength;
                }
                dataDestination = dataDestination[nameLength..];
            }
            MemoryMarshal.Write(unknownHeaderStructureDestination, in unknownHeaderStructure);
            unknownHeaderStructureDestination = unknownHeaderStructureDestination[unknownHeadersSize..];
        }
        return dataDestination;
    }

    private static List<(string, string)> GenerateUnknownHeaders(int count)
    {
        var result = new List<(string, string)>();
        for (var i = 0; i < count; i++)
        {
            result.Add(($"X-Custom-{i}", $"Value-{i}"));
        }
        return result;
    }
}
