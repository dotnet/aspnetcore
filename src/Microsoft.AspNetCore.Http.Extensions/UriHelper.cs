// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.Http.Extensions
{
    /// <summary>
    /// A helper class for constructing encoded Uris for use in headers and other Uris.
    /// </summary>
    public static class UriHelper
    {
        private const string SchemeDelimiter = "://";

        /// <summary>
        /// Combines the given URI components into a string that is properly encoded for use in HTTP headers.
        /// </summary>
        /// <param name="pathBase">The first portion of the request path associated with application root.</param>
        /// <param name="path">The portion of the request path that identifies the requested resource.</param>
        /// <param name="query">The query, if any.</param>
        /// <param name="fragment">The fragment, if any.</param>
        /// <returns></returns>
        public static string BuildRelative(
            PathString pathBase = new PathString(),
            PathString path = new PathString(),
            QueryString query = new QueryString(),
            FragmentString fragment = new FragmentString())
        {
            string combinePath = (pathBase.HasValue || path.HasValue) ? (pathBase + path).ToString() : "/";
            return combinePath + query.ToString() + fragment.ToString();
        }

        /// <summary>
        /// Combines the given URI components into a string that is properly encoded for use in HTTP headers.
        /// Note that unicode in the HostString will be encoded as punycode.
        /// </summary>
        /// <param name="scheme">http, https, etc.</param>
        /// <param name="host">The host portion of the uri normally included in the Host header. This may include the port.</param>
        /// <param name="pathBase">The first portion of the request path associated with application root.</param>
        /// <param name="path">The portion of the request path that identifies the requested resource.</param>
        /// <param name="query">The query, if any.</param>
        /// <param name="fragment">The fragment, if any.</param>
        /// <returns></returns>
        public static string BuildAbsolute(
            string scheme,
            HostString host,
            PathString pathBase = new PathString(),
            PathString path = new PathString(),
            QueryString query = new QueryString(),
            FragmentString fragment = new FragmentString())
        {
            var combinedPath = (pathBase.HasValue || path.HasValue) ? (pathBase + path).ToString() : "/";

            var encodedHost = host.ToString();
            var encodedQuery = query.ToString();
            var encodedFragment = fragment.ToString();

            // PERF: Calculate string length to allocate correct buffer size for StringBuilder.
            var length = scheme.Length + SchemeDelimiter.Length + encodedHost.Length
                + combinedPath.Length + encodedQuery.Length + encodedFragment.Length;

            return new StringBuilder(length)
                .Append(scheme)
                .Append(SchemeDelimiter)
                .Append(encodedHost)
                .Append(combinedPath)
                .Append(encodedQuery)
                .Append(encodedFragment)
                .ToString();
        }

        /// <summary>
        /// Generates a string from the given absolute or relative Uri that is appropriately encoded for use in
        /// HTTP headers. Note that a unicode host name will be encoded as punycode.
        /// </summary>
        /// <param name="uri">The Uri to encode.</param>
        /// <returns></returns>
        public static string Encode(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return BuildAbsolute(
                    scheme: uri.Scheme,
                    host: HostString.FromUriComponent(uri),
                    pathBase: PathString.FromUriComponent(uri),
                    query: QueryString.FromUriComponent(uri),
                    fragment: FragmentString.FromUriComponent(uri));
            }
            else
            {
                return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
        }

        /// <summary>
        /// Returns the combined components of the request URL in a fully escaped form suitable for use in HTTP headers
        /// and other HTTP operations.
        /// </summary>
        /// <param name="request">The request to assemble the uri pieces from.</param>
        /// <returns></returns>
        public static string GetEncodedUrl(this HttpRequest request)
        {
            return BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path, request.QueryString);
        }

        /// <summary>
        /// Returns the combined components of the request URL in a fully un-escaped form (except for the QueryString)
        /// suitable only for display. This format should not be used in HTTP headers or other HTTP operations.
        /// </summary>
        /// <param name="request">The request to assemble the uri pieces from.</param>
        /// <returns></returns>
        public static string GetDisplayUrl(this HttpRequest request)
        {
            var host = request.Host.Value;
            var pathBase = request.PathBase.Value;
            var path = request.Path.Value;
            var queryString = request.QueryString.Value;

            // PERF: Calculate string length to allocate correct buffer size for StringBuilder.
            var length = request.Scheme.Length + SchemeDelimiter.Length + host.Length
                + pathBase.Length + path.Length + queryString.Length;

            return new StringBuilder(length)
                .Append(request.Scheme)
                .Append(SchemeDelimiter)
                .Append(host)
                .Append(pathBase)
                .Append(path)
                .Append(queryString)
                .ToString();
        }
    }
}
