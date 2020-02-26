// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.ResponseCompression;

namespace ResponseCompressionSample
{
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
}
