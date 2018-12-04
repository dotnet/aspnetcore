// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Used to examine requests and responses to see if compression should be enabled.
    /// </summary>
    public interface IResponseCompressionProvider
    {
        /// <summary>
        /// Examines the request and selects an acceptable compression provider, if any.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>A compression provider or null if compression should not be used.</returns>
        ICompressionProvider GetCompressionProvider(HttpContext context);

        /// <summary>
        /// Examines the response on first write to see if compression should be used.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        bool ShouldCompressResponse(HttpContext context);

        /// <summary>
        /// Examines the request to see if compression should be used for response.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        bool CheckRequestAcceptsCompression(HttpContext context);
    }
}
