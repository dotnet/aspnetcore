// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.WebUtilities
{
    public static class QueryHelpers
    {
        /// <summary>
        /// Append the given query key and value to the URI.
        /// </summary>
        /// <param name="uri">The base URI.</param>
        /// <param name="name">The name of the query key.</param>
        /// <param name="value">The query value.</param>
        /// <returns>The combined result.</returns>
        public static string AddQueryString([NotNull] string uri, [NotNull] string name, [NotNull] string value)
        {
            bool hasQuery = uri.IndexOf('?') != -1;
            return uri + (hasQuery ? "&" : "?") + Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value);
        }

        /// <summary>
        /// Append the given query keys and values to the uri.
        /// </summary>
        /// <param name="uri">The base uri.</param>
        /// <param name="queryString">A collection of name value query pairs to append.</param>
        /// <returns>The combine result.</returns>
        public static string AddQueryString([NotNull] string uri, [NotNull] IDictionary<string, string> queryString)
        {
            var sb = new StringBuilder();
            sb.Append(uri);
            bool hasQuery = uri.IndexOf('?') != -1;
            foreach (var parameter in queryString)
            {
                sb.Append(hasQuery ? '&' : '?');
                sb.Append(Uri.EscapeDataString(parameter.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(parameter.Value));
                hasQuery = true;
            }
            return sb.ToString();
        }
    }
}