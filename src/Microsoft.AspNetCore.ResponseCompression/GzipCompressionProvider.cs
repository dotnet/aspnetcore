// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Compression;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// GZIP compression provider.
    /// </summary>
    public class GzipCompressionProvider : ICompressionProvider
    {
        /// <summary>
        /// Initialize a new <see cref="GzipCompressionProvider"/>.
        /// </summary>
        public GzipCompressionProvider()
        {
        }

        /// <inheritdoc />
        public string EncodingName => "gzip";

        /// <summary>
        /// What level of compression to use for the stream.
        /// </summary>
        public CompressionLevel Level { get; set; } = CompressionLevel.Fastest;

        /// <inheritdoc />
        public Stream CreateStream(Stream outputStream)
        {
            return new GZipStream(outputStream, Level, leaveOpen: true);
        }
    }
}
