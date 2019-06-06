// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Options for the HTTP response compression middleware.
    /// </summary>
    public class ResponseCompressionOptions
    {
        /// <summary>
        /// Response Content-Type MIME types to compress.
        /// </summary>
        public IEnumerable<string> MimeTypes { get; set; }

        /// <summary>
        /// Response Content-Type MIME types to not compress.
        /// </summary>
        public IEnumerable<string> ExcludedMimeTypes { get; set; }

        /// <summary>
        /// Indicates if responses over HTTPS connections should be compressed. The default is 'false'.
        /// Enabling compression on HTTPS requests for remotely manipulable content may expose security problems.
        /// </summary>
        /// <remarks>
        /// This can be overridden per request using <see cref="IHttpsCompressionFeature"/>.
        /// </remarks>
        public bool EnableForHttps { get; set; } = false;

        /// <summary>
        /// The <see cref="ICompressionProvider"/> types to use for responses.
        /// Providers are prioritized based on the order they are added.
        /// </summary>
        public CompressionProviderCollection Providers { get; } = new CompressionProviderCollection();
    }
}
