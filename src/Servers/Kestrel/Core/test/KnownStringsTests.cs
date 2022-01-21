// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests;

public class KnownStringsTests
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

    const int MagicNumber = 0x0600000C;
    static readonly byte[] _invalidMethod1 = BitConverter.GetBytes((ulong)MagicNumber);
    static readonly byte[] _invalidMethod2 = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
    static readonly byte[] _invalidMethod3 = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    static readonly byte[] _invalidMethod4 = Encoding.ASCII.GetBytes("CONNECT_");
    static readonly byte[] _invalidMethod5 = Encoding.ASCII.GetBytes("DELETE_\0");
    static readonly byte[] _invalidMethod6 = Encoding.ASCII.GetBytes("GET_");
    static readonly byte[] _invalidMethod7 = Encoding.ASCII.GetBytes("HEAD_\0\0\0");
    static readonly byte[] _invalidMethod8 = Encoding.ASCII.GetBytes("PATCH_\0\0");
    static readonly byte[] _invalidMethod9 = Encoding.ASCII.GetBytes("POST_\0\0\0");
    static readonly byte[] _invalidMethod10 = Encoding.ASCII.GetBytes("PUT_\0\0\0\0");
    static readonly byte[] _invalidMethod11 = Encoding.ASCII.GetBytes("OPTIONS_");
    static readonly byte[] _invalidMethod12 = Encoding.ASCII.GetBytes("TRACE_\0\0");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object[] CreateTestDataEntry(byte[] methodData, int expectedMethod, int expectedLength, bool expectedResult)
    {
        return new object[] { methodData, expectedMethod, expectedLength, expectedResult };
    }

    private static readonly object[][] _testData = new object[][]
    {
            CreateTestDataEntry(_methodGet, (int)HttpMethod.Get, 3, true),
            CreateTestDataEntry(_methodPut, (int)HttpMethod.Put, 3, true),
            CreateTestDataEntry(_methodPost, (int)HttpMethod.Post, 4, true),
            CreateTestDataEntry(_methodHead, (int)HttpMethod.Head, 4, true),
            CreateTestDataEntry(_methodTrace, (int)HttpMethod.Trace, 5, true),
            CreateTestDataEntry(_methodPatch, (int)HttpMethod.Patch, 5, true),
            CreateTestDataEntry(_methodDelete, (int)HttpMethod.Delete, 6, true),
            CreateTestDataEntry(_methodConnect, (int)HttpMethod.Connect, 7, true),
            CreateTestDataEntry(_methodOptions, (int)HttpMethod.Options, 7, true),
            CreateTestDataEntry(_invalidMethod1, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod2, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod3, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod4, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod5, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod6, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod7, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod8, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod9, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod10, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod11, (int)HttpMethod.Custom, 0, false),
            CreateTestDataEntry(_invalidMethod12, (int)HttpMethod.Custom, 0, false),
    };

    public static IEnumerable<object[]> TestData => _testData;

    [Theory]
    [MemberData(nameof(TestData), MemberType = typeof(KnownStringsTests))]
    public void GetsKnownMethod(byte[] methodData, int intExpectedMethod, int expectedLength, bool expectedResult)
    {
        var expectedMethod = (HttpMethod)intExpectedMethod;
        var data = new ReadOnlySpan<byte>(methodData);

        var result = data.GetKnownMethod(out var method, out var length);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedMethod, method);
        Assert.Equal(expectedLength, length);
    }
}
