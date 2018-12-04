// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Options for the GzipCompressionProvider
    /// </summary>
    public class GzipCompressionProviderOptions : IOptions<GzipCompressionProviderOptions>
    {
        /// <summary>
        /// What level of compression to use for the stream. The default is Fastest.
        /// </summary>
        public CompressionLevel Level { get; set; } = CompressionLevel.Fastest;

        /// <inheritdoc />
        GzipCompressionProviderOptions IOptions<GzipCompressionProviderOptions>.Value => this;
    }
}
