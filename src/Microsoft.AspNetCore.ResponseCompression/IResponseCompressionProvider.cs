// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Provides methods to be able to compress HTTP responses.
    /// </summary>
    public interface IResponseCompressionProvider
    {
        /// <summary>
        /// The name that will be searched in the 'Accept-Encoding' request header.
        /// </summary>
        string EncodingName { get; }

        /// <summary>
        /// Create a new compression stream.
        /// </summary>
        /// <param name="outputStream">The stream where the compressed data have to be written.</param>
        /// <returns>The new stream.</returns>
        Stream CreateStream(Stream outputStream);
    }
}
