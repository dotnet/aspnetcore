// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// This feature exposes HTTP request trailer headers, either for HTTP/1.1 chunked bodies or HTTP/2 trailing headers.
    /// </summary>
    public interface IHttpRequestTrailersFeature
    {
        /// <summary>
        /// Indicates if the <see cref="Trailers"/> are available yet. They may not be available until the
        /// request body is fully read.
        /// </summary>
        bool Available { get; }

        /// <summary>
        /// The trailing headers received. This will throw <see cref="InvalidOperationException"/> if <see cref="Available"/>
        /// returns false. They may not be available until the request body is fully read. If there are no trailers this will
        /// return an empty collection.
        /// </summary>
        IHeaderDictionary Trailers { get; }
    }
}
