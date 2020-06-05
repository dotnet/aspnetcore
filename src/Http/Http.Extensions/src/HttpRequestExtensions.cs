// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

#nullable enable

namespace Microsoft.AspNetCore.Http
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Checks the Content-Type header for JSON types.
        /// </summary>
        /// <returns>true if the Content-Type header represents a JSON content type; otherwise, false.</returns>
        public static bool HasJsonContentType(this HttpRequest request)
        {
            return request.HasJsonContentType(out _);
        }

        internal static bool HasJsonContentType(this HttpRequest request, out StringSegment charset)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mt))
            {
                charset = StringSegment.Empty;
                return false;
            }

            // Matches application/json
            if (mt.MediaType.Equals(JsonConstants.JsonContentType, StringComparison.OrdinalIgnoreCase))
            {
                charset = mt.Charset;
                return true;
            }

            // Matches +json, e.g. application/ld+json
            if (mt.Suffix.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                charset = mt.Charset;
                return true;
            }

            charset = StringSegment.Empty;
            return false;
        }
    }
}
