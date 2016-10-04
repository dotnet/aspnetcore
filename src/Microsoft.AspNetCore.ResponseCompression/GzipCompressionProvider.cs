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
        /// <inheritdoc />
        public string EncodingName => "gzip";

        /// <inheritdoc />
        public bool SupportsFlush
        {
            get
            {
#if NET451
                return false;
#elif NETSTANDARD1_3
                return true;
#else
                // Not implemented, compiler break
#endif
            }
        }

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
