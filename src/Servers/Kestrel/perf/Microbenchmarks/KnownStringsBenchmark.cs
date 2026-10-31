// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class KnownStringsBenchmark
{
    static readonly byte[] _methodConnect = Encoding.ASCII.GetBytes("CONNECT ");
    static readonly byte[] _methodDelete = Encoding.ASCII.GetBytes("DELETE \0");
    static readonly byte[] _methodGet = Encoding.ASCII.GetBytes("GET ");
    static readonly byte[] _methodHead = Encoding.ASCII.GetBytes("HEAD \0\0\0");
    static readonly byte[] _methodPatch = Encoding.ASCII.GetBytes("PATCH \0\0");
    static readonly byte[] _methodPost = Encoding.ASCII.GetBytes("POST \0\0\0");
    static readonly byte[] _methodPut = Encoding.ASCII.GetBytes("PUT \0\0\0\0");
    static readonly byte[] _methodOptions = Encoding.ASCII.GetBytes("OPTIONS ");
    static readonly byte[] _methodTrace = Encoding.ASCII.GetBytes("TRACE \0\0");

    static readonly byte[] _version = Encoding.UTF8.GetBytes("HTTP/1.1\r\n");
    const int loops = 1000;

    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_GET()
    {
        Span<byte> data = _methodGet;

        return GetKnownMethod(data);
    }

    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_CONNECT()
    {
        Span<byte> data = _methodConnect;

        return GetKnownMethod(data);
    }

    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_DELETE()
    {
        Span<byte> data = _methodDelete;

        return GetKnownMethod(data);
    }
    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_HEAD()
    {
        Span<byte> data = _methodHead;

        return GetKnownMethod(data);
    }

    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_PATCH()
    {
        Span<byte> data = _methodPatch;

        return GetKnownMethod(data);
    }
    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_POST()
    {
        Span<byte> data = _methodPost;

        return GetKnownMethod(data);
    }
    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_PUT()
    {
        Span<byte> data = _methodPut;

        return GetKnownMethod(data);
    }

    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_OPTIONS()
    {
        Span<byte> data = _methodOptions;

        return GetKnownMethod(data);
    }

    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownMethod_TRACE()
    {
        Span<byte> data = _methodTrace;

        return GetKnownMethod(data);
    }

    private int GetKnownMethod(ReadOnlySpan<byte> data)
    {
        int len = 0;
        HttpMethod method;

        for (int i = 0; i < loops; i++)
        {
            data.GetKnownMethod(out method, out var length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
            data.GetKnownMethod(out method, out length);
            len += length;
        }
        return len;
    }

    [Benchmark(OperationsPerInvoke = loops * 10)]
    public int GetKnownVersion_HTTP1_1()
    {
        int len = 0;
        HttpVersion version;
        ReadOnlySpan<byte> data = _version;
        for (int i = 0; i < loops; i++)
        {
            data.GetKnownVersion(out version, out var length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
            data.GetKnownVersion(out version, out length);
            len += length;
        }
        return len;
    }
}
