// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Response Compression middleware utility methods.
    /// </summary>
    public static class ResponseCompressionUtils
    {
        /// <summary>
        /// Create a delegate that propose to compress response, depending on a list of authorized
        /// MIME types for the HTTP response.
        /// </summary>
        public static Func<HttpContext, bool> CreateShouldCompressResponseDelegate(IEnumerable<string> mimeTypes)
        {
            if (mimeTypes == null)
            {
                throw new ArgumentNullException(nameof(mimeTypes));
            }

            var mimeTypeSet = new HashSet<string>(mimeTypes);

            return (httpContext) =>
            {
                var mimeType = httpContext.Response.ContentType;

                if (string.IsNullOrEmpty(mimeType))
                {
                    return false;
                }

                var separator = mimeType.IndexOf(';');
                if (separator >= 0)
                {
                    // Remove the content-type optional parameters
                    mimeType = mimeType.Substring(0, separator);
                    mimeType = mimeType.Trim();
                }

                return mimeTypeSet.Contains(mimeType);
            };
        }
    }
}
