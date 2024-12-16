﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// GZIP compression provider.
    /// </summary>
    public class GzipCompressionProvider : ICompressionProvider
    {
        /// <summary>
        /// Creates a new instance of GzipCompressionProvider with options.
        /// </summary>
        /// <param name="options"></param>
        public GzipCompressionProvider(IOptions<GzipCompressionProviderOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Options = options.Value;
        }

        private GzipCompressionProviderOptions Options { get; }

        /// <inheritdoc />
        public string EncodingName => "gzip";

        /// <inheritdoc />
        public bool SupportsFlush
        {
            get
            {
#if NET462
                return false;
#elif NETSTANDARD2_0
                return true;
#else
#error target frameworks need to be updated
#endif
            }
        }

        /// <inheritdoc />
        public Stream CreateStream(Stream outputStream)
        {
            return new GZipStream(outputStream, Options.Level, leaveOpen: true);
        }
    }
}
