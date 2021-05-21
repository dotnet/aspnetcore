// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides access to response trailers.
    /// <para>
    /// Response trailers allow for additional headers to be sent at the end of an HTTP/1.1 (chunked) or HTTP/2 response.
    /// For more details, see <see href="https://tools.ietf.org/html/rfc7230#section-4.1.2">RFC7230</see>.
    /// </para>
    /// </summary>
    public interface IHttpResponseTrailersFeature
    {
        /// <summary>
        /// Gets or sets the trailer headers.
        /// </summary>
        IHeaderDictionary Trailers { get; set; }
    }
}
