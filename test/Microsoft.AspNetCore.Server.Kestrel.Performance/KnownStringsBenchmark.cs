// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class KnownStringsBenchmark
    {
        static byte[] _method = Encoding.UTF8.GetBytes("GET ");
        static byte[] _version = Encoding.UTF8.GetBytes("HTTP/1.1\r\n");
        const int loops = 1000;

        [Benchmark(OperationsPerInvoke = loops * 10)]
        public int GetKnownMethod_GET()
        {
            int len = 0;
            HttpMethod method;
            Span<byte> data = _method;
            for (int i = 0; i < loops; i++) {
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
            Span<byte> data = _version;
            for (int i = 0; i < loops; i++) {
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
}
