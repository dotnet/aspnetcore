// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// This feature exposes HTTP request trailer headers, either for HTTP/1.1 chunked bodies or HTTP/2 trailing headers.
    /// </summary>
    public interface IHttpRequestTrailersFeature
    {
        /// <summary>
        /// The trailing headers received. This will return null if the request body has not been read yet.
        /// If there are no trailers this will return an empty collection.
        /// </summary>
        IHeaderDictionary Trailers { get; }
    }
}
