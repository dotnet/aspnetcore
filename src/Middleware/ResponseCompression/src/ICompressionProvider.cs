// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Provides a specific compression implementation to compress HTTP responses.
    /// </summary>
    public interface ICompressionProvider
    {
        /// <summary>
        /// The encoding name used in the 'Accept-Encoding' request header and 'Content-Encoding' response header.
        /// </summary>
        string EncodingName { get; }

        /// <summary>
        /// Indicates if the given provider supports Flush and FlushAsync. If not, compression may be disabled in some scenarios.
        /// </summary>
        bool SupportsFlush { get; }

        /// <summary>
        /// Create a new compression stream.
        /// </summary>
        /// <param name="outputStream">The stream where the compressed data have to be written.</param>
        /// <returns>The compression stream.</returns>
        Stream CreateStream(Stream outputStream);
    }
}
