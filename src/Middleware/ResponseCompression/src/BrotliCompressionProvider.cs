// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Brotli compression provider.
    /// </summary>
    public class BrotliCompressionProvider : ICompressionProvider
    {
        /// <summary>
        /// Creates a new instance of <see cref="BrotliCompressionProvider"/> with options.
        /// </summary>
        /// <param name="options"></param>
        public BrotliCompressionProvider(IOptions<BrotliCompressionProviderOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Options = options.Value;
        }

        private BrotliCompressionProviderOptions Options { get; }

        /// <inheritdoc />
        public string EncodingName { get; } = "br";

        /// <inheritdoc />
        public bool SupportsFlush { get; } = true;

        /// <inheritdoc />
        public Stream CreateStream(Stream outputStream)
        {
            return new BrotliStream(outputStream, Options.Level, leaveOpen: true);
        }
    }
}
