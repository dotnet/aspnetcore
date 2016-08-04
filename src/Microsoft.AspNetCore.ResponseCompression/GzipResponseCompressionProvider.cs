// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Compression;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// GZIP compression provider.
    /// </summary>
    public class GzipResponseCompressionProvider : IResponseCompressionProvider
    {
        private readonly CompressionLevel _level;

        /// <summary>
        /// Initialize a new <see cref="GzipResponseCompressionProvider"/>.
        /// </summary>
        /// <param name="level">The compression level.</param>
        public GzipResponseCompressionProvider(CompressionLevel level)
        {
            _level = level;
        }

        /// <inheritdoc />
        public string EncodingName { get; } = "gzip";

        /// <inheritdoc />
        public Stream CreateStream(Stream outputStream)
        {
            return new GZipStream(outputStream, _level, true);
        }
    }
}
