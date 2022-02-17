// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.RequestDecompression;

namespace RequestDecompressionSample;

public class CustomDecompressionProvider : IDecompressionProvider
{
    public string EncodingName => "custom";

    public Stream CreateStream(Stream outputStream)
    {
        // Create a custom decompression stream wrapper here.
        return outputStream;
    }
}
