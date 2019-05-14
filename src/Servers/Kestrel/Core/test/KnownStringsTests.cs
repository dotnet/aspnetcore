// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class KnownStringsTests
    {
        static byte[] _methodConnect = Encoding.ASCII.GetBytes("CONNECT ");
        static byte[] _methodDelete = Encoding.ASCII.GetBytes("DELETE \0");
        static byte[] _methodGet = Encoding.ASCII.GetBytes("GET ");
        static byte[] _methodHead = Encoding.ASCII.GetBytes("HEAD \0\0\0");
        static byte[] _methodPatch = Encoding.ASCII.GetBytes("PATCH \0\0");
        static byte[] _methodPost = Encoding.ASCII.GetBytes("POST \0\0\0");
        static byte[] _methodPut = Encoding.ASCII.GetBytes("PUT \0\0\0\0");
        static byte[] _methodOptions = Encoding.ASCII.GetBytes("OPTIONS ");
        static byte[] _methodTrace = Encoding.ASCII.GetBytes("TRACE \0\0");

        const int MagicNumber = 0x0600000C;
        static byte[] _invalidMethod1 = BitConverter.GetBytes((ulong)MagicNumber);
        static byte[] _invalidMethod2 = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        static byte[] _invalidMethod3 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        static byte[] _invalidMethod4 = Encoding.ASCII.GetBytes("CONNECT_");
        static byte[] _invalidMethod5 = Encoding.ASCII.GetBytes("DELETE_\0");
        static byte[] _invalidMethod6 = Encoding.ASCII.GetBytes("GET_");
        static byte[] _invalidMethod7 = Encoding.ASCII.GetBytes("HEAD_\0\0\0");
        static byte[] _invalidMethod8 = Encoding.ASCII.GetBytes("PATCH_\0\0");
        static byte[] _invalidMethod9 = Encoding.ASCII.GetBytes("POST_\0\0\0");
        static byte[] _invalidMethod10 = Encoding.ASCII.GetBytes("PUT_\0\0\0\0");
        static byte[] _invalidMethod11 = Encoding.ASCII.GetBytes("OPTIONS_");
        static byte[] _invalidMethod12 = Encoding.ASCII.GetBytes("TRACE_\0\0");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object[] CreateTestDataEntry(byte[] methodData, HttpMethod expectedMethod, int expectedLength, bool expectedResult)
        {
            return new object[] { methodData, expectedMethod, expectedLength, expectedResult };
        }

        private static readonly object[][] _testData = new object[][]
        {
            CreateTestDataEntry(_methodGet, HttpMethod.Get, 3, true),
            CreateTestDataEntry(_methodPut, HttpMethod.Put, 3, true),
            CreateTestDataEntry(_methodPost, HttpMethod.Post, 4, true),
            CreateTestDataEntry(_methodHead, HttpMethod.Head, 4, true),
            CreateTestDataEntry(_methodTrace, HttpMethod.Trace, 5, true),
            CreateTestDataEntry(_methodPatch, HttpMethod.Patch, 5, true),
            CreateTestDataEntry(_methodDelete, HttpMethod.Delete, 6, true),
            CreateTestDataEntry(_methodConnect, HttpMethod.Connect, 7, true),
            CreateTestDataEntry(_methodOptions, HttpMethod.Options, 7, true),
            CreateTestDataEntry(_invalidMethod1, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod2, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod3, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod4, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod5, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod6, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod7, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod8, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod9, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod10, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod11, HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod12, HttpMethod.Custom, 0, false),
        };

        public static IEnumerable<object[]> TestData => _testData;

        [Theory]
        [MemberData(nameof(TestData), MemberType = typeof(KnownStringsTests))]
        public void GetsKnownMethod(byte[] methodData, HttpMethod expectedMethod, int expectedLength, bool expectedResult)
        {
            var data = new Span<byte>(methodData);

            var result = data.GetKnownMethod(out var method, out var length);

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedMethod, method);
            Assert.Equal(expectedLength, length);
        }
    }
}
