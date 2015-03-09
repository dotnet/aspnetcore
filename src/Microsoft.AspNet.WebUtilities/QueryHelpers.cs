// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Framework.WebEncoders;

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
            return uri + (hasQuery ? "&" : "?") + UrlEncoder.Default.UrlEncode(name) + "=" + UrlEncoder.Default.UrlEncode(value);
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
                sb.Append(UrlEncoder.Default.UrlEncode(parameter.Key));
                sb.Append('=');
                sb.Append(UrlEncoder.Default.UrlEncode(parameter.Value));
                hasQuery = true;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parse a query string into its component key and value parts.
        /// </summary>
        /// <param name="text">The raw query string value, with or without the leading '?'.</param>
        /// <returns>A collection of parsed keys and values.</returns>
        public static IDictionary<string, string[]> ParseQuery(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString) && queryString[0] == '?')
            {
                queryString = queryString.Substring(1);
            }
            var accumulator = new KeyValueAccumulator<string, string>(StringComparer.OrdinalIgnoreCase);

            int textLength = queryString.Length;
            int equalIndex = queryString.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }
            int scanIndex = 0;
            while (scanIndex < textLength)
            {
                int delimiterIndex = queryString.IndexOf('&', scanIndex);
                if (delimiterIndex == -1)
                {
                    delimiterIndex = textLength;
                }
                if (equalIndex < delimiterIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    string name = queryString.Substring(scanIndex, equalIndex - scanIndex);
                    string value = queryString.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                    accumulator.Append(
                        Uri.UnescapeDataString(name.Replace('+', ' ')),
                        Uri.UnescapeDataString(value.Replace('+', ' ')));
                    equalIndex = queryString.IndexOf('=', delimiterIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                scanIndex = delimiterIndex + 1;
            }

            return accumulator.GetResults();
        }
    }
}