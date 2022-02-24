// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ResponseCompression;

namespace ResponseCompressionSample;

public class CustomCompressionProvider : ICompressionProvider
{
    public string EncodingName => "custom";

    public bool SupportsFlush => true;

    public Stream CreateStream(Stream outputStream)
    {
        // Create a custom compression stream wrapper here
        return outputStream;
    }
}
