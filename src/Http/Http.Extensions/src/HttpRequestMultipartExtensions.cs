// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Extensions
{
    /// <summary>
    /// Extension methods for working with multipart form requests.
    /// </summary>
    public static class HttpRequestMultipartExtensions
    {
        /// <summary>
        /// Gets the mutipart boundary from the <c>Content-Type</c> header.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <returns>The multipart boundary.</returns>
        public static string GetMultipartBoundary(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType))
            {
                return string.Empty;
            }
            return HeaderUtilities.RemoveQuotes(mediaType.Boundary).ToString();
        }
    }
}
