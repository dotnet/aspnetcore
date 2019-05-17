// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// HttpRequest extensions for working with request trailing headers.
    /// </summary>
    public static class RequestTrailerExtensions
    {
        public static StringValues GetDeclaredTrailers(this HttpRequest request)
        {
            return request.Headers[HeaderNames.Trailer];
        }

        /// <summary>
        /// Indicates if the server supports receiving trailer headers.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool SupportsTrailers(this HttpRequest request)
        {
            return request.HttpContext.Features.Get<IHttpRequestTrailersFeature>() != null;
        }

        /// <summary>
        /// Checks if the server supports trailers and they are available to be read now.
        /// This does not mean that there are any trailers to read.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool CheckTrailersAvailable(this HttpRequest request)
        {
            return request.HttpContext.Features.Get<IHttpRequestTrailersFeature>()?.Trailers != null;
        }

        /// <summary>
        /// Gets the requested trailing header from the response. Check <see cref="SupportsTrailers"/>
        /// or a NotSupportedException may be thrown.
        /// Check <see cref="CheckTrailersAvailable" /> or an InvalidOperationException may be thrown.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="trailerName"></param>
        public static StringValues GetTrailer(this HttpRequest request, string trailerName)
        {
            var feature = request.HttpContext.Features.Get<IHttpRequestTrailersFeature>();
            if (feature == null)
            {
                throw new NotSupportedException("This server does not support request trailers.");
            }
            if (feature.Trailers == null)
            {
                throw new InvalidOperationException("The trailers are not available yet. Did you finish reading the request body?");
            }

            return feature.Trailers[trailerName];
        }
    }
}
