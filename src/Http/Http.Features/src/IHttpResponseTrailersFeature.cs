// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides access to Trailer response headers.
    /// <para>
    /// The Trailer response header allows the sender to include additional fields at the end of chunked messages.
    /// For more details, see <see href="https://tools.ietf.org/html/rfc7230#section-4.4">RFC7230</see>.
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
