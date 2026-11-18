// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpSys.Internal;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys.Tests;

public class NativeRequestContextTests
{
    [Fact]
    public unsafe void GetChunks_MultipleChunks()
    {
        byte* data = stackalloc byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var output = new byte[16];
        Array.Fill(output, (byte)0xFF);

        var chunks = stackalloc HTTP_DATA_CHUNK[2];
        chunks[0] = default;
        chunks[0].DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
        chunks[0].Anonymous.FromMemory.pBuffer = data;
        chunks[0].Anonymous.FromMemory.BufferLength = 4;
        chunks[1] = default;
        chunks[1].DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
        chunks[1].Anonymous.FromMemory.pBuffer = data + 4;
        chunks[1].Anonymous.FromMemory.BufferLength = 8;

        var request = new HTTP_REQUEST_V1 { EntityChunkCount = 2, pEntityChunks = chunks };
        var nativeContext = new NativeRequestContext(&request, false);
        var dataChunkIndex = 0;
        var dataChunkOffset = 0u;

        var dataRead = nativeContext.GetChunks(ref dataChunkIndex, ref dataChunkOffset, output, 0, 6);

        Assert.Equal(6u, dataRead);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, output[..6]);
        Assert.All(output[6..], b => Assert.Equal(0xFF, b));
    }
}
