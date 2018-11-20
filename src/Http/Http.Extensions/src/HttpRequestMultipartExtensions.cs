// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Extensions
{
    public static class HttpRequestMultipartExtensions
    {
        public static string GetMultipartBoundary(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            MediaTypeHeaderValue mediaType;
            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out mediaType))
            {
                return string.Empty;
            }
            return HeaderUtilities.RemoveQuotes(mediaType.Boundary).ToString();
        }
    }
}
