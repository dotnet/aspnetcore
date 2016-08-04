// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Options for the HTTP response compression middleware.
    /// </summary>
    public class ResponseCompressionOptions
    {
        /// <summary>
        /// Called when an HTTP request accepts a compatible compression algorithm, and returns True
        /// if the response should be compressed.
        /// </summary>
        public Func<HttpContext, bool> ShouldCompressResponse { get; set; }

        /// <summary>
        /// The compression providers. If 'null', the GZIP provider is set as default.
        /// </summary>
        public IEnumerable<IResponseCompressionProvider> Providers { get; set; }

        /// <summary>
        /// 'False' to enable compression only on HTTP requests. Enable compression on HTTPS requests
        /// may lead to security problems.
        /// </summary>
        public bool EnableHttps { get; set; } = false;
    }
}
